namespace FeriadoTracker.Web.Services;

public static class NotificationTemplates
{
    public const string Title = "Feriado se aproxima!";
    public const string DefaultUrl = "/";

    // Data absoluta no texto: a mensagem continua correta mesmo se o push
    // service entregar com atraso (contagem relativa ficaria defasada).
    public static string Body(int daysUntil, string holidayName, DateOnly date) => daysUntil switch
    {
        <= 0 => $"Hoje é {holidayName}!",
        1 => $"Amanhã é {holidayName}!",
        _ => $"{holidayName} em {PtBrFormat.DiaEMes(date)}."
    };
}
