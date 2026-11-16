using FeriadoTracker.Web.Models;

namespace FeriadoTracker.Web.Services;

/// <param name="Codigo">Código IBGE: 2 dígitos no estado (SP = 35), 7 no município (São Paulo = 3550308). É a chave.</param>
/// <param name="Rotulo">Texto da pílula: a sigla no estado, o nome no município.</param>
/// <param name="Nome">Nome completo, para leitor de tela e dica.</param>
/// <param name="Uf">Sigla do estado, só para exibição.</param>
/// <param name="Local">Nome com a preposição certa, para frases como "Feriado no Pará".</param>
public sealed record LocalLiberado(int Codigo, string Rotulo, string Nome, string Uf, string Local)
{
    public bool EhMunicipio => Codigo > 99;

    /// <summary>Selo na lista de feriados: a sigla no estado, "Municipal" no município.</summary>
    public string Selo => EhMunicipio ? "Municipal" : Uf;

    /// <summary>Código IBGE do estado; no município, são os 2 primeiros dígitos.</summary>
    public int CodigoUf => EhMunicipio ? Codigo / 100_000 : Codigo;

    public bool Inclui(Feriado f) => EhMunicipio
        ? f.CodigoMunicipio == Codigo
        : f.CodigoUf == Codigo && f.CodigoMunicipio == null;
}

/// <summary>
/// Estados e cidades com feriados já confirmados em fonte oficial. Os demais entram em levas.
/// </summary>
public static class LocaisLiberados
{
    public static readonly IReadOnlyList<LocalLiberado> Todos =
    [
        new(32, "ES", "Espírito Santo", "ES", "no Espírito Santo"),
        new(15, "PA", "Pará", "PA", "no Pará"),
        new(26, "PE", "Pernambuco", "PE", "em Pernambuco"),
        new(42, "SC", "Santa Catarina", "SC", "em Santa Catarina"),
        new(35, "SP", "Estado de São Paulo", "SP", "no estado de São Paulo"),
        // Mesmo nome do estado: o rótulo deixa claro que é a cidade.
        new(3550308, "São Paulo (cidade)", "Cidade de São Paulo", "SP", "na cidade de São Paulo")
    ];

    public static LocalLiberado? Buscar(int codigo) => Todos.FirstOrDefault(l => l.Codigo == codigo);

    /// <summary>Só os códigos liberados, sem repetição, na ordem da lista.</summary>
    public static List<LocalLiberado> Filtrar(IEnumerable<int>? codigos)
    {
        var pedidos = codigos?.ToHashSet() ?? [];
        return Todos.Where(l => pedidos.Contains(l.Codigo)).ToList();
    }

    /// <summary>Texto do botão do topo: nenhum, o rótulo do único local ou "Várias localidades".</summary>
    public static string Resumo(IReadOnlyList<LocalLiberado> locais) => locais.Count switch
    {
        0 => "Só nacionais",
        1 => locais[0].Rotulo,
        _ => "Várias localidades"
    };
}
