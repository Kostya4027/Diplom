using System.ComponentModel.DataAnnotations;

namespace ExamTickets.Core.Models;

public class User
{
    public int UserID { get; set; }

    [Required]
    public string Login { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public ICollection<Teacher> Teachers { get; set; } = new List<Teacher>();
    public ICollection<ExamEvent> CreatedExamEvents { get; set; } = new List<ExamEvent>();
    public ICollection<QuestionDocument> UploadedQuestionDocuments { get; set; } = new List<QuestionDocument>();
    public ICollection<TicketDocument> GeneratedTicketDocuments { get; set; } = new List<TicketDocument>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}