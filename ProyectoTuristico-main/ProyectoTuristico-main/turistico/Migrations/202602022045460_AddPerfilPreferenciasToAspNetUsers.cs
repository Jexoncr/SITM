namespace turistico.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPerfilPreferenciasToAspNetUsers : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "Canton", c => c.String(maxLength: 150));
            AddColumn("dbo.AspNetUsers", "PrefEventos", c => c.Boolean(nullable: false));
            AddColumn("dbo.AspNetUsers", "PrefEcologico", c => c.Boolean(nullable: false));
            AddColumn("dbo.AspNetUsers", "PrefGastronomia", c => c.Boolean(nullable: false));
            AddColumn("dbo.AspNetUsers", "PrefAventura", c => c.Boolean(nullable: false));
            AddColumn("dbo.AspNetUsers", "TipoNotificacion", c => c.String(maxLength: 50));
            AddColumn("dbo.AspNetUsers", "Idioma", c => c.String(maxLength: 50));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "Idioma");
            DropColumn("dbo.AspNetUsers", "TipoNotificacion");
            DropColumn("dbo.AspNetUsers", "PrefAventura");
            DropColumn("dbo.AspNetUsers", "PrefGastronomia");
            DropColumn("dbo.AspNetUsers", "PrefEcologico");
            DropColumn("dbo.AspNetUsers", "PrefEventos");
            DropColumn("dbo.AspNetUsers", "Canton");
        }
    }
}
