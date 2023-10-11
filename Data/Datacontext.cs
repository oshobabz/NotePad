using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NotePad.Models;

namespace NotePad.Data
{
    public class Datacontext : IdentityDbContext<User, IdentityRole <int>,int>
    {
        public Datacontext(DbContextOptions options) : base(options)
        {
            
        }

        public DbSet<Note> Notes { get; set; }
        public DbSet<User> Users { get; set; }
    }
}
