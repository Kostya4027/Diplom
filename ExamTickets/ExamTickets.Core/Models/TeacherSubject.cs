namespace ExamTickets.Core.Models;
public class TeacherSubject
{
    public int TeacherID { get; set; }

    public int SubjectID { get; set; }

    public Teacher? Teacher { get; set; }
    public Subject? Subject { get; set; }
}