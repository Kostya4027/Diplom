using System.ComponentModel.DataAnnotations;

namespace ExamTickets.Core.Models;

public class Teacher
{
    public int TeacherID { get; set; }

    [Required]
    public string FullName { get; set; } = string.Empty;

    public int? UserID { get; set; }

    public User? User { get; set; }
    public ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();
    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
    public ICollection<ExamEvent> ExamEvents { get; set; } = new List<ExamEvent>();
}