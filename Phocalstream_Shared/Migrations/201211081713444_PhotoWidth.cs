namespace Phocalstream_Shared.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class PhotoWidth : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Photos", "Width", c => c.Int(nullable: false));
            AddColumn("dbo.Photos", "Height", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Photos", "Height");
            DropColumn("dbo.Photos", "Width");
        }
    }
}
