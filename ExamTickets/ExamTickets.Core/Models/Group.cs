using System.ComponentModel.DataAnnotations;

namespace ExamTickets.Core.Models;

public class Group
{
    public int GroupID { get; set; }

    public int SpecialtyID { get; set; }

    [Required]
    public string GroupNumber { get; set; } = string.Empty;

    public Specialty? Specialty { get; set; }
    public ICollection<ExamEvent> ExamEvents { get; set; } = new List<ExamEvent>();
}