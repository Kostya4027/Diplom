using System.ComponentModel.DataAnnotations;

namespace ExamTickets.Core.Models;

public class AuditLog
{
    public int LogID { get; set; }

    public int UserID { get; set; }

    [Required]
    public string Action { get; set; } = string.Empty;

    [Required]
    public string Details { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
}