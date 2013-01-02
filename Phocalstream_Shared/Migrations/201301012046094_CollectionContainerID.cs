namespace Phocalstream_Shared.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CollectionContainerID : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Collections", "ContainerID", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Collections", "ContainerID");
        }
    }
}
