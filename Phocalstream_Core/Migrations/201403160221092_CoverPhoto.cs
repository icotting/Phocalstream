namespace Phocalstream_Service.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CoverPhoto : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Collections", "CoverPhoto_ID", c => c.Long());
            CreateIndex("dbo.Collections", "CoverPhoto_ID");
            AddForeignKey("dbo.Collections", "CoverPhoto_ID", "dbo.Photos", "ID");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Collections", "CoverPhoto_ID", "dbo.Photos");
            DropIndex("dbo.Collections", new[] { "CoverPhoto_ID" });
            DropColumn("dbo.Collections", "CoverPhoto_ID");
        }
    }
}
