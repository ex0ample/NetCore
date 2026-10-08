using Microsoft.EntityFrameworkCore;
using EBookStoreApi.Entities;

namespace EBookStoreApi.Data;

public class EBookDbContext : DbContext
{
    public EBookDbContext(DbContextOptions<EBookDbContext> options) : base(options)
    {
    }

    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Book> Books => Set<Book>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // กำหนด Fluent API สำหรับ Book Entity
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Title).IsRequired().HasMaxLength(200);
            entity.Property(b => b.ISBN).IsRequired().HasMaxLength(20);
            entity.HasIndex(b => b.ISBN).IsUnique();
            entity.Property(b => b.Price).HasPrecision(10, 2);
            entity.Property(b => b.FileUrl).HasMaxLength(500);

            // ความสัมพันธ์ One-to-Many: Author -> Books
            entity.HasOne(b => b.Author)
                  .WithMany(a => a.Books)
                  .HasForeignKey(b => b.AuthorId)
                  .OnDelete(DeleteBehavior.Cascade);

            // ความสัมพันธ์ One-to-Many: Category -> Books
            entity.HasOne(b => b.Category)
                  .WithMany(c => c.Books)
                  .HasForeignKey(b => b.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(50);
            entity.HasIndex(c => c.Name).IsUnique();
        });

        modelBuilder.Entity<Author>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Name).IsRequired().HasMaxLength(100);
            entity.Property(a => a.Email).HasMaxLength(255);
        });
    }
}
