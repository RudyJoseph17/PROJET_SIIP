using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Entities;
using System.Linq;

namespace Shared.Infrastructure
{
    public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Privilege> Privileges { get; set; }
        public DbSet<RolePrivilege> RolePrivileges { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            var boolConverter = new BoolToZeroOneConverter<short>();
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties().Where(p => p.ClrType == typeof(bool)))
                {
                    property.SetValueConverter(boolConverter);
                    property.SetColumnType("NUMBER(1)");
                }
            }

            builder.Entity<Privilege>()
                .HasKey(p => p.IdPrivilege);

            builder.Entity<RolePrivilege>()
                .HasKey(rp => new { rp.RoleId, rp.IdPrivilege });

            builder.Entity<RolePrivilege>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePrivileges)
                .HasForeignKey(rp => rp.RoleId);

            builder.Entity<RolePrivilege>()
                .HasOne(rp => rp.Privilege)
                .WithMany(p => p.RolePrivileges)
                .HasForeignKey(rp => rp.IdPrivilege);
        }
    }
}