using System.Globalization;

namespace FeriadoTracker.Web.Services;

public static class NotificationTemplates
{
    public const string Title = "Feriado se aproxima!";
    public const string DefaultUrl = "/";

    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    // Data absoluta no texto: a mensagem continua correta mesmo se o push
    // service entregar com atraso (contagem relativa ficaria defasada).
    public static string Body(int daysUntil, string holidayName, DateOnly date) => daysUntil switch
    {
        <= 0 => $"Hoje é {holidayName}!",
        1 => $"Amanhã é {holidayName}!",
        _ => $"{holidayName} em {date.ToString("dd 'de' MMMM", PtBr)}."
    };
}
