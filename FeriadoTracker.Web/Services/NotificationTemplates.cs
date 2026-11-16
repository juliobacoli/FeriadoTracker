namespace FeriadoTracker.Web.Services;

public static class NotificationTemplates
{
    public const string Title = "Feriado se aproxima!";
    public const string DefaultUrl = "/";

    /// <param name="local">Estado ou cidade com preposição: "no Pará", "na cidade de São Paulo".</param>
    public static string TitleRegional(string local) => $"Feriado {local} se aproxima!";

    // Data absoluta no texto: a mensagem continua correta mesmo se o push
    // service entregar com atraso (contagem relativa ficaria defasada).
    public static string Body(int daysUntil, string holidayName, DateOnly date) => daysUntil switch
    {
        <= 0 => $"Hoje é {holidayName}!",
        1 => $"Amanhã é {holidayName}!",
        _ => $"{holidayName} em {PtBrFormat.DiaEMes(date)}."
    };

    /// <summary>Mesma mensagem, seguida do que o feriado vira: folga prolongada, ponte ou nada.</summary>
    public static string Body(int daysUntil, FeriadoAnalisado feriado) =>
        $"{Body(daysUntil, feriado.Nome, feriado.Inicio)} {FeriadoVisaoBuilder.Impacto(feriado)}";
}
