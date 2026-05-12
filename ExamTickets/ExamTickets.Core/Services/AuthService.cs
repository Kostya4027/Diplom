using BCrypt.Net;
using ExamTickets.Core.Data;
using ExamTickets.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ExamTickets.Core.Services;

public class AuthService
{
    private const int WorkFactor = 12;
    private readonly AppDbContext _dbContext;

    public AuthService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> LoginAsync(string login, string password)
    {
        var normalizedLogin = login.Trim();
        var normalizedPassword = password.Trim();

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Login == normalizedLogin);

        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return null;
        }

        var hash = user.PasswordHash.Trim();

        return BCrypt.Net.BCrypt.Verify(normalizedPassword, hash) ? user : null;
    }

    public async Task<bool> CreateUserAsync(string login, string password, string fullName, UserRole role)
    {
        var exists = await _dbContext.Users.AnyAsync(x => x.Login == login);
        if (exists)
        {
            return false;
        }

        var user = new User
        {
            Login = login,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, WorkFactor),
            FullName = fullName,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangePasswordAsync(int userId, string newPassword)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.UserID == userId);
        if (user is null)
        {
            return false;
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, WorkFactor);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleUserActiveAsync(int userId)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.UserID == userId);
        if (user is null)
        {
            return false;
        }

        user.IsActive = !user.IsActive;
        await _dbContext.SaveChangesAsync();
        return true;
    }
}