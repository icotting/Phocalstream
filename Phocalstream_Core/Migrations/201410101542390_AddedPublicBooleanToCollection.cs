namespace Phocalstream_Service.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddedPublicBooleanToCollection : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Collections", "Public", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Collections", "Public");
        }
    }
}
