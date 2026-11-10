using System.Text.Json.Serialization;

namespace FeriadoTracker.Web.Services;

/// <summary>
/// Serialização gerada em compilação dos feriados enviados à página,
/// sem a reflexão (e a memória) do serializador padrão.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(List<FeriadoVisao>))]
public partial class FeriadoJsonContext : JsonSerializerContext;
