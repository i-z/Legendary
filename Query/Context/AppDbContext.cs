using Legendary.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Legendary.Data.Context
{
    public class AppDbContext : DbContext
    {
        public DbSet<Book> Books { get; set; }
        public DbSet<Chapter> Chapters { get; set; }
        public DbSet<Entry> Entries { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<AuthToken> AuthTokens { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Book>(entity =>
            {
                entity.HasKey(b => b.bookId);
                entity.Property(b => b.title).HasMaxLength(255).IsRequired();
            });

            modelBuilder.Entity<Chapter>(entity =>
            {
                entity.HasKey(c => new { c.bookId, c.chapter });
                entity.Property(c => c.title).HasMaxLength(255);
                entity.Property(c => c.summary).HasMaxLength(255);

                entity.HasOne<Book>()
                      .WithMany()
                      .HasForeignKey(c => c.bookId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Entry>(entity =>
            {
                entity.HasKey(e => e.entryId);
                entity.Property(e => e.title).HasMaxLength(255);
                entity.Property(e => e.summary).HasMaxLength(255);
                entity.Property(e => e.book_title).HasMaxLength(255);
                entity.Property(e => e.author).HasMaxLength(64);
                entity.Property(e => e.auther_email).HasMaxLength(64);

                // Relationships (optional depending on real use)
                entity.HasOne<Book>()
                      .WithMany()
                      .HasForeignKey(e => e.bookId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Optional: index on userId/bookId/chapter if you filter on them often
            });

            modelBuilder.Entity<User>().HasKey(u => u.userId);
            modelBuilder.Entity<AuthToken>().HasKey(t => t.Id);
            modelBuilder.Entity<AuthToken>()
                .HasOne(t => t.User)
                .WithMany(u => u.Tokens)
                .HasForeignKey(t => t.UserId);
        }
    }
}
