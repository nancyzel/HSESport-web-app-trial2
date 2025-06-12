using Microsoft.EntityFrameworkCore;
using static NuGet.Packaging.PackagingConstants;

namespace HSESport_web_app_trial2.Models
{
    public class MyDbContextTeachers:DbContext
    {

        public MyDbContextTeachers(DbContextOptions<MyDbContextTeachers> options)
            : base(options)
        { }
        public DbSet<Students> Students { get; set; }
        public DbSet<Teachers> Teachers { get; set; }

        public DbSet<Sections> Sections { get; set; }

        public DbSet<AttendanceDates> AttendanceDates { get; set; }

        // Добавляем DbSet для новой таблицы-связки TeacherSections
        public DbSet<TeacherSection> TeacherSections { get; set; }
        public DbSet<StudentsSections> StudentsSections { get; set; } // <--- Добавляем DbSet для StudentSection

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Настройка составного первичного ключа для TeacherSection
            // Это говорит EF Core, что TeacherId и SectionId вместе образуют PK
            modelBuilder.Entity<TeacherSection>()
                .HasKey(ts => new { ts.TeacherId, ts.SectionId });

            // Настройка отношения между TeacherSection и Teacher
            modelBuilder.Entity<TeacherSection>()
                .HasOne(ts => ts.Teacher) // Одна TeacherSection связана с одним Teacher
                .WithMany(t => t.TeacherSections) // Один Teacher может иметь много TeacherSection
                .HasForeignKey(ts => ts.TeacherId); // Внешний ключ в TeacherSection - TeacherId

            // Настройка отношения между TeacherSection и Section
            modelBuilder.Entity<TeacherSection>()
                .HasOne(ts => ts.Section) // Одна TeacherSection связана с одной Section
                .WithMany(s => s.TeacherSections) // Одна Section может иметь много TeacherSection
                .HasForeignKey(ts => ts.SectionId); // Внешний ключ в TeacherSection - SectionId
                                                    // НОВАЯ НАСТРОЙКА для StudentSection (отношение многие ко многим)
            modelBuilder.Entity<StudentsSections>()
                .HasKey(ss => new { ss.StudentId, ss.SectionId }); // Составной первичный ключ

            modelBuilder.Entity<StudentsSections>()
                .HasOne(ss => ss.Student) // Одна запись StudentSection связана с одним Student
                .WithMany(s => s.StudentsSections) // Один Student может иметь много записей StudentSection
                .HasForeignKey(ss => ss.StudentId); // Внешний ключ - StudentId

            modelBuilder.Entity<StudentsSections>()
                .HasOne(ss => ss.Section) // Одна запись StudentSection связана с одной Section
                .WithMany(s => s.StudentsSections) // Одна Section может иметь много записей StudentSection
                .HasForeignKey(ss => ss.SectionId); // Внешний ключ - SectionId
        }
    }
}

