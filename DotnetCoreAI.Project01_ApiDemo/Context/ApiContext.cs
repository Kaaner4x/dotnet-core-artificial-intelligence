using DotnetCoreAI.Project01_ApiDemo.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotnetCoreAI.Project01_ApiDemo.Context
{
    public class ApiContext : DbContext
    {
        override protected void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS;initial catalog=ApiAIDb; integrated security=true; trustServerCertificate=true;");
        }

        public DbSet<Customer> Customers { get; set; }
    }
}
