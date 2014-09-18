namespace Phocalstream_Service.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveGoogleID : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Users", "ProviderID", c => c.String());
            DropColumn("dbo.Users", "GoogleID");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Users", "GoogleID", c => c.String());
            DropColumn("dbo.Users", "ProviderID");
        }
    }
}
