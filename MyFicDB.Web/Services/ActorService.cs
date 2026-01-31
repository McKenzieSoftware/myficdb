using Microsoft.EntityFrameworkCore;
using MyFicDB.Core;
using MyFicDB.Core.Helpers;
using MyFicDB.Core.Models;
using System.Security.Cryptography;

namespace MyFicDB.Web.Services
{
    /// <summary>
    /// Provides operations such as creating, normalization, slug gen, and search suggestions for Actors.
    /// Actor logic is centralized to prevent duplication.
    /// </summary>
    public class ActorService
    {
        private readonly ApplicationDbContext _context;

        public sealed record ActorSuggestion(Guid Id, string Name);

        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/png",
            "image/jpeg",
            "image/webp"
        };

        private const long MaxBytes = 500 * 1024; // 500KB TODO: Maybe make this configurable?

        public ActorService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Creates a fully-specified actor (used by /actor/create) and ensures name normalization + unique slug + optional image
        /// </summary>
        public async Task<Actor> CreateAsync(string name, string description, int? age, IFormFile? image, CancellationToken cancellationToken = default)
        {
            var cleanedName = NamePipeline.CleanDisplayName(name);
            if (string.IsNullOrWhiteSpace(cleanedName))
            {
                throw new ArgumentException("Actor name is required.", nameof(name));
            }

            var normalizedName = NamePipeline.NormalizeUpper(cleanedName);

            // If the actor already exists by normalized name, return it (prevents duplicates).
            var existing = await _context.Actors
                .Include(a => a.Image)
                .FirstOrDefaultAsync(a => a.NormalizedName == normalizedName, cancellationToken);

            if (existing is not null)
            {
                return existing;
            }

            var slugBase = NamePipeline.Slugify(normalizedName);
            var slug = await EnsureUniqueSlugAsync(slugBase, null, cancellationToken);

            var (imgBytes, imgContentType, imgFileName, imgSha) = await ReadAndValidateImageAsync(image, cancellationToken);

            await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

            var actor = new Actor
            {
                Name = cleanedName,
                NormalizedName = normalizedName,
                Slug = slug,
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                Age = age
            };

            _context.Actors.Add(actor);
            await _context.SaveChangesAsync(cancellationToken);

            if (imgBytes is not null)
            {
                actor.Image = new ActorImage
                {
                    ActorId = actor.Id,
                    Data = imgBytes,
                    ContentType = imgContentType!,
                    FileName = imgFileName,
                    Sha256 = imgSha
                };

                await _context.SaveChangesAsync(cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
            return actor;
        }

        /// <summary>
        /// Resolves a collection of actor names to existing actors and creates any missing ones.
        /// </summary>
        public async Task<List<Actor>> GetOrCreateAsync(IEnumerable<string> rawNames, CancellationToken cancellationToken = default)
        {
            var names = NamePipeline.CleanDedupeAndLimit(rawNames, max: 30);
            if (names.Count == 0)
            {
                return [];
            }

            var normalized = names.Select(NamePipeline.NormalizeUpper).ToList();

            // get existing actors
            var existing = await _context.Actors
                .Where(a => normalized.Contains(a.NormalizedName))
                .ToListAsync(cancellationToken);

            // gets actors based on normalized name
            var existingByNorm = existing.ToDictionary(x => x.NormalizedName, x => x);
            var created = new List<Actor>();

            foreach (var displayName in names)
            {
                var norm = NamePipeline.NormalizeUpper(displayName);
                if (existingByNorm.ContainsKey(norm))
                {
                    continue;
                }

                var slugBase = NamePipeline.Slugify(norm);
                var slug = await EnsureUniqueSlugAsync(slugBase, excludeActorId: null, ct: cancellationToken);

                var actor = new Actor
                {
                    Name = displayName,
                    NormalizedName = norm,
                    Slug = slug,
                    Description = null,
                    Age = null,
                    Image = null
                };

                _context.Actors.Add(actor);
                created.Add(actor);
                existingByNorm[norm] = actor;
            }

            if (created.Count > 0)
            {
                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException)
                {
                    // Concurrency: someone else created one or more tags.
                    // This shouldn't actually happen since this is a single user system, but just in case 
                    // someone shares an account (that also shouldn't happen) or runs the same thing on a Computer and
                    // mobile at the same time

                    return await _context.Actors
                        .Where(a => normalized.Contains(a.NormalizedName))
                        .ToListAsync(cancellationToken);
                }
            }

            return existingByNorm.Values.ToList();
        }

        public async Task<Actor> UpdateAsync(Guid actorId, string routeSlug, string name, string? description, int? age, IFormFile? image, bool removeImage, CancellationToken cancellationToken = default)
        {
            if (actorId == Guid.Empty)
            {
                throw new ArgumentException("Actor id is required.", nameof(actorId));
            }

            if (string.IsNullOrWhiteSpace(routeSlug))
            {
                throw new ArgumentException("Route slug is required.", nameof(routeSlug));
            }

            var actor = await _context.Actors
                .Include(a => a.Image)
                .FirstOrDefaultAsync(a => a.Id == actorId, cancellationToken);

            if (actor is null)
            {
                throw new KeyNotFoundException("Actor not found.");
            }

            // Prevent editing the wrong record
            if (!string.Equals(actor.Slug, routeSlug, StringComparison.OrdinalIgnoreCase))
            {
                throw new KeyNotFoundException("Actor not found.");
            }

            var cleanedName = NamePipeline.CleanDisplayName(name);
            if (string.IsNullOrWhiteSpace(cleanedName))
            {
                throw new InvalidOperationException("Actor name is required.");
            }

            var normalizedName = NamePipeline.NormalizeUpper(cleanedName);

            // prevent renaming to an existing actor name
            var nameCollision = await _context.Actors
                .AsNoTracking()
                .AnyAsync(a => a.Id != actor.Id && a.NormalizedName == normalizedName, cancellationToken);

            if (nameCollision)
            {
                throw new InvalidOperationException("An actor with that name already exists.");
            }

            // If a new image is uploaded, validate/read it.
            var (newBytes, newContentType, newFileName, newSha) = await ReadAndValidateImageAsync(image, cancellationToken);

            await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

            // Update main fields
            actor.Name = cleanedName;
            actor.NormalizedName = normalizedName;
            actor.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            actor.Age = age;

            // Update slug if name implies a different slug
            var proposedSlugBase = NamePipeline.Slugify(normalizedName);
            if (string.IsNullOrWhiteSpace(proposedSlugBase))
            {
                proposedSlugBase = "actor";
            }

            // Only regenerate slug if it actually differs
            if (!string.Equals(actor.Slug, proposedSlugBase, StringComparison.OrdinalIgnoreCase))
            {
                actor.Slug = await EnsureUniqueSlugAsync(proposedSlugBase, actor.Id, cancellationToken);
            }

            // Image rules:
            // - If new upload => replace/create
            // - Else if removeImage => delete
            if (newBytes is not null)
            {
                if (actor.Image is null)
                {
                    actor.Image = new ActorImage
                    {
                        ActorId = actor.Id,
                        Data = newBytes,
                        ContentType = newContentType!,
                        FileName = newFileName,
                        Sha256 = newSha
                    };
                }
                else
                {
                    actor.Image.Data = newBytes;
                    actor.Image.ContentType = newContentType!;
                    actor.Image.FileName = newFileName;
                    actor.Image.Sha256 = newSha;
                }
            }
            else if (removeImage && actor.Image is not null)
            {
                _context.Set<ActorImage>().Remove(actor.Image);
                actor.Image = null;
            }

            await _context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return actor;
        }

        /// <summary>
        /// Returns a list of existing Actors based on the users input query.
        /// </summary>
        public async Task<List<ActorSuggestion>> SuggestAsync(string query, int limit = 10, CancellationToken cancellationToken = default)
        {
            query = (query ?? string.Empty).Trim();
            if (query.Length < 2)
            {
                return [];
            }

            var qNorm = NamePipeline.NormalizeUpper(query);
            limit = Math.Clamp(limit, 1, 20);

            return await _context.Actors
                .AsNoTracking()
                .Where(a => a.NormalizedName.Contains(qNorm))
                .OrderByDescending(a => a.NormalizedName.StartsWith(qNorm))
                .ThenBy(a => a.Name)
                .Take(limit)
                .Select(a => new ActorSuggestion(a.Id, a.Name))
                .ToListAsync(cancellationToken);
        }

        private async Task<string> EnsureUniqueSlugAsync(string slugBase, Guid? excludeActorId, CancellationToken ct)
        {
            var baseSlug = string.IsNullOrWhiteSpace(slugBase) ? "actor" : slugBase.Trim();
            var candidate = baseSlug;

            // candidate must be unique. allow the current actor
            var i = 2;
            while (await _context.Actors.AnyAsync(
                a => a.Slug == candidate && (excludeActorId == null || a.Id != excludeActorId.Value),
                ct))
            {
                candidate = $"{baseSlug}-{i}";
                i++;
                if (i > 1000)
                {
                    throw new InvalidOperationException("Could not create unique actor slug.");
                }
            }

            return candidate;
        }

        private static async Task<(byte[]? Bytes, string? ContentType, string? FileName, string? Sha256)> ReadAndValidateImageAsync(IFormFile? file, CancellationToken ct)
        {
            if (file is null || file.Length <= 0)
            {
                return (null, null, null, null);
            }

            if (file.Length > MaxBytes)
            {
                throw new InvalidOperationException($"Image must be under {MaxBytes / 1024}KB.");
            }

            if (!AllowedContentTypes.Contains(file.ContentType))
            {
                throw new InvalidOperationException("Only PNG, JPG, or WEBP images are allowed.");
            }

            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);

            var bytes = ms.ToArray();
            var sha = ComputeSha256Hex(bytes);

            return (bytes, file.ContentType, file.FileName, sha);
        }

        private static string ComputeSha256Hex(byte[] bytes)
        {
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
