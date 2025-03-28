using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HSESport_web_app_trial2.Models;

namespace HSESport_web_app_trial2.Data
{
    public class HSESport_web_app_trial2Context : DbContext
    {
        public HSESport_web_app_trial2Context (DbContextOptions<HSESport_web_app_trial2Context> options)
            : base(options)
        {
        }

        public DbSet<HSESport_web_app_trial2.Models.Students> Students { get; set; } = default!;
    }
}
