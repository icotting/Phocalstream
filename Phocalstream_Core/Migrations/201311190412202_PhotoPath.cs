namespace Phocalstream_Service.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class PhotoPath : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Collections",
                c => new
                    {
                        ID = c.Long(nullable: false, identity: true),
                        Name = c.String(),
                        ContainerID = c.String(),
                        Type = c.Int(nullable: false),
                        Status = c.Int(nullable: false),
                        Owner_ID = c.Int(),
                        Site_ID = c.Long(),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Users", t => t.Owner_ID)
                .ForeignKey("dbo.CameraSites", t => t.Site_ID)
                .Index(t => t.Owner_ID)
                .Index(t => t.Site_ID);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        GoogleID = c.String(),
                        FirstName = c.String(nullable: false),
                        LastName = c.String(nullable: false),
                        Role = c.Int(nullable: false),
                        EmailAddress = c.String(nullable: false),
                        Organization = c.String(),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.Photos",
                c => new
                    {
                        ID = c.Long(nullable: false, identity: true),
                        BlobID = c.String(),
                        Captured = c.DateTime(nullable: false),
                        Latitude = c.Double(nullable: false),
                        Longitude = c.Double(nullable: false),
                        Altitude = c.Double(nullable: false),
                        UserComments = c.String(),
                        ExposureTime = c.Double(nullable: false),
                        ShutterSpeed = c.String(),
                        MaxAperture = c.Double(nullable: false),
                        FocalLength = c.Double(nullable: false),
                        Flash = c.Boolean(nullable: false),
                        ISO = c.Int(nullable: false),
                        Width = c.Int(nullable: false),
                        Height = c.Int(nullable: false),
                        FileName = c.String(),
                        Site_ID = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.CameraSites", t => t.Site_ID, cascadeDelete: true)
                .Index(t => t.Site_ID);
            
            CreateTable(
                "dbo.MetaDatums",
                c => new
                    {
                        ID = c.Long(nullable: false, identity: true),
                        Type = c.String(),
                        Name = c.String(),
                        Value = c.String(),
                        Photo_ID = c.Long(),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Photos", t => t.Photo_ID, cascadeDelete: true)
                .Index(t => t.Photo_ID);
            
            CreateTable(
                "dbo.PhotoAnnotations",
                c => new
                    {
                        ID = c.Long(nullable: false, identity: true),
                        Text = c.String(),
                        Top = c.Double(nullable: false),
                        Left = c.Double(nullable: false),
                        Bottom = c.Double(nullable: false),
                        Right = c.Double(nullable: false),
                        Added = c.DateTime(nullable: false),
                        Author_ID = c.Int(),
                        Photo_ID = c.Long(),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Users", t => t.Author_ID)
                .ForeignKey("dbo.Photos", t => t.Photo_ID)
                .Index(t => t.Author_ID)
                .Index(t => t.Photo_ID);
            
            CreateTable(
                "dbo.CameraSites",
                c => new
                    {
                        ID = c.Long(nullable: false, identity: true),
                        Name = c.String(),
                        Latitude = c.Double(nullable: false),
                        Longitude = c.Double(nullable: false),
                        CountyFips = c.Int(nullable: false),
                        ContainerID = c.String(),
                        DirectoryName = c.String(),
                    })
                .PrimaryKey(t => t.ID);
            
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
            DropForeignKey("dbo.Collections", "Site_ID", "dbo.CameraSites");
            DropForeignKey("dbo.CollectionPhotos", "PhotoId", "dbo.Photos");
            DropForeignKey("dbo.CollectionPhotos", "CollectionId", "dbo.Collections");
            DropForeignKey("dbo.Photos", "Site_ID", "dbo.CameraSites");
            DropForeignKey("dbo.PhotoAnnotations", "Photo_ID", "dbo.Photos");
            DropForeignKey("dbo.PhotoAnnotations", "Author_ID", "dbo.Users");
            DropForeignKey("dbo.MetaDatums", "Photo_ID", "dbo.Photos");
            DropForeignKey("dbo.Collections", "Owner_ID", "dbo.Users");
            DropIndex("dbo.Collections", new[] { "Site_ID" });
            DropIndex("dbo.CollectionPhotos", new[] { "PhotoId" });
            DropIndex("dbo.CollectionPhotos", new[] { "CollectionId" });
            DropIndex("dbo.Photos", new[] { "Site_ID" });
            DropIndex("dbo.PhotoAnnotations", new[] { "Photo_ID" });
            DropIndex("dbo.PhotoAnnotations", new[] { "Author_ID" });
            DropIndex("dbo.MetaDatums", new[] { "Photo_ID" });
            DropIndex("dbo.Collections", new[] { "Owner_ID" });
            DropTable("dbo.CollectionPhotos");
            DropTable("dbo.CameraSites");
            DropTable("dbo.PhotoAnnotations");
            DropTable("dbo.MetaDatums");
            DropTable("dbo.Photos");
            DropTable("dbo.Users");
            DropTable("dbo.Collections");
        }
    }
}
