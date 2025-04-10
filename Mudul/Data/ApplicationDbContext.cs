using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Mudul.EntityModels;

namespace Mudul.Data
{
    // Este contexto solo manejará las tablas relacionadas con Identity
    public class ApplicationDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // No incluimos las entidades del dominio, ya que se manejan en DefaultdbContext

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            // Configurar las tablas de Identity
            builder.Entity<IdentityUser>(entity =>
            {
                entity.ToTable("AspNetUsers");
            });

            builder.Entity<IdentityRole>(entity =>
            {
                entity.ToTable("AspNetRoles");
            });

            builder.Entity<IdentityUserRole<string>>(entity =>
            {
                entity.ToTable("AspNetUserRoles");
            });

            builder.Entity<IdentityUserLogin<string>>(entity =>
            {
                entity.ToTable("AspNetUserLogins");
            });

            builder.Entity<IdentityUserClaim<string>>(entity =>
            {
                entity.ToTable("AspNetUserClaims");
            });

            builder.Entity<IdentityRoleClaim<string>>(entity =>
            {
                entity.ToTable("AspNetRoleClaims");
            });

            builder.Entity<IdentityUserToken<string>>(entity =>
            {
                entity.ToTable("AspNetUserTokens");
            });

            // Ignorar las entidades personalizadas que tienen el mismo nombre que las de Identity
            builder.Ignore<AspNetUser>();
            builder.Ignore<AspNetRole>();
            builder.Ignore<AspNetUserRole>();
            builder.Ignore<AspNetUserLogin>();
            builder.Ignore<AspNetUserClaim>();
            builder.Ignore<AspNetRoleClaim>();
            builder.Ignore<AspNetUserToken>();
        }
    }
}
