namespace Phocalstream_Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddCollection : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Collections",
                c => new
                    {
                        ID = c.Long(nullable: false, identity: true),
                        Name = c.String(),
                        Type = c.Int(nullable: false),
                        Site_ID = c.Long(),
                        Owner_ID = c.Long(),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.CameraSites", t => t.Site_ID)
                .ForeignKey("dbo.Users", t => t.Owner_ID)
                .Index(t => t.Site_ID)
                .Index(t => t.Owner_ID);
            
            CreateTable(
                "dbo.CollectionPhotos",
                c => new
                    {
                        CollectionId = c.Long(nullable: false),
                        PhotoId = c.Long(nullable: false),
                    })
                .PrimaryKey(t => new { t.CollectionId, t.PhotoId })
                .ForeignKey("dbo.Collections", t => t.CollectionId, cascadeDelete: true)
                .ForeignKey("dbo.Photos", t => t.PhotoId, cascadeDelete: true)
                .Index(t => t.CollectionId)
                .Index(t => t.PhotoId);
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.CollectionPhotos", new[] { "PhotoId" });
            DropIndex("dbo.CollectionPhotos", new[] { "CollectionId" });
            DropIndex("dbo.Collections", new[] { "Owner_ID" });
            DropIndex("dbo.Collections", new[] { "Site_ID" });
            DropForeignKey("dbo.CollectionPhotos", "PhotoId", "dbo.Photos");
            DropForeignKey("dbo.CollectionPhotos", "CollectionId", "dbo.Collections");
            DropForeignKey("dbo.Collections", "Owner_ID", "dbo.Users");
            DropForeignKey("dbo.Collections", "Site_ID", "dbo.CameraSites");
            DropTable("dbo.CollectionPhotos");
            DropTable("dbo.Collections");
        }
    }
}
