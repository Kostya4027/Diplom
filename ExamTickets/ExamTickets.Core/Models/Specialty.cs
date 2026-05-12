using System.ComponentModel.DataAnnotations;

namespace ExamTickets.Core.Models;

public class Specialty
{
    public int SpecialtyID { get; set; }

    [Required]
    public string SpecialtyName { get; set; } = string.Empty;

    [Required]
    public string SpecialtyNumber { get; set; } = string.Empty;

    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
    public ICollection<Group> Groups { get; set; } = new List<Group>();
    public ICollection<ExamEvent> ExamEvents { get; set; } = new List<ExamEvent>();
}