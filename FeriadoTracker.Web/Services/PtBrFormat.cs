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

    /// <summary>Ex.: 25 de dezembro</summary>
    public static string DiaEMes(DateOnly data) =>
        $"{data.Day:00} de {Meses[data.Month - 1]}";

    private static readonly string[] DiasSemanaCurtos = ["dom", "seg", "ter", "qua", "qui", "sex", "sáb"];

    /// <summary>Ex.: Sexta-feira</summary>
    public static string DiaDaSemana(DateOnly data) =>
        DiasSemana[(int)data.DayOfWeek];

    /// <summary>Ex.: sexta-feira</summary>
    public static string DiaDaSemanaMinusculo(DateOnly data) =>
        DiasSemana[(int)data.DayOfWeek].ToLowerInvariant();

    /// <summary>Ex.: sex</summary>
    public static string DiaDaSemanaCurto(DateOnly data) =>
        DiasSemanaCurtos[(int)data.DayOfWeek];

    /// <summary>Ex.: novembro</summary>
    public static string Mes(DateOnly data) =>
        Meses[data.Month - 1];

    /// <summary>Ex.: nov</summary>
    public static string MesCurto(DateOnly data) =>
        Meses[data.Month - 1][..3];
}
