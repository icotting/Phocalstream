namespace Phocalstream_Shared.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CameraSiteCountyFips : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CameraSites", "CountyFips", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.CameraSites", "CountyFips");
        }
    }
}
