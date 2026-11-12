using System.ComponentModel.DataAnnotations;
using FeriadoTracker.Web.Models;

namespace FeriadoTracker.Web.Tests;

public class AvaliacaoTests
{
    private static List<ValidationResult> Validar(Avaliacao avaliacao)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(avaliacao, new ValidationContext(avaliacao), resultados, validateAllProperties: true);
        return resultados;
    }

    [Fact]
    public void NovaAvaliacao_TemValoresPadrao()
    {
        var avaliacao = new Avaliacao();

        Assert.Equal(0, avaliacao.Id);
        Assert.Equal(0, avaliacao.Estrelas);
        Assert.Null(avaliacao.Comentario);
        Assert.Equal(default, avaliacao.CriadoEmUtc);
    }

    [Fact]
    public void Propriedades_GuardamOsValoresAtribuidos()
    {
        var criadoEm = new DateTime(2026, 11, 12, 15, 30, 0, DateTimeKind.Utc);

        var avaliacao = new Avaliacao
        {
            Id = 9,
            Estrelas = 4,
            Comentario = "Gostei",
            CriadoEmUtc = criadoEm,
        };

        Assert.Equal(9, avaliacao.Id);
        Assert.Equal(4, avaliacao.Estrelas);
        Assert.Equal("Gostei", avaliacao.Comentario);
        Assert.Equal(criadoEm, avaliacao.CriadoEmUtc);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void Estrelas_DeUmACinco_SaoValidas(int estrelas)
    {
        Assert.Empty(Validar(new Avaliacao { Estrelas = estrelas }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void Estrelas_ForaDeUmACinco_SaoInvalidas(int estrelas)
    {
        var erro = Assert.Single(Validar(new Avaliacao { Estrelas = estrelas }));

        Assert.Contains(nameof(Avaliacao.Estrelas), erro.MemberNames);
    }

    [Fact]
    public void Comentario_ComQuinhentosCaracteres_EValido()
    {
        var avaliacao = new Avaliacao { Estrelas = 5, Comentario = new string('a', 500) };

        Assert.Empty(Validar(avaliacao));
    }

    [Fact]
    public void Comentario_ComMaisDeQuinhentosCaracteres_EInvalido()
    {
        var avaliacao = new Avaliacao { Estrelas = 5, Comentario = new string('a', 501) };

        var erro = Assert.Single(Validar(avaliacao));

        Assert.Contains(nameof(Avaliacao.Comentario), erro.MemberNames);
    }

    [Fact]
    public void Comentario_Nulo_EValido()
    {
        Assert.Empty(Validar(new Avaliacao { Estrelas = 2, Comentario = null }));
    }
}
