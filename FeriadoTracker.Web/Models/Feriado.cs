using System.ComponentModel.DataAnnotations;

namespace FeriadoTracker.Web.Models;

public class Feriado
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    public DateOnly Data { get; set; }

    [MaxLength(50)]
    public string Tipo { get; set; } = "Nacional";

    /// <summary>Código IBGE do estado (SP = 35). Chave do estado; nulo nos nacionais.</summary>
    public int? CodigoUf { get; set; }

    /// <summary>Sigla do estado, só para exibição.</summary>
    [MaxLength(2)]
    public string? Uf { get; set; }

    /// <summary>Código IBGE do município (São Paulo = 3550308). Chave da cidade; nulo nos não municipais.</summary>
    public int? CodigoMunicipio { get; set; }

    /// <summary>Nome do município, só para exibição.</summary>
    [MaxLength(60)]
    public string? Municipio { get; set; }
}
