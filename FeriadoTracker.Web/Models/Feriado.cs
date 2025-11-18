using System.ComponentModel.DataAnnotations;

namespace FeriadoTracker.Web.Models;

public class Feriado
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    public DateTime Data { get; set; }

    [MaxLength(50)]
    public string Tipo { get; set; } = "Nacional";
}
