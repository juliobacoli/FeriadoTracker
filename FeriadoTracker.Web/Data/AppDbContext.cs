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
            new Feriado { Id = 2, Nome = "Confraternização Universal", Data = new DateTime(2026, 01, 01), Tipo = "Nacional" },
            new Feriado { Id = 3, Nome = "Carnaval", Data = new DateTime(2026, 02, 16), Tipo = "Nacional" },
            new Feriado { Id = 4, Nome = "Carnaval", Data = new DateTime(2026, 02, 17), Tipo = "Nacional" },
            new Feriado { Id = 5, Nome = "Sexta-feira Santa", Data = new DateTime(2026, 04, 03), Tipo = "Nacional" },
            new Feriado { Id = 6, Nome = "Tiradentes", Data = new DateTime(2026, 04, 21), Tipo = "Nacional" },
            new Feriado { Id = 7, Nome = "Dia do Trabalho", Data = new DateTime(2026, 05, 01), Tipo = "Nacional" },
            new Feriado { Id = 8, Nome = "Corpus Christi", Data = new DateTime(2026, 06, 04), Tipo = "Nacional" },
            new Feriado { Id = 9, Nome = "Independência do Brasil", Data = new DateTime(2026, 09, 07), Tipo = "Nacional" },
            new Feriado { Id = 10, Nome = "Nossa Senhora Aparecida", Data = new DateTime(2026, 10, 12), Tipo = "Nacional" },
            new Feriado { Id = 11, Nome = "Finados", Data = new DateTime(2026, 11, 02), Tipo = "Nacional" },
            new Feriado { Id = 12, Nome = "Proclamação da República", Data = new DateTime(2026, 11, 15), Tipo = "Nacional" },
            new Feriado { Id = 13, Nome = "Dia Nacional da Consciência Negra", Data = new DateTime(2026, 11, 20), Tipo = "Nacional" },
            new Feriado { Id = 14, Nome = "Natal", Data = new DateTime(2026, 12, 25), Tipo = "Nacional" }
        );
    }
}
