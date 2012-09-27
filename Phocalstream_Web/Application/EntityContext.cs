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

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}