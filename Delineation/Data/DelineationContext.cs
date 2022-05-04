using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using CustomIdentity.Models;
using Delineation.Models;

namespace Delineation.Data
{
    public class DelineationContext : IdentityDbContext<User>//DbContext
    {
        public DbSet<D_Res> D_Reses { get; set; }
        public DbSet<D_Person> D_Persons { get; set; }
        public DbSet<D_Tc> D_Tces { get; set; }
        public DbSet<D_Act> D_Acts { get; set; }
        public DbSet<D_Agreement> D_Agreements { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<D_Position> Positions { get; set; }
        public DelineationContext(DbContextOptions<DelineationContext> options) : base(options)
        {
            //Database.EnsureCreated();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //  получение данных через представление
            modelBuilder.Entity<Unit>(p => { p.HasNoKey(); p.ToTable("sprpodr"); });
            modelBuilder.Entity<D_Act>().Property(p => p.Date).HasDefaultValueSql("'now'");
            modelBuilder.Entity<D_Act>().Property(u => u.State).HasDefaultValue(Stat.Edit);
            modelBuilder.Entity<D_Agreement>().Property(p => p.Accept).HasDefaultValue(false);
            modelBuilder.Entity<D_Agreement>().Property(p => p.Date).HasDefaultValueSql("'now'");
            base.OnModelCreating(modelBuilder);
            {
            }
        }
        public DbSet<Delineation.Models.D_Position> D_Position { get; set; }
    }
}
