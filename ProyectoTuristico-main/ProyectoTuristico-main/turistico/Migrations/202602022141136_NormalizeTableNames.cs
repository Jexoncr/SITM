namespace turistico.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class NormalizeTableNames : DbMigration
    {
        public override void Up()
        {
            RenameTable(name: "dbo.AccionAuditorias", newName: "AccionesAuditoria");
            RenameTable(name: "dbo.Lugars", newName: "Lugares");
            RenameTable(name: "dbo.ImagenLugars", newName: "ImagenesLugar");
            RenameTable(name: "dbo.CategoriaEventoes", newName: "CategoriasEvento");
            RenameTable(name: "dbo.Eventoes", newName: "Eventos");
            RenameTable(name: "dbo.ComercioReguladoes", newName: "ComerciosRegulados");
            RenameTable(name: "dbo.ProductoServicios", newName: "ProductosServicio");
            RenameTable(name: "dbo.Favoritoes", newName: "Favoritos");
        }
        
        public override void Down()
        {
            RenameTable(name: "dbo.Favoritos", newName: "Favoritoes");
            RenameTable(name: "dbo.ProductosServicio", newName: "ProductoServicios");
            RenameTable(name: "dbo.ComerciosRegulados", newName: "ComercioReguladoes");
            RenameTable(name: "dbo.Eventos", newName: "Eventoes");
            RenameTable(name: "dbo.CategoriasEvento", newName: "CategoriaEventoes");
            RenameTable(name: "dbo.ImagenesLugar", newName: "ImagenLugars");
            RenameTable(name: "dbo.Lugares", newName: "Lugars");
            RenameTable(name: "dbo.AccionesAuditoria", newName: "AccionAuditorias");
        }
    }
}
