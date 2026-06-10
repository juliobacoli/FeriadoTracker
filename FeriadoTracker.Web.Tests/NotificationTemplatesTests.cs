using FeriadoTracker.Web.Services;

namespace FeriadoTracker.Web.Tests;

public class NotificationTemplatesTests
{
    #region Testes de unidade — função pura, sem dependências

    // DateOnly não é suportado em [InlineData] (não é constante de compilação),
    // por isso [MemberData].
    public static TheoryData<int, string, DateOnly, string> Casos => new()
    {
        { 0, "Natal", new DateOnly(2026, 12, 25), "Hoje é Natal!" },
        { 1, "Ano Novo", new DateOnly(2027, 1, 1), "Amanhã é Ano Novo!" },
        { 5, "Carnaval", new DateOnly(2026, 2, 17), "Carnaval em 17 de fevereiro." },
        { 3, "Corpus Christi", new DateOnly(2026, 6, 4), "Corpus Christi em 04 de junho." },
        { -1, "Passado", new DateOnly(2026, 1, 1), "Hoje é Passado!" }
    };

    [Theory]
    [MemberData(nameof(Casos))]
    public void Body_RetornaMensagemCorreta(int dias, string nome, DateOnly data, string esperado)
    {
        var resultado = NotificationTemplates.Body(dias, nome, data);
        Assert.Equal(esperado, resultado);
    }

    #endregion
}
