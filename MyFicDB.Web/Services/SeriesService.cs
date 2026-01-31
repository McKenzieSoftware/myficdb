using Microsoft.EntityFrameworkCore;
using MyFicDB.Core;
using MyFicDB.Core.Helpers;
using MyFicDB.Core.Models;

namespace MyFicDB.Web.Services
{
    /// <summary>
    /// Provides operations such as creating, normalization, and search suggestions for Series.  Series logic is centralized to prevent duplication.
    /// </summary>
    public sealed class SeriesService
    {
        private readonly ApplicationDbContext _context;
        
        public sealed record SeriesSuggestion(Guid Id, string Name);

        public SeriesService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Resolves collection of Series names to existing series and creates any missing ones where required; also normalized and de-duped
        /// </summary>
        public async Task<List<Series>> GetOrCreateAsync(IEnumerable<string> rawNames, CancellationToken cancellationToken = default)
        {
            var names = NamePipeline.CleanDedupeAndLimit(rawNames, max: 30);
            if (names.Count == 0)
            {
                return [];
            }

            var normalized = names.Select(NamePipeline.NormalizeUpper).ToList();

            // gets all our existing series
            var existing = await _context.Series
                .Where(s => normalized.Contains(s.NormalizedName))
                .ToListAsync(cancellationToken);

            // get series based on normalized name
            var existingByNorm = existing.ToDictionary(x => x.NormalizedName, x => x);
            var created = new List<Series>();

            foreach (var displayName in names)
            {
                var norm = NamePipeline.NormalizeUpper(displayName);
                if (existingByNorm.ContainsKey(norm))
                {
                    continue;
                }

                var slugBase = NamePipeline.Slugify(norm);
                var slug = await EnsureUniqueSlugAsync(slugBase, cancellationToken);

                var series = new Series
                {
                    Name = displayName,
                    NormalizedName = norm,
                    Slug = slug
                };

                _context.Series.Add(series);
                created.Add(series);
                existingByNorm[norm] = series;
            }

            if (created.Count > 0)
            {
                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException)
                {
                    // Concurrency: someone else created one or more series.
                    // This shouldn't actually happen since this is a single user system, but just in case 
                    // someone shares an account or something (that also shouldn't happen)
                    return await _context.Series
                        .Where(s => normalized.Contains(s.NormalizedName))
                        .ToListAsync(cancellationToken);
                }
            }

            return existingByNorm.Values.ToList();
        }

        /// <summary>
        /// Returns a list of existing series based on the users input <paramref name="query"/>
        /// </summary>
        public async Task<List<SeriesSuggestion>> SuggestAsync(string query, int limit = 10, CancellationToken cancellationToken = default)
        {
            query = (query ?? string.Empty).Trim();
            if (query.Length < 2)
            {
                return [];
            }

            var qNorm = NamePipeline.NormalizeUpper(query);
            limit = Math.Clamp(limit, 1, 20);

            return await _context.Series
                .AsNoTracking()
                .Where(s => s.NormalizedName.Contains(qNorm))
                .OrderByDescending(s => s.NormalizedName.StartsWith(qNorm))
                .ThenBy(s => s.Name)
                .Take(limit)
                .Select(s => new SeriesSuggestion(s.Id, s.Name))
                .ToListAsync(cancellationToken);
        }

        private async Task<string> EnsureUniqueSlugAsync(string slugBase, CancellationToken ct)
        {
            var baseSlug = string.IsNullOrWhiteSpace(slugBase) ? "series" : slugBase.Trim();
            var candidate = baseSlug;

            var i = 2;
            while (await _context.Series.AnyAsync(s => s.Slug == candidate, ct))
            {
                candidate = $"{baseSlug}-{i}";
                i++;

                if (i > 1000)
                {
                    throw new InvalidOperationException("Could not create unique series slug.");
                }
            }

            return candidate;
        }
    }
}
