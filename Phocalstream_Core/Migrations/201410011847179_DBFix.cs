namespace Phocalstream_Service.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class DBFix : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.CameraSites", "Owner_ID", "dbo.Users");
            DropIndex("dbo.CameraSites", new[] { "Owner_ID" });
            DropColumn("dbo.CameraSites", "Type");
            DropColumn("dbo.CameraSites", "Owner_ID");
        }
        
        public override void Down()
        {
            AddColumn("dbo.CameraSites", "Owner_ID", c => c.Int());
            AddColumn("dbo.CameraSites", "Type", c => c.Int(nullable: false));
            CreateIndex("dbo.CameraSites", "Owner_ID");
            AddForeignKey("dbo.CameraSites", "Owner_ID", "dbo.Users", "ID");
        }
    }
}
