using System.Reflection.Emit;
using libraryMVC.Models;
using Microsoft.EntityFrameworkCore;

namespace libraryMVC.Data
{
    public class ApplicationDbContext : DbContext
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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

        }
    }
}
