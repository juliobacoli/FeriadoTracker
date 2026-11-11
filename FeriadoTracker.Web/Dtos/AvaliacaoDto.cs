using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace FeriadoTracker.Web.Dtos;

/// <summary>
/// Classe com propriedades, e não record posicional: com NullabilityInfoContextSupport
/// desligado no .csproj, o MVC pode tratar <c>string?</c> como obrigatória e devolver 400
/// para avaliação sem comentário. ValidateNever tira a validação implícita do comentário;
/// o controller valida tamanho e conteúdo explicitamente.
/// </summary>
public class AvaliacaoDto
{
    public int Estrelas { get; set; }

    [ValidateNever]
    public string? Comentario { get; set; }
}
