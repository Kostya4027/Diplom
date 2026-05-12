using DocumentFormat.OpenXml.Spreadsheet;
using ExamTickets.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ExamTickets.Core.Data;

public class AppDbContext : DbContext
{
    private const string ConnectionString = "Data Source=.\\SQLEXPRESS;Initial Catalog=ExamTicketsDB;Integrated Security=True;TrustServerCertificate=True;";

    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Specialty> Specialties => Set<Specialty>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<ExamTickets.Core.Models.Group> Groups => Set<ExamTickets.Core.Models.Group>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<TeacherSubject> TeacherSubjects => Set<TeacherSubject>();
    public DbSet<ExamEvent> ExamEvents => Set<ExamEvent>();
    public DbSet<QuestionDocument> QuestionDocuments => Set<QuestionDocument>();
    public DbSet<TicketDocument> TicketDocuments => Set<TicketDocument>();
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(ConnectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserID);
            entity.HasIndex(e => e.Login).IsUnique();

            entity.Property(e => e.Login).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Role).HasConversion<int>();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2");
        });

        modelBuilder.Entity<Specialty>(entity =>
        {
            entity.HasKey(e => e.SpecialtyID);
            entity.Property(e => e.SpecialtyName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.SpecialtyNumber).IsRequired().HasMaxLength(50);
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.SubjectID);
            entity.Property(e => e.SubjectName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Semester).IsRequired();

            entity.HasOne(e => e.Specialty)
                .WithMany(e => e.Subjects)
                .HasForeignKey(e => e.SpecialtyID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExamTickets.Core.Models.Group>(entity =>
        {
            entity.HasKey(e => e.GroupID);
            entity.Property(e => e.GroupNumber).IsRequired().HasMaxLength(50);

            entity.HasOne(e => e.Specialty)
                .WithMany(e => e.Groups)
                .HasForeignKey(e => e.SpecialtyID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Teacher>(entity =>
        {
            entity.HasKey(e => e.TeacherID);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);

            entity.HasOne(e => e.User)
                .WithMany(e => e.Teachers)
                .HasForeignKey(e => e.UserID)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Subjects)
                .WithMany(e => e.Teachers)
                .UsingEntity<TeacherSubject>(
                    right => right.HasOne(e => e.Subject)
                        .WithMany(e => e.TeacherSubjects)
                        .HasForeignKey(e => e.SubjectID)
                        .OnDelete(DeleteBehavior.Cascade),
                    left => left.HasOne(e => e.Teacher)
                        .WithMany(e => e.TeacherSubjects)
                        .HasForeignKey(e => e.TeacherID)
                        .OnDelete(DeleteBehavior.Cascade),
                    join =>
                    {
                        join.ToTable("TeacherSubjects");
                        join.HasKey(e => new { e.TeacherID, e.SubjectID });
                    });
        });

        modelBuilder.Entity<ExamEvent>(entity =>
        {
            entity.HasKey(e => e.EventID);

            entity.Property(e => e.ProtocolNumber).HasMaxLength(100);
            entity.Property(e => e.CommissionName).HasMaxLength(200);
            entity.Property(e => e.Chairman).HasMaxLength(200);
            entity.Property(e => e.Affirmer).HasMaxLength(200);
            entity.Property(e => e.AffirmerLastName).HasMaxLength(200);
            entity.Property(e => e.DateOfStatement).HasColumnType("datetime2");
            entity.Property(e => e.ExamDate).HasColumnType("datetime2");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2");
            entity.Property(e => e.QuestionsPerTicket).HasDefaultValue(3);

            entity.HasOne(e => e.Subject)
                .WithMany(e => e.ExamEvents)
                .HasForeignKey(e => e.SubjectID)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Specialty)
                .WithMany(e => e.ExamEvents)
                .HasForeignKey(e => e.SpecialtyID)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Group)
                .WithMany(e => e.ExamEvents)
                .HasForeignKey(e => e.GroupID)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Teacher)
                .WithMany(e => e.ExamEvents)
                .HasForeignKey(e => e.TeacherID)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.QuestionDocument)
                .WithMany(e => e.ExamEvents)
                .HasForeignKey(e => e.QuestionID)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.TicketDocument)
                .WithMany(e => e.ExamEvents)
                .HasForeignKey(e => e.TicketID)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.CreatedByUser)
                .WithMany(e => e.CreatedExamEvents)
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<QuestionDocument>(entity =>
        {
            entity.HasKey(e => e.QuestionID);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.UploadedAt).HasColumnType("datetime2");

            entity.HasOne(e => e.Subject)
                .WithMany(e => e.QuestionDocuments)
                .HasForeignKey(e => e.SubjectID)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.UploadedByUser)
                .WithMany(e => e.UploadedQuestionDocuments)
                .HasForeignKey(e => e.UploadedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TicketDocument>(entity =>
        {
            entity.HasKey(e => e.TicketID);
            entity.Property(e => e.FilePathDocx).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FilePathPdf).HasMaxLength(500);
            entity.Property(e => e.GeneratedAt).HasColumnType("datetime2");

            entity.HasOne(e => e.GeneratedByUser)
                .WithMany(e => e.GeneratedTicketDocuments)
                .HasForeignKey(e => e.GeneratedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Setting>(entity =>
        {
            entity.HasKey(e => e.SettingKey);
            entity.Property(e => e.SettingKey).HasMaxLength(200);
            entity.Property(e => e.SettingValue).HasMaxLength(4000);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.LogID);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Details).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2");

            entity.HasOne(e => e.User)
                .WithMany(e => e.AuditLogs)
                .HasForeignKey(e => e.UserID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Настройки
        modelBuilder.Entity<Setting>().HasData(
            new Setting
            {
                SettingKey = "InstitutionName",
                SettingValue = "МИНИСТЕРСТВО НАУКИ И ВЫСШЕГО ОБРАЗОВАНИЯ РОССИЙСКОЙ ФЕДЕРАЦИИ\nфедеральное государственное автономное образовательное учреждение высшего образования\n«Санкт-Петербургский политехнический университет Петра Великого»\n(ФГАОУ ВО «СПбПУ»)\nИнститут среднего профессионального образования"
            }
        );

        // Администратор по умолчанию
        modelBuilder.Entity<User>().HasData(
            new User
            {
                UserID = 1,
                Login = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123", 12),
                FullName = "Администратор",
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = new DateTime(2025, 1, 1)
            }
        );

        // Пример специальности
        modelBuilder.Entity<Specialty>().HasData(
            new Specialty
            {
                SpecialtyID = 1,
                SpecialtyName = "Информационные системы и программирование",
                SpecialtyNumber = "09.02.07"
            }
        );
    }
}