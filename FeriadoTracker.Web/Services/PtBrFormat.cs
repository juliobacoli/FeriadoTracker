namespace FeriadoTracker.Web.Services;

/// <summary>
/// Formatação pt-BR sem depender do ICU. Permite rodar com
/// InvariantGlobalization=true, que remove ~30 MB de dados de cultura
/// mapeados no processo.
/// </summary>
public static class PtBrFormat
{
    private static readonly string[] Meses =
    [
        "janeiro", "fevereiro", "março", "abril", "maio", "junho",
        "julho", "agosto", "setembro", "outubro", "novembro", "dezembro"
    ];

    private static readonly string[] DiasSemana =
    [
        "Domingo", "Segunda-feira", "Terça-feira", "Quarta-feira",
        "Quinta-feira", "Sexta-feira", "Sábado"
    ];

    /// <summary>Ex.: 25 de dezembro de 2026</summary>
    public static string DataPorExtenso(DateOnly data) =>
        $"{data.Day:00} de {Meses[data.Month - 1]} de {data.Year:0000}";

    /// <summary>Ex.: 25 de dezembro</summary>
    public static string DiaEMes(DateOnly data) =>
        $"{data.Day:00} de {Meses[data.Month - 1]}";

    /// <summary>Ex.: 25/12/2026</summary>
    public static string DataCurta(DateOnly data) =>
        $"{data.Day:00}/{data.Month:00}/{data.Year:0000}";

    /// <summary>Ex.: Sexta-feira</summary>
    public static string DiaDaSemana(DateOnly data) =>
        DiasSemana[(int)data.DayOfWeek];

    /// <summary>Ex.: 2026-12-25T00:00:00 (formato consumido pelo JS do cliente).</summary>
    public static string Iso(DateOnly data) =>
        $"{data.Year:0000}-{data.Month:00}-{data.Day:00}T00:00:00";
}
