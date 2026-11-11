using System.ComponentModel.DataAnnotations;

namespace FeriadoTracker.Web.Models;

/// <summary>
/// Avaliação manual da tela. Guarda só a nota, o comentário opcional e a hora:
/// nenhum dado do visitante (IP, User-Agent, identificador) é persistido.
/// </summary>
public class Avaliacao
{
    public int Id { get; set; }

    [Range(1, 5)]
    public int Estrelas { get; set; }

    [MaxLength(500)]
    public string? Comentario { get; set; }

    /// <summary>Preenchido no servidor; nunca vem do navegador.</summary>
    public DateTime CriadoEmUtc { get; set; }
}
