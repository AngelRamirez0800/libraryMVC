using System.Reflection.Emit;
using libraryMVC.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace libraryMVC.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public required DbSet<Book> Books
        {
            get; set;
        }
        public required DbSet<Author> Authors
        {
            get; set;
        }
        public required DbSet<BookImage> BookImages
        {
            get; set;
        }
        public required DbSet<Category> Categories
        {
            get; set;
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Author>()
                .Property(author => author.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()")
                .HasValueGenerator<TemporaryGuidValueGenerator>();

            modelBuilder.Entity<Book>()
                .Property(book => book.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()")
                .HasValueGenerator<TemporaryGuidValueGenerator>();

            modelBuilder.Entity<BookImage>()
                .Property(image => image.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()")
                .HasValueGenerator<TemporaryGuidValueGenerator>();

            modelBuilder.Entity<Category>()
                .Property(category => category.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()")
                .HasValueGenerator<TemporaryGuidValueGenerator>();

            modelBuilder.Entity<Author>()
                .HasIndex(author => new { author.FirstName, author.LastName, author.BirthDate })
                .IsUnique();

            modelBuilder.Entity<Book>()
                .HasIndex(book => book.Isbn)
                .IsUnique();

            modelBuilder.Entity<BookImage>()
                .HasOne(bi => bi.Book)
                .WithMany(b => b.Images)
                .HasForeignKey(bi => bi.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BookImage>()
                .Property(bi => bi.ImagePath)
                .IsRequired()
                .HasMaxLength(500);

            modelBuilder.Entity<BookImage>()
                .Property(bi => bi.OriginalFileName)
                .HasMaxLength(255);

            modelBuilder.Entity<BookImage>()
                .HasIndex(bi => bi.BookId);

            modelBuilder.Entity<Category>()
                .Property(category => category.Name)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<Category>()
                .Property(category => category.Description)
                .HasMaxLength(250);

            modelBuilder.Entity<Category>()
                .HasIndex(category => category.Name)
                .IsUnique();

        }
    }
}
