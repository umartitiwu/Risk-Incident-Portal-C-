using Microsoft.EntityFrameworkCore;
using riskportal.Models;

namespace riskportal.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Incident> Incidents { get; set; }

    }
}
