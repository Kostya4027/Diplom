using System.ComponentModel.DataAnnotations;

namespace ExamTickets.Core.Models;

public class QuestionDocument
{
    public int QuestionID { get; set; }

    [Required]
    public string FilePath { get; set; } = string.Empty;

    public int? SubjectID { get; set; }

    public int UploadedBy { get; set; }

    public DateTime UploadedAt { get; set; }

    public Subject? Subject { get; set; }
    public User? UploadedByUser { get; set; }
    public ICollection<ExamEvent> ExamEvents { get; set; } = new List<ExamEvent>();
}