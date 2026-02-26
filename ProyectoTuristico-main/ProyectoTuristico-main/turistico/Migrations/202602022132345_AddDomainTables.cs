namespace turistico.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddDomainTables : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AccionAuditorias",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        AdminUserId = c.String(nullable: false, maxLength: 128),
                        EntidadAfectada = c.String(nullable: false, maxLength: 100),
                        IdEntidad = c.Int(nullable: false),
                        TipoAccion = c.String(maxLength: 100),
                        Fecha = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.AdminUserId, cascadeDelete: true)
                .Index(t => t.AdminUserId);
            
            CreateTable(
                "dbo.Categorias",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Nombre = c.String(nullable: false, maxLength: 100),
                        Descripcion = c.String(maxLength: 300),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Lugars",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CategoriaId = c.Int(nullable: false),
                        Nombre = c.String(nullable: false, maxLength: 150),
                        Descripcion = c.String(maxLength: 500),
                        Latitud = c.Decimal(precision: 18, scale: 2),
                        Longitud = c.Decimal(precision: 18, scale: 2),
                        Direccion = c.String(maxLength: 250),
                        Telefono = c.String(maxLength: 50),
                        Horario = c.String(maxLength: 200),
                        SitioWeb = c.String(maxLength: 200),
                        Estado = c.String(maxLength: 50),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Categorias", t => t.CategoriaId, cascadeDelete: true)
                .Index(t => t.CategoriaId);
            
            CreateTable(
                "dbo.ImagenLugars",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        LugarId = c.Int(nullable: false),
                        UrlImagen = c.String(nullable: false, maxLength: 255),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Lugars", t => t.LugarId, cascadeDelete: true)
                .Index(t => t.LugarId);
            
            CreateTable(
                "dbo.CategoriaEventoes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Nombre = c.String(nullable: false, maxLength: 100),
                        Descripcion = c.String(maxLength: 300),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Eventoes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        LugarId = c.Int(nullable: false),
                        CategoriaEventoId = c.Int(nullable: false),
                        Nombre = c.String(nullable: false, maxLength: 150),
                        Descripcion = c.String(maxLength: 500),
                        FechaInicio = c.DateTime(),
                        FechaFin = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CategoriaEventoes", t => t.CategoriaEventoId, cascadeDelete: true)
                .ForeignKey("dbo.Lugars", t => t.LugarId, cascadeDelete: true)
                .Index(t => t.LugarId)
                .Index(t => t.CategoriaEventoId);
            
            CreateTable(
                "dbo.Comercios",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        LugarId = c.Int(nullable: false),
                        Nombre = c.String(nullable: false, maxLength: 150),
                        Descripcion = c.String(maxLength: 500),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Lugars", t => t.LugarId, cascadeDelete: true)
                .Index(t => t.LugarId);
            
            CreateTable(
                "dbo.ComercioReguladoes",
                c => new
                    {
                        ComercioId = c.Int(nullable: false),
                        NumeroPatente = c.String(nullable: false, maxLength: 100),
                        FechaVencimiento = c.DateTime(),
                        EstadoValidacion = c.String(maxLength: 50),
                    })
                .PrimaryKey(t => t.ComercioId)
                .ForeignKey("dbo.Comercios", t => t.ComercioId)
                .Index(t => t.ComercioId);
            
            CreateTable(
                "dbo.ProductoServicios",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ComercioId = c.Int(nullable: false),
                        Nombre = c.String(maxLength: 150),
                        Precio = c.Decimal(precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Comercios", t => t.ComercioId, cascadeDelete: true)
                .Index(t => t.ComercioId);
            
            CreateTable(
                "dbo.Favoritoes",
                c => new
                    {
                        UserId = c.String(nullable: false, maxLength: 128),
                        LugarId = c.Int(nullable: false),
                        FechaGuardado = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => new { t.UserId, t.LugarId })
                .ForeignKey("dbo.Lugars", t => t.LugarId, cascadeDelete: true)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId)
                .Index(t => t.LugarId);
            
            CreateTable(
                "dbo.Resenas",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.String(nullable: false, maxLength: 128),
                        LugarId = c.Int(nullable: false),
                        Calificacion = c.Int(nullable: false),
                        Comentario = c.String(maxLength: 500),
                        Fecha = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Lugars", t => t.LugarId, cascadeDelete: true)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId)
                .Index(t => t.LugarId);
            
            CreateTable(
                "dbo.Reservas",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.String(nullable: false, maxLength: 128),
                        LugarId = c.Int(nullable: false),
                        FechaReserva = c.DateTime(nullable: false),
                        NumeroPersonas = c.Int(nullable: false),
                        Estado = c.String(maxLength: 50),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Lugars", t => t.LugarId, cascadeDelete: true)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId)
                .Index(t => t.LugarId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Reservas", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.Reservas", "LugarId", "dbo.Lugars");
            DropForeignKey("dbo.Resenas", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.Resenas", "LugarId", "dbo.Lugars");
            DropForeignKey("dbo.Favoritoes", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.Favoritoes", "LugarId", "dbo.Lugars");
            DropForeignKey("dbo.ProductoServicios", "ComercioId", "dbo.Comercios");
            DropForeignKey("dbo.Comercios", "LugarId", "dbo.Lugars");
            DropForeignKey("dbo.ComercioReguladoes", "ComercioId", "dbo.Comercios");
            DropForeignKey("dbo.Eventoes", "LugarId", "dbo.Lugars");
            DropForeignKey("dbo.Eventoes", "CategoriaEventoId", "dbo.CategoriaEventoes");
            DropForeignKey("dbo.ImagenLugars", "LugarId", "dbo.Lugars");
            DropForeignKey("dbo.Lugars", "CategoriaId", "dbo.Categorias");
            DropForeignKey("dbo.AccionAuditorias", "AdminUserId", "dbo.AspNetUsers");
            DropIndex("dbo.Reservas", new[] { "LugarId" });
            DropIndex("dbo.Reservas", new[] { "UserId" });
            DropIndex("dbo.Resenas", new[] { "LugarId" });
            DropIndex("dbo.Resenas", new[] { "UserId" });
            DropIndex("dbo.Favoritoes", new[] { "LugarId" });
            DropIndex("dbo.Favoritoes", new[] { "UserId" });
            DropIndex("dbo.ProductoServicios", new[] { "ComercioId" });
            DropIndex("dbo.ComercioReguladoes", new[] { "ComercioId" });
            DropIndex("dbo.Comercios", new[] { "LugarId" });
            DropIndex("dbo.Eventoes", new[] { "CategoriaEventoId" });
            DropIndex("dbo.Eventoes", new[] { "LugarId" });
            DropIndex("dbo.ImagenLugars", new[] { "LugarId" });
            DropIndex("dbo.Lugars", new[] { "CategoriaId" });
            DropIndex("dbo.AccionAuditorias", new[] { "AdminUserId" });
            DropTable("dbo.Reservas");
            DropTable("dbo.Resenas");
            DropTable("dbo.Favoritoes");
            DropTable("dbo.ProductoServicios");
            DropTable("dbo.ComercioReguladoes");
            DropTable("dbo.Comercios");
            DropTable("dbo.Eventoes");
            DropTable("dbo.CategoriaEventoes");
            DropTable("dbo.ImagenLugars");
            DropTable("dbo.Lugars");
            DropTable("dbo.Categorias");
            DropTable("dbo.AccionAuditorias");
        }
    }
}
