namespace FeriadoTracker.Web.Services;

public static class NotificationTemplates
{
    public const string Title = "Feriado se aproxima!";
    public const string DefaultUrl = "/";

    public static string Body(int daysUntil, string holidayName) => daysUntil switch
    {
        <= 0 => $"Hoje é {holidayName}!",
        1 => $"Amanhã é {holidayName}!",
        _ => $"Faltam {daysUntil} dias para {holidayName}."
    };
}
