using System.ComponentModel.DataAnnotations;

namespace ExamTickets.Core.Models;

public class Subject
{
    public int SubjectID { get; set; }

    [Required]
    public string SubjectName { get; set; } = string.Empty;

    public int SpecialtyID { get; set; }

    public int Semester { get; set; }

    public Specialty? Specialty { get; set; }
    public ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();
    public ICollection<Teacher> Teachers { get; set; } = new List<Teacher>();
    public ICollection<QuestionDocument> QuestionDocuments { get; set; } = new List<QuestionDocument>();
    public ICollection<ExamEvent> ExamEvents { get; set; } = new List<ExamEvent>();
}