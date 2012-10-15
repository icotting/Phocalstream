namespace Phocalstream_Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CameraSites",
                c => new
                    {
                        ID = c.Long(nullable: false, identity: true),
                        Name = c.String(),
                        Latitude = c.Double(nullable: false),
                        Longitude = c.Double(nullable: false),
                        ContainerID = c.String(),
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
                        Photo_ID = c.Long(),
                        Author_ID = c.Long(),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Photos", t => t.Photo_ID)
                .ForeignKey("dbo.Users", t => t.Author_ID)
                .Index(t => t.Photo_ID)
                .Index(t => t.Author_ID);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        ID = c.Long(nullable: false, identity: true),
                        GoogleID = c.String(),
                        FirstName = c.String(),
                        LastName = c.String(),
                    })
                .PrimaryKey(t => t.ID);
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.PhotoAnnotations", new[] { "Author_ID" });
            DropIndex("dbo.PhotoAnnotations", new[] { "Photo_ID" });
            DropIndex("dbo.MetaDatums", new[] { "Photo_ID" });
            DropIndex("dbo.Photos", new[] { "Site_ID" });
            DropForeignKey("dbo.PhotoAnnotations", "Author_ID", "dbo.Users");
            DropForeignKey("dbo.PhotoAnnotations", "Photo_ID", "dbo.Photos");
            DropForeignKey("dbo.MetaDatums", "Photo_ID", "dbo.Photos");
            DropForeignKey("dbo.Photos", "Site_ID", "dbo.CameraSites");
            DropTable("dbo.Users");
            DropTable("dbo.PhotoAnnotations");
            DropTable("dbo.MetaDatums");
            DropTable("dbo.Photos");
            DropTable("dbo.CameraSites");
        }
    }
}
