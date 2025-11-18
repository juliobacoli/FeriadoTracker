using FeriadoTracker.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FeriadoTracker.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Feriado> Feriados { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Feriado>().HasData(
            new Feriado { Id = 1, Nome = "Natal", Data = new DateTime(2025, 12, 25), Tipo = "Nacional" },
            new Feriado { Id = 2, Nome = "Ano Novo", Data = new DateTime(2026, 01, 01), Tipo = "Nacional" },
            new Feriado { Id = 3, Nome = "Tiradentes", Data = new DateTime(2026, 04, 21), Tipo = "Nacional" }
        );
    }
}
