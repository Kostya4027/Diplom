using System.ComponentModel.DataAnnotations;

namespace ExamTickets.Core.Models;

public class Setting
{
    [Required]
    public string SettingKey { get; set; } = string.Empty;

    public string? SettingValue { get; set; }
}