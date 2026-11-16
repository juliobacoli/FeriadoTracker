namespace FeriadoTracker.Web.Dtos;

/// <param name="Locais">Códigos IBGE dos estados e cidades marcados.</param>
public record PushLocaisDto(string Endpoint, int[] Locais);
