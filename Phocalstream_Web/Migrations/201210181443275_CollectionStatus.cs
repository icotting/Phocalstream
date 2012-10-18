namespace Phocalstream_Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CollectionStatus : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Collections", "Status", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Collections", "Status");
        }
    }
}
