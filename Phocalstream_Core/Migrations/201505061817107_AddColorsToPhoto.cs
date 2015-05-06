namespace Phocalstream_Service.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddColorsToPhoto : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Photos", "Black", c => c.Single(nullable: false));
            AddColumn("dbo.Photos", "White", c => c.Single(nullable: false));
            AddColumn("dbo.Photos", "Red", c => c.Single(nullable: false));
            AddColumn("dbo.Photos", "Green", c => c.Single(nullable: false));
            AddColumn("dbo.Photos", "Blue", c => c.Single(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Photos", "Blue");
            DropColumn("dbo.Photos", "Green");
            DropColumn("dbo.Photos", "Red");
            DropColumn("dbo.Photos", "White");
            DropColumn("dbo.Photos", "Black");
        }
    }
}
