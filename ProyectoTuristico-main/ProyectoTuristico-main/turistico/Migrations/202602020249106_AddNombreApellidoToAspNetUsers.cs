namespace turistico.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddNombreApellidoToAspNetUsers : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "Nombre", c => c.String(maxLength: 100));
            AddColumn("dbo.AspNetUsers", "Apellido", c => c.String(maxLength: 100));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "Apellido");
            DropColumn("dbo.AspNetUsers", "Nombre");
        }
    }
}
