namespace FeriadoTracker.Web.Models;

public class NotificationLog
{
    public int Id { get; set; }
    public int SubscriptionId { get; set; }
    public int FeriadoId { get; set; }
    public DateOnly SentDate { get; set; }
    public DateTime SentAtUtc { get; set; }

    public PushSubscription? Subscription { get; set; }
    public Feriado? Feriado { get; set; }
}
