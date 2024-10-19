using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;
using Raftel.Core.Storage;

namespace Raftel.Data.DbContexts.BlobStorage
{
    public class BlobDbContext : DbContext
    {
        public BlobDbContext(DbContextOptions<BlobDbContext> options)
            : base(options)
        {
        }

        public DbSet<Folder> Folders { get; init; }
        public DbSet<Document> Documents { get; init; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Folder>()
                .ToCollection("folders")
                .HasKey(f => f.Id);

            modelBuilder.Entity<Folder>()
                .Property(f => f.Name)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<Folder>()
                .HasMany(p => p.SubFolders)
                .WithOne()
                .HasForeignKey(p => p.Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Folder>()
                .HasMany(p => p.Documents)
                .WithOne()
                .HasForeignKey("FolderId")
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Document>()
                .ToCollection("documents")
                .HasKey(d => d.Id);

            modelBuilder.Entity<Document>()
                .Property(d => d.Name)
                .IsRequired()
                .HasMaxLength(255);

            modelBuilder.Entity<Document>()
                .Property(d => d.Extension)
                .IsRequired()
                .HasMaxLength(10);

            modelBuilder.Entity<Document>()
                .Property(d => d.Size)
                .IsRequired();

            modelBuilder.Entity<Document>()
                .Property(d => d.CreationDate)
                .IsRequired();

            modelBuilder.Entity<Document>()
                .Property(d => d.BlobFileId)
                .IsRequired();
        }
    }
}