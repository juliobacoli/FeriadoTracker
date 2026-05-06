namespace FeriadoTracker.Web.Services;

public sealed class BrazilTimeProvider : TimeProvider
{
    private static readonly TimeZoneInfo BrazilTimeZone = ResolveTimeZone();

    public override TimeZoneInfo LocalTimeZone => BrazilTimeZone;

    private static TimeZoneInfo ResolveTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        }
    }
}
