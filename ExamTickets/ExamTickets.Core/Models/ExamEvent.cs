using System.ComponentModel.DataAnnotations;

namespace ExamTickets.Core.Models;

public class ExamEvent
{
    public int EventID { get; set; }

    public int SubjectID { get; set; }

    public int SpecialtyID { get; set; }

    public int GroupID { get; set; }

    public int TeacherID { get; set; }

    public int? QuestionID { get; set; }

    public int? TicketID { get; set; }

    public DateTime ExamDate { get; set; }

    [Required]
    public string ProtocolNumber { get; set; } = string.Empty;

    [Required]
    public string CommissionName { get; set; } = string.Empty;

    [Required]
    public string Chairman { get; set; } = string.Empty;

    [Required]
    public string Affirmer { get; set; } = string.Empty;

    [Required]
    public string AffirmerLastName { get; set; } = string.Empty;

    public DateTime DateOfStatement { get; set; }

    public int Semester { get; set; }

    [Required]
    public string ExamType { get; set; } = string.Empty;

    public int TicketCount { get; set; }

    public int QuestionsPerTicket { get; set; } = 3;

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public Subject? Subject { get; set; }
    public Specialty? Specialty { get; set; }
    public Group? Group { get; set; }
    public Teacher? Teacher { get; set; }
    public QuestionDocument? QuestionDocument { get; set; }
    public TicketDocument? TicketDocument { get; set; }
    public User? CreatedByUser { get; set; }
}