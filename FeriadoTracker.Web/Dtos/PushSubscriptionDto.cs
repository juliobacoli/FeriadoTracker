namespace FeriadoTracker.Web.Dtos;

public record PushSubscriptionDto(string Endpoint, string P256dh, string Auth);
