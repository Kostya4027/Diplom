using ExamTickets.Core.Models;

namespace ExamTickets.WPF.Helpers;

public static class CurrentUser
{
    public static User? Instance { get; private set; }

    public static bool IsAdmin => Instance?.Role == UserRole.Admin;
    public static bool IsChairman => Instance?.Role == UserRole.Chairman;
    public static bool IsTeacher => Instance?.Role == UserRole.Teacher;

    public static void SetUser(User user)
    {
        Instance = user;
    }

    public static void Clear()
    {
        Instance = null;
    }
}