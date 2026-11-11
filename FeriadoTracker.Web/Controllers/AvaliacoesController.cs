using FeriadoTracker.Web.Data;
using FeriadoTracker.Web.Dtos;
using FeriadoTracker.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text;

namespace FeriadoTracker.Web.Controllers;

[ApiController]
[Route("api/avaliacoes")]
[EnableRateLimiting("avaliacao")]
public class AvaliacoesController(AppDbContext db, TimeProvider time) : ControllerBase
{
    public const int ComentarioMaximo = 500;

    /// <summary>
    /// Resposta cega: grava e devolve 204 sem corpo. Não existe GET público;
    /// a leitura é feita direto no banco.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar([FromBody] AvaliacaoDto dto, CancellationToken ct)
    {
        if (dto.Estrelas is < 1 or > 5)
        {
            return BadRequest(new { error = "Avaliação inválida." });
        }

        var comentario = Limpar(dto.Comentario);
        if (comentario is { Length: > ComentarioMaximo })
        {
            return BadRequest(new { error = "Avaliação inválida." });
        }

        db.Avaliacoes.Add(new Avaliacao
        {
            Estrelas = dto.Estrelas,
            Comentario = comentario,
            CriadoEmUtc = time.GetUtcNow().UtcDateTime
        });
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    /// <summary>
    /// Tira caracteres de controle invisíveis (mantém quebra de linha e tabulação)
    /// e aplica Trim. Vazio ou só espaços vira null.
    /// </summary>
    private static string? Limpar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;

        var sb = new StringBuilder(texto.Length);
        foreach (var c in texto)
        {
            if (!char.IsControl(c) || c is '\n' or '\r' or '\t') sb.Append(c);
        }

        var limpo = sb.ToString().Trim();
        return limpo.Length == 0 ? null : limpo;
    }
}
