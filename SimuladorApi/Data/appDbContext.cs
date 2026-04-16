using Microsoft.EntityFrameworkCore;
using SimuladorApi.Models;

namespace SimuladorApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        public DbSet<Scenario> Scenarios { get; set; }

        public DbSet<ScenarioVariable> ScenarioVariables { get; set; }

        public DbSet<Simulation> Simulations { get; set; }

        public DbSet<SimulationVariableValue> SimulationVariableValues { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Scenario>()
                .HasOne(s => s.CreatedByUser)
                .WithMany()
                .HasForeignKey(s => s.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ScenarioVariable>()
                .HasOne(v => v.Scenario)
                .WithMany(s => s.Variables)
                .HasForeignKey(v => v.ScenarioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Simulation>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Simulation>()
                .HasOne(s => s.Scenario)
                .WithMany()
                .HasForeignKey(s => s.ScenarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SimulationVariableValue>()
                .HasOne(v => v.Simulation)
                .WithMany(s => s.VariableValues)
                .HasForeignKey(v => v.SimulationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SimulationVariableValue>()
                .HasOne(v => v.ScenarioVariable)
                .WithMany()
                .HasForeignKey(v => v.ScenarioVariableId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}