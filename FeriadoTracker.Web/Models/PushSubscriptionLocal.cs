namespace FeriadoTracker.Web.Models;

/// <summary>Estado ou cidade cujos feriados o aparelho quer receber.</summary>
public class PushSubscriptionLocal
{
    public int SubscriptionId { get; set; }

    /// <summary>Código IBGE: 2 dígitos no estado (SP = 35), 7 no município (São Paulo = 3550308).</summary>
    public int Codigo { get; set; }
}
