using Microsoft.EntityFrameworkCore;
using MyFicDB.Core;
using MyFicDB.Core.Helpers;
using MyFicDB.Core.Models;

namespace MyFicDB.Web.Services
{
    /// <summary>
    /// Provides operations such as creating, normalization, slug gen, and search suggestions for Tags.  Tag logic is centralized to prevent duplicatation
    /// </summary>
    public sealed class TagService
    {
        private readonly ApplicationDbContext _context;

        public sealed record TagSuggestion(Guid Id, string Name, string Slug);

        public TagService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Resolves a collection of tag names to existing tags and creates any missing ones where required.  Tags are also normalized and de-duped.
        /// </summary>
        public async Task<List<Tag>> GetOrCreateAsync(IEnumerable<string> rawNames, CancellationToken cancellationToken = default)
        {
            var names = NamePipeline.CleanDedupeAndLimit(rawNames, max: 30);
            if (names.Count == 0)
            {
                return [];
            }

            var normalized = names.Select(NamePipeline.NormalizeUpper).ToList();

            // gets all our existing tags
            var existing = await _context.Tags
                .Where(t => normalized.Contains(t.NormalizedName))
                .ToListAsync(cancellationToken);

            // gets tags based on normalized name
            var existingByNorm = existing.ToDictionary(x => x.NormalizedName, x => x);
            var created = new List<Tag>();

            foreach (var displayName in names)
            {
                var norm = NamePipeline.NormalizeUpper(displayName);
                if (existingByNorm.ContainsKey(norm))
                {
                    continue;
                }

                var slugBase = NamePipeline.Slugify(norm);
                var slug = await EnsureUniqueSlugAsync(slugBase, cancellationToken);

                var tag = new Tag
                {
                    Id = Guid.NewGuid(),
                    Name = displayName.ToLowerInvariant(), // invariant: tags stored lowercase
                    NormalizedName = norm,                 // invariant: uppercase
                    Slug = slug
                };

                _context.Tags.Add(tag);
                created.Add(tag);
                existingByNorm[norm] = tag;
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
                    // someone shares an account or something (that also shouldn't happen)
                    return await _context.Tags
                        .Where(t => normalized.Contains(t.NormalizedName))
                        .ToListAsync(cancellationToken);
                }
            }

            return existingByNorm.Values.ToList();
        }

        /// <summary>
        /// Returns a list of existing tags based on the users input <paramref name="query"/>
        /// </summary>
        public async Task<List<TagSuggestion>> SuggestAsync(string query, int limit = 10, CancellationToken cancellationToken = default)
        {
            query = (query ?? string.Empty).Trim();
            if (query.Length < 2)
            {
                return [];
            }

            // Tags store NormalizedName uppercase (invariant)
            var qNorm = NamePipeline.NormalizeUpper(query);
            limit = Math.Clamp(limit, 1, 20);

            return await _context.Tags
                .AsNoTracking()
                .Where(t => t.NormalizedName.Contains(qNorm))
                .OrderByDescending(t => t.NormalizedName.StartsWith(qNorm))
                .ThenBy(t => t.Name)
                .Take(limit)
                .Select(t => new TagSuggestion(t.Id, t.Name, t.Slug))
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Generates a unique slug for a tag, appends a numeric suffix if needed
        /// </summary>
        private async Task<string> EnsureUniqueSlugAsync(string slugBase, CancellationToken cancellationToken)
        {
            var baseSlug = string.IsNullOrWhiteSpace(slugBase) ? "tag" : slugBase.Trim();
            var candidate = baseSlug;

            var i = 2;
            while (await _context.Tags.AnyAsync(t => t.Slug == candidate, cancellationToken))
            {
                candidate = $"{baseSlug}-{i}";
                i++;
                if (i > 1000)
                {
                    throw new InvalidOperationException("Could not create unique tag slug with iteration of 1000.");
                }
            }

            return candidate;
        }
    }
}
