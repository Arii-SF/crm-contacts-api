using Microsoft.EntityFrameworkCore;
using CrmContactsApi.Models;

namespace CrmContactsApi.Models
{
    public class CrmDbContext : DbContext
    {
        public CrmDbContext(DbContextOptions<CrmDbContext> options) : base(options)
        {
        }

        public DbSet<Contacto> Contactos { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de la tabla contactos
            modelBuilder.Entity<Contacto>(entity =>
            {
                entity.ToTable("contactos");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.Nombre)
                    .HasColumnName("nombre")
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.Apellido)
                    .HasColumnName("apellido")
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.Telefono)
                    .HasColumnName("telefono")
                    .HasMaxLength(15);

                entity.Property(e => e.Email)
                    .HasColumnName("email")
                    .HasMaxLength(100);

                entity.Property(e => e.Dpi)
                    .HasColumnName("dpi")
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(e => e.Nit)
                    .HasColumnName("nit")
                    .HasMaxLength(20);

                entity.Property(e => e.Direccion)
                    .HasColumnName("direccion")
                    .HasMaxLength(255);

                entity.Property(e => e.Zona)
                    .HasColumnName("zona")
                    .HasMaxLength(10);

                entity.Property(e => e.Municipio)
                    .HasColumnName("municipio")
                    .HasMaxLength(50);

                entity.Property(e => e.Departamento)
                    .HasColumnName("departamento")
                    .HasMaxLength(50);

                entity.Property(e => e.DiasCredito)
                    .HasColumnName("dias_credito")
                    .HasDefaultValue(0);

                entity.Property(e => e.LimiteCredito)
                    .HasColumnName("limite_credito")
                    .HasColumnType("decimal(10,2)")
                    .HasDefaultValue(0.00m);

                entity.Property(e => e.Categoria)
                    .HasColumnName("categoria")
                    .HasMaxLength(50);

                entity.Property(e => e.Subcategoria)
                    .HasColumnName("subcategoria")
                    .HasMaxLength(50);

                entity.Property(e => e.FechaCreacion)
                    .HasColumnName("fecha_creacion")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.FechaActualizacion)
                    .HasColumnName("fecha_actualizacion")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");

                entity.Property(e => e.UsuarioCreacion)
                    .HasColumnName("usuario_creacion");

                entity.Property(e => e.UsuarioActualizacion)
                    .HasColumnName("usuario_actualizacion");

                entity.Property(e => e.Activo)
                    .HasColumnName("activo")
                    .HasDefaultValue(true);

                // Índice único para DPI
                entity.HasIndex(e => e.Dpi)
                    .IsUnique()
                    .HasDatabaseName("IX_contactos_dpi");
            });

           
        }
    }
}