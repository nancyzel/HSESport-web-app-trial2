using Microsoft.EntityFrameworkCore;
using static NuGet.Packaging.PackagingConstants;

namespace HSESport_web_app_trial2.Models
{
    public class MyDbContextTeachers:DbContext
    {

        public MyDbContextTeachers(DbContextOptions<MyDbContextTeachers> options)
            : base(options)
        { }
        public DbSet<Teachers> Teachers { get; set; }

        public DbSet<Sections> Sections { get; set; }

        public DbSet<AttendanceDates> AttendanceDates { get; set; }
    }
}

