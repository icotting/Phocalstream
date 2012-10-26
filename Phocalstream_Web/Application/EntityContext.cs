using Phocalstream_Web.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace Phocalstream_Web.Application
{
    public class EntityContext : DbContext
    {
        public EntityContext() : base("DbConnection") { }

        public DbSet<CameraSite> Sites { get; set; }
        public DbSet<Photo> Photos { get; set; }
        public DbSet<PhotoAnnotation> PhotoAnnotations { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Collection> Collections { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Photo>()
                .HasRequired<CameraSite>(p => p.Site)
                .WithMany(s => s.Photos)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<MetaDatum>()
                .HasOptional<Photo>(m => m.Photo)
                .WithMany(p => p.AdditionalExifProperties)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<Collection>().HasMany<Photo>(c => c.Photos).WithMany(p => p.FoundIn).Map(m =>
                {
                    m.MapLeftKey("CollectionId");
                    m.MapRightKey("PhotoId");
                    m.ToTable("CollectionPhotos");
                });
        }
    }
}