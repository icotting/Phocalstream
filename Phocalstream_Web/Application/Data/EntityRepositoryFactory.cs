using Phocalstream_Shared.Data;
using Phocalstream_Shared.Data.Model.Photo;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace Phocalstream_Web.Application.Data
{
    public class EntityRepositoryFactory : IEntityRepositoryFactory
    {
        private ApplicationContext _dbContext;

        public EntityRepositoryFactory()
        {
            _dbContext = new ApplicationContext();
        }

        public void SaveChanges()
        {
            _dbContext.SaveChanges();
        }

        public IEntityRepository<T> GetRepository<T>() where T : class
        {
            return null;
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }

    public class ApplicationContext : DbContext
    {
        public ApplicationContext()
            : base("DbConnection") 
        { 
            
        }

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