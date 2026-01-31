using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyFicDB.Core.Models;
using MyFicDB.Core.Models.Story;

namespace MyFicDB.Core
{
    public sealed class ApplicationDbContext : IdentityDbContext, IDataProtectionKeyContext
    {
        // Core entities
        public DbSet<Story> Stories { get; set; }
        public DbSet<Chapter> Chapters { get; set; }

        // Chapter Information
        public DbSet<ChapterContent> ChapterContents { get; set; }
        public DbSet<ChapterInlineNote> ChapterInlineNotes { get; set; }

        // Relationship items for Stories
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Actor> Actors { get; set; }
        public DbSet<Series> Series { get; set; }

        // Join tables for relationship items to Stories
        public DbSet<StoryTag> StoryTags { get; set; }
        public DbSet<StoryActor> StoryActors { get; set; }
        public DbSet<StorySeries> StorySeries { get; set; }
        public DbSet<ActorImage> ActorImages { get; set; }

        // Data Protection
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

        private ILogger<ApplicationDbContext> _logger;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ILogger<ApplicationDbContext> logger) : base(options)
        {
            _logger = logger;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // STORY
            modelBuilder.Entity<Story>(b =>
            {
                b.HasKey(x => x.Id);

                b.Property(x => x.Title)
                    .HasMaxLength(300)
                    .IsRequired();

                b.Property(x => x.Summary);
                b.Property(x => x.Notes);

                b.HasMany(x => x.Chapters)
                    .WithOne(x => x.Story)
                    .HasForeignKey(x => x.StoryId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasMany(x => x.StoryTags)
                    .WithOne(x => x.Story)
                    .HasForeignKey(x => x.StoryId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasMany(x => x.StoryActors)
                    .WithOne(x => x.Story)
                    .HasForeignKey(x => x.StoryId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasMany(x => x.StorySeries)
                    .WithOne(x => x.Story)
                    .HasForeignKey(x => x.StoryId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasIndex(x => x.Title);
                b.HasIndex(x => x.CreatedDate);
            });

            // CHAPTER
            modelBuilder.Entity<Chapter>(b =>
            {
                b.HasKey(x => x.Id);

                b.Property(x => x.Title)
                    .HasMaxLength(300);

                b.Property(x => x.ChapterNumber)
                    .IsRequired();

                // Ensure no duplicate chapter number inside a single story.
                b.HasIndex(x => new { x.StoryId, x.ChapterNumber })
                    .IsUnique();

                // 1:1 Chapter -> ChapterContent (shared PK)
                b.HasOne(x => x.Content)
                    .WithOne(x => x.Chapter)
                    .HasForeignKey<ChapterContent>(x => x.ChapterId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Helpful for retrieving chapters in order quickly
                b.HasIndex(x => new { x.StoryId, x.ChapterNumber });
            });

            // CHAPTER CONTENT
            modelBuilder.Entity<ChapterContent>(b =>
            {
                b.HasKey(x => x.ChapterId);

                // IMPORTANT: shared PK value is assigned from Chapter.Id, not DB-generated.
                b.Property(x => x.ChapterId)
                    .ValueGeneratedNever();

                b.Property(x => x.Body)
                    .IsRequired();

                b.Property(x => x.WordCount).IsRequired();
            });

            // CHAPTER INLINE NOTES
            modelBuilder.Entity<ChapterInlineNote>(b =>
            {
                b.HasKey(x => x.Id);

                // IMPORTANT: shared PK value is assigned from Chapter.Id, not DB-generated.
                b.Property(x => x.ChapterId)
                    .ValueGeneratedNever();

                b.Property(x => x.Details).HasMaxLength(800).IsRequired();

                b.HasOne(x => x.Chapter)
                    .WithMany()
                    .HasForeignKey(x => x.ChapterId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // TAGS
            modelBuilder.Entity<Tag>(b =>
            {
                b.HasKey(x => x.Id);

                b.Property(x => x.Name)
                    .HasMaxLength(80)
                    .UseCollation("NOCASE")
                    .IsRequired();

                b.Property(x => x.Slug)
                    .HasMaxLength(100)
                    .IsRequired();

                b.Property(x => x.NormalizedName)
                    .HasMaxLength(80)
                    .IsRequired();

                b.HasIndex(x => x.Name).IsUnique();
                b.HasIndex(x => x.Slug).IsUnique();
                b.HasIndex(x => x.NormalizedName).IsUnique();
            });

            // ACTORS (STARS? PERFORMERS? CAST? IDK; WE'LL KEEP IT AS ACTORS FOR NOW. OPEN TO RENAMING IF IT WORKS BETTER THAN ACTORS.)
            modelBuilder.Entity<Actor>(b =>
            {
                b.HasKey(x => x.Id);

                b.Property(x => x.NormalizedName)
                    .HasMaxLength(200)
                    .IsRequired();

                b.Property(x => x.Name)
                    .HasMaxLength(200)
                    .IsRequired();

                b.Property(x => x.Slug)
                    .HasMaxLength(220)
                    .IsRequired();

                b.Property(x => x.Description)
                    .HasMaxLength(2000);

                b.Property(x => x.Age);

                b.HasIndex(x => x.Slug).IsUnique();
                b.HasIndex(x => x.Name).IsUnique();
                b.HasIndex(x => x.NormalizedName).IsUnique();

                b.HasOne(x => x.Image)
                    .WithOne(x => x.Actor)
                    .HasForeignKey<ActorImage>(x => x.ActorId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // SERIES
            modelBuilder.Entity<Series>(b =>
            {
                b.HasKey(x => x.Id);

                b.Property(x => x.Name)
                    .HasMaxLength(200)
                    .UseCollation("NOCASE")
                    .IsRequired();

                b.Property(x => x.NormalizedName)
                    .HasMaxLength(200)
                    .IsRequired();

                b.Property(x => x.Slug)
                    .HasMaxLength(220)
                    .IsRequired();

                b.HasIndex(x => x.Name).IsUnique();
                b.HasIndex(x => x.NormalizedName).IsUnique();
                b.HasIndex(x => x.Slug).IsUnique();
            });

            // ACTOR <> IMAGE RELATIONSHIP
            modelBuilder.Entity<ActorImage>(entity =>
            {
                entity.HasKey(x => x.ActorId);

                entity.Property(x => x.ActorId)
                    .ValueGeneratedNever();

                entity.Property(x => x.Data)
                    .IsRequired(); // SQLite maps byte[] > BLOB

                entity.Property(x => x.ContentType)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.FileName)
                    .HasMaxLength(255);

                entity.Property(x => x.Sha256)
                    .HasMaxLength(64);
            });

            // STORY <> TAG RELATIONSHIP
            modelBuilder.Entity<StoryTag>(b =>
            {
                b.HasKey(x => new { x.StoryId, x.TagId });

                b.HasOne(x => x.Story)
                    .WithMany(x => x.StoryTags)
                    .HasForeignKey(x => x.StoryId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(x => x.Tag)
                    .WithMany(x => x.StoryTags)
                    .HasForeignKey(x => x.TagId)
                    .OnDelete(DeleteBehavior.Restrict); // Can't delete a tag if it's referenced under a story

                // Helpful for queries "all stories with tag X"
                b.HasIndex(x => x.TagId);
            });

            // STORY <> ACOTOR RELATIONSHIP
            modelBuilder.Entity<StoryActor>(b =>
            {
                b.HasKey(x => new { x.StoryId, x.ActorId });

                b.HasOne(x => x.Story)
                    .WithMany(x => x.StoryActors)
                    .HasForeignKey(x => x.StoryId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(x => x.Actor)
                    .WithMany(x => x.StoryActors)
                    .HasForeignKey(x => x.ActorId)
                    .OnDelete(DeleteBehavior.Restrict); // Can't delete an actor if it's referenced under a story

                b.HasIndex(x => x.ActorId);
            });

            // STORY <> SERIES RELATIONSHIP
            modelBuilder.Entity<StorySeries>(b =>
            {
                b.HasKey(x => new { x.StoryId, x.SeriesId });

                b.HasOne(x => x.Story)
                    .WithMany(x => x.StorySeries)
                    .HasForeignKey(x => x.StoryId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(x => x.Series)
                    .WithMany(x => x.StorySeries)
                    .HasForeignKey(x => x.SeriesId)
                    .OnDelete(DeleteBehavior.Restrict); // Can't delete a series if it's referenced under a story

                b.HasIndex(x => x.SeriesId);
            });

            // Just so it matches the existing table naming convnetion 
            modelBuilder.Entity<DataProtectionKey>().ToTable("tblDataProtectionKeys");

            RenameDefaultTables(modelBuilder);
        }

        /// <summary>
        /// Renames default Asp tables to match apps naming convention for db
        /// </summary>
        /// <param name="builder"></param>
        private void RenameDefaultTables(ModelBuilder builder)
        {
            builder.Entity<IdentityUser>(b =>
            {
                b.ToTable("tblSystemUser");
            });

            builder.Entity<IdentityUserClaim<string>>(b =>
            {
                b.ToTable("tblSystemUserClaims");
            });

            builder.Entity<IdentityUserLogin<string>>(b =>
            {
                b.ToTable("tblSystemUserLogins");
            });

            builder.Entity<IdentityUserToken<string>>(b =>
            {
                b.ToTable("tblSystemUserTokens");
            });

            builder.Entity<IdentityRole>(b =>
            {
                b.ToTable("tblSystemRoles");
            });

            builder.Entity<IdentityRoleClaim<string>>(b =>
            {
                b.ToTable("tblSystemRoleClaims");
            });

            builder.Entity<IdentityUserRole<string>>(b =>
            {
                b.ToTable("tblSystemUserRoles");
            });
        }

        #region Save Override
        // Below Source: https://threewill.com/how-to-auto-generate-created-updated-field-in-ef-core/
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            try
            {
                OnBeforeSaving();
                return base.SaveChanges(acceptAllChangesOnSuccess);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to save changes");
                return -1;
            }
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            OnBeforeSaving();
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void OnBeforeSaving()
        {
            var entries = ChangeTracker.Entries();
            var utcNow = DateTime.UtcNow;

            foreach (var entry in entries)
            {
                // for entities that inherit from BaseEntity,
                // set UpdatedOn / CreatedOn appropriately
                if (entry.Entity is Base trackable)
                {
                    switch (entry.State)
                    {
                        case EntityState.Modified:
                            // set the updated date to "now"
                            trackable.UpdatedDate = utcNow;

                            // mark property as "don't touch"
                            // we don't want to update on a Modify operation
                            entry.Property("CreatedDate").IsModified = false;
                            //entry.Property("CreatedBy").IsModified = false;
                            break;

                        case EntityState.Added:
                            // set both updated and created date to "now"
                            trackable.CreatedDate = utcNow;
                            trackable.UpdatedDate = utcNow;
                            break;
                    }
                }
            }
        }
        #endregion
    }
}
