using Email_Project.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Email_Project.Context
{
    public class EmailContext :IdentityDbContext<AppUser>
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {          
            optionsBuilder.UseSqlServer("Server=savasseren\\MSSQLSERVER02;initial catalog=EmailProjectDb;integrated security=true;TrustServerCertificate=True;Trusted_Connection=True");
        }
        public DbSet<Message> Messages { get; set; }   
        public DbSet<Category> Categories { get; set; }   
    }
}
