using Microsoft.EntityFrameworkCore;
using static NuGet.Packaging.PackagingConstants;

namespace HSESport_web_app_trial2.Models
{
    public class MyDbContextStudents:DbContext
    {

        public MyDbContextStudents(DbContextOptions<MyDbContextStudents> options)
            : base(options)
        { }
        public DbSet<Students> Students { get; set; }
    }
}

