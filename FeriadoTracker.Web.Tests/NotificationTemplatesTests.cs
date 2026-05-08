using FeriadoTracker.Web.Services;

namespace FeriadoTracker.Web.Tests;

public class NotificationTemplatesTests
{
    [Theory]
    [InlineData(0, "Natal", "Hoje é Natal!")]
    [InlineData(1, "Ano Novo", "Amanhã é Ano Novo!")]
    [InlineData(5, "Carnaval", "Faltam 5 dias para Carnaval.")]
    [InlineData(-1, "Passado", "Hoje é Passado!")]
    public void Body_RetornaMensagemCorreta(int dias, string nome, string esperado)
    {
        var resultado = NotificationTemplates.Body(dias, nome);
        Assert.Equal(esperado, resultado);
    }
}
