using FeriadoTracker.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FeriadoTracker.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Feriado> Feriados { get; set; }
    public DbSet<PushSubscription> PushSubscriptions { get; set; }
    public DbSet<PushSubscriptionLocal> PushSubscriptionLocais { get; set; }
    public DbSet<NotificationLog> NotificationLogs { get; set; }
    public DbSet<Avaliacao> Avaliacoes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PushSubscription>()
            .HasIndex(p => p.Endpoint)
            .IsUnique();

        modelBuilder.Entity<PushSubscriptionLocal>()
            .HasKey(l => new { l.SubscriptionId, l.Codigo });

        modelBuilder.Entity<PushSubscription>()
            .HasMany(p => p.Locais)
            .WithOne()
            .HasForeignKey(u => u.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<NotificationLog>()
            .HasIndex(n => new { n.SubscriptionId, n.FeriadoId })
            .IsUnique();

        modelBuilder.Entity<NotificationLog>()
            .HasOne(n => n.Subscription)
            .WithMany()
            .HasForeignKey(n => n.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<NotificationLog>()
            .HasOne(n => n.Feriado)
            .WithMany()
            .HasForeignKey(n => n.FeriadoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Feriado>().HasData(
            new Feriado { Id = 1, Nome = "Natal", Data = new DateOnly(2025, 12, 25), Tipo = "Nacional" },
            new Feriado { Id = 2, Nome = "Confraternização Universal", Data = new DateOnly(2026, 01, 01), Tipo = "Nacional" },
            new Feriado { Id = 3, Nome = "Carnaval", Data = new DateOnly(2026, 02, 16), Tipo = "Nacional" },
            new Feriado { Id = 4, Nome = "Carnaval", Data = new DateOnly(2026, 02, 17), Tipo = "Nacional" },
            new Feriado { Id = 5, Nome = "Sexta-feira Santa", Data = new DateOnly(2026, 04, 03), Tipo = "Nacional" },
            new Feriado { Id = 6, Nome = "Tiradentes", Data = new DateOnly(2026, 04, 21), Tipo = "Nacional" },
            new Feriado { Id = 7, Nome = "Dia do Trabalho", Data = new DateOnly(2026, 05, 01), Tipo = "Nacional" },
            new Feriado { Id = 8, Nome = "Corpus Christi", Data = new DateOnly(2026, 06, 04), Tipo = "Nacional" },
            new Feriado { Id = 9, Nome = "Independência do Brasil", Data = new DateOnly(2026, 09, 07), Tipo = "Nacional" },
            new Feriado { Id = 10, Nome = "Nossa Senhora Aparecida", Data = new DateOnly(2026, 10, 12), Tipo = "Nacional" },
            new Feriado { Id = 11, Nome = "Finados", Data = new DateOnly(2026, 11, 02), Tipo = "Nacional" },
            new Feriado { Id = 12, Nome = "Proclamação da República", Data = new DateOnly(2026, 11, 15), Tipo = "Nacional" },
            new Feriado { Id = 13, Nome = "Dia Nacional da Consciência Negra", Data = new DateOnly(2026, 11, 20), Tipo = "Nacional" },
            new Feriado { Id = 14, Nome = "Natal", Data = new DateOnly(2026, 12, 25), Tipo = "Nacional" },
            new Feriado { Id = 15, Nome = "Confraternização Universal", Data = new DateOnly(2027, 01, 01), Tipo = "Nacional" },
            new Feriado { Id = 16, Nome = "Carnaval", Data = new DateOnly(2027, 02, 08), Tipo = "Nacional" },
            new Feriado { Id = 17, Nome = "Carnaval", Data = new DateOnly(2027, 02, 09), Tipo = "Nacional" },
            new Feriado { Id = 18, Nome = "Sexta-feira Santa", Data = new DateOnly(2027, 03, 26), Tipo = "Nacional" },
            new Feriado { Id = 19, Nome = "Tiradentes", Data = new DateOnly(2027, 04, 21), Tipo = "Nacional" },
            new Feriado { Id = 20, Nome = "Dia do Trabalho", Data = new DateOnly(2027, 05, 01), Tipo = "Nacional" },
            new Feriado { Id = 21, Nome = "Corpus Christi", Data = new DateOnly(2027, 05, 27), Tipo = "Nacional" },
            new Feriado { Id = 22, Nome = "Independência do Brasil", Data = new DateOnly(2027, 09, 07), Tipo = "Nacional" },
            new Feriado { Id = 23, Nome = "Nossa Senhora Aparecida", Data = new DateOnly(2027, 10, 12), Tipo = "Nacional" },
            new Feriado { Id = 24, Nome = "Finados", Data = new DateOnly(2027, 11, 02), Tipo = "Nacional" },
            new Feriado { Id = 25, Nome = "Proclamação da República", Data = new DateOnly(2027, 11, 15), Tipo = "Nacional" },
            new Feriado { Id = 26, Nome = "Dia Nacional da Consciência Negra", Data = new DateOnly(2027, 11, 20), Tipo = "Nacional" },
            new Feriado { Id = 27, Nome = "Natal", Data = new DateOnly(2027, 12, 25), Tipo = "Nacional" },

            // Estaduais de 2027: só os confirmados em lei ou decreto estadual. Os outros estados entram em levas.
            // CodigoUf = código IBGE do estado (chave); Uf = sigla, só para exibição.
            // Móvel: segunda-feira, 8 dias depois do domingo de Páscoa (28/03/2027).
            new Feriado { Id = 28, Nome = "Nossa Senhora da Penha", Data = new DateOnly(2027, 04, 05), Tipo = "Estadual", CodigoUf = 32, Uf = "ES" },
            new Feriado { Id = 29, Nome = "Adesão do Pará à Independência", Data = new DateOnly(2027, 08, 15), Tipo = "Estadual", CodigoUf = 15, Uf = "PA" },
            new Feriado { Id = 30, Nome = "Revolução Pernambucana", Data = new DateOnly(2027, 03, 06), Tipo = "Estadual", CodigoUf = 26, Uf = "PE" },
            // 11/08 cai numa quarta em 2027; a lei estadual transfere a comemoração para o domingo seguinte.
            new Feriado { Id = 31, Nome = "Dia de Santa Catarina", Data = new DateOnly(2027, 08, 15), Tipo = "Estadual", CodigoUf = 42, Uf = "SC" },
            // Lei estadual 9.497/1997.
            new Feriado { Id = 32, Nome = "Revolução Constitucionalista", Data = new DateOnly(2027, 07, 09), Tipo = "Estadual", CodigoUf = 35, Uf = "SP" },

            // Municipais: valem só na cidade indicada, nunca no estado inteiro. Fonte: feriados-brasil (municipal, codigo_ibge).
            // Lei Municipal 14.485/2007.
            new Feriado { Id = 33, Nome = "Aniversário de São Paulo", Data = new DateOnly(2027, 01, 25), Tipo = "Municipal", CodigoUf = 35, Uf = "SP", CodigoMunicipio = 3550308, Municipio = "São Paulo" }
        );
    }
}
