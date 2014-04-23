using Phocalstream_Shared.Data;
using Phocalstream_Shared.Data.Model.Photo;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web;

namespace Phocalstream_Service.Data
{
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

            modelBuilder.Entity<Photo>().HasMany<Tag>(p => p.Tags).WithMany(t => t.Photos);

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

    public class ApplicationContextAdapter : IDbSetFactory, IDbContext
    {
        private readonly DbContext _context;

        public ApplicationContextAdapter(DbContext context)
        {
            _context = context;
        }

        #region IObjectContext Members

        public void SaveChanges()
        {
            _context.SaveChanges();
        }

        #endregion

        #region IObjectSetFactory Members

        public void Dispose()
        {
            _context.Dispose();
        }

        public DbSet<T> CreateDbSet<T>() where T : class
        {
            return _context.Set<T>();
        }

        public void ChangeObjectState(object entity, System.Data.Entity.EntityState state)
        {
            _context.Entry(entity).State = state;
        }

        #endregion
    }
}