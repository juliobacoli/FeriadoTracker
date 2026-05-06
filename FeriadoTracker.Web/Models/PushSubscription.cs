using System.ComponentModel.DataAnnotations;

namespace FeriadoTracker.Web.Models;

public class PushSubscription
{
    public int Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string P256dh { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Auth { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
