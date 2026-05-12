using System.ComponentModel.DataAnnotations;

namespace ExamTickets.Core.Models;

public class TicketDocument
{
    public int TicketID { get; set; }

    [Required]
    public string FilePathDocx { get; set; } = string.Empty;

    public string? FilePathPdf { get; set; }

    public int GeneratedBy { get; set; }

    public DateTime GeneratedAt { get; set; }

    public User? GeneratedByUser { get; set; }
    public ICollection<ExamEvent> ExamEvents { get; set; } = new List<ExamEvent>();
}