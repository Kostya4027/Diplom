using System;
using System.Threading.Tasks;
using System.Diagnostics;
using ExamTickets.Core.Data;
using ExamTickets.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ExamTickets.Core.Services;

public class AuditService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public AuditService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    public async Task LogAsync(int? userId, string action, string details = "")
    {
        if (userId is null) return;

        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var log = new AuditLog
            {
                UserID = userId.Value,
                Action = action,
                Details = details ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };
            context.AuditLogs.Add(log);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Не бросаем наружу — только логируем в Debug
            Debug.WriteLine($"AuditService.LogAsync error: {ex}");
        }
    }
}