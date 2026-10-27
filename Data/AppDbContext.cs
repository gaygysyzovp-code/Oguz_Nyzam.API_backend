using Microsoft.EntityFrameworkCore;
using Oguz_Nyzam.API.Entities;

namespace Oguz_Nyzam.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Faculty> Faculties => Set<Faculty>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<ViolationCategory> ViolationCategories => Set<ViolationCategory>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<AttendanceDetail> AttendanceDetails => Set<AttendanceDetail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AttendanceRecord>()
            .HasOne(ar => ar.Group)
            .WithMany()
            .HasForeignKey(ar => ar.GroupId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<AttendanceRecord>()
            .HasOne(ar => ar.User)
            .WithMany()
            .HasForeignKey(ar => ar.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AttendanceRecord>()
            .HasIndex(ar => new { ar.GroupId, ar.Date })
            .IsUnique();

        modelBuilder.Entity<AttendanceDetail>()
            .HasOne(d => d.AttendanceRecord)
            .WithMany(r => r.Details)
            .HasForeignKey(d => d.AttendanceRecordId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<AttendanceDetail>()
            .HasOne(d => d.Student)
            .WithMany()
            .HasForeignKey(d => d.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<AttendanceDetail>()
            .HasOne(d => d.ViolationCategory)
            .WithMany()
            .HasForeignKey(d => d.ViolationCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Dean" },
            new Role { Id = 2, Name = "Deputy Dean" }
        );

        modelBuilder.Entity<ViolationCategory>().HasData(
            new ViolationCategory { Id = 1, Name = "Nyzama gelmedik" },
            new ViolationCategory { Id = 2, Name = "Targetkasy ýok" },
            new ViolationCategory { Id = 3, Name = "Egin eşigi düzüw däl" }
        );
    }
}