namespace ExamTickets.Core.DTOs;

public class TicketFormData
{
    public string EducationalInstitution { get; set; } = string.Empty;

    public string Commission { get; set; } = string.Empty;

    public string ProtocolNumber { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public string Chairman { get; set; } = string.Empty;

    public string SpecialtyNumber { get; set; } = string.Empty;

    public string ExamType { get; set; } = string.Empty;

    public string Exam { get; set; } = string.Empty;

    public string GroupsNumber { get; set; } = string.Empty;

    public int Semester { get; set; }

    public string Affirmer { get; set; } = string.Empty;

    public string AffirmerLastName { get; set; } = string.Empty;

    public DateTime DateOfStatement { get; set; }

    public string Teachers { get; set; } = string.Empty;

    public int TicketCount { get; set; }

    public int QuestionsPerTicket { get; set; }

    public string QuestionFilePath { get; set; } = string.Empty;
}