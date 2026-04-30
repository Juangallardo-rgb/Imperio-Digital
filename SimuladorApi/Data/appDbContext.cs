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

        // Nuevo módulo Design Thinking
        public DbSet<ScenarioPhaseSetting> ScenarioPhaseSettings { get; set; }

        public DbSet<PhaseCriteriaSetting> PhaseCriteriaSettings { get; set; }

        public DbSet<ScenarioOption> ScenarioOptions { get; set; }

        public DbSet<SimulationAttempt> SimulationAttempts { get; set; }

        public DbSet<SimulationPhaseResponse> SimulationPhaseResponses { get; set; }

        public DbSet<SimulationAnswer> SimulationAnswers { get; set; }

        public DbSet<SimulationKpiResult> SimulationKpiResults { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==========================
            // RELACIONES EXISTENTES
            // ==========================

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

            // ==========================
            // NUEVO MÓDULO DESIGN THINKING
            // ==========================

            modelBuilder.Entity<ScenarioPhaseSetting>()
                .HasOne(p => p.Scenario)
                .WithMany(s => s.PhaseSettings)
                .HasForeignKey(p => p.ScenarioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PhaseCriteriaSetting>()
                .HasOne(c => c.ScenarioPhaseSetting)
                .WithMany(p => p.Criteria)
                .HasForeignKey(c => c.ScenarioPhaseSettingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ScenarioOption>()
                .HasOne(o => o.Scenario)
                .WithMany(s => s.Options)
                .HasForeignKey(o => o.ScenarioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SimulationAttempt>()
                .HasOne(a => a.Scenario)
                .WithMany(s => s.SimulationAttempts)
                .HasForeignKey(a => a.ScenarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SimulationAttempt>()
                .HasOne(a => a.Student)
                .WithMany()
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SimulationPhaseResponse>()
                .HasOne(r => r.SimulationAttempt)
                .WithMany(a => a.PhaseResponses)
                .HasForeignKey(r => r.SimulationAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SimulationAnswer>()
                .HasOne(a => a.SimulationPhaseResponse)
                .WithMany(r => r.Answers)
                .HasForeignKey(a => a.SimulationPhaseResponseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SimulationKpiResult>()
                .HasOne(k => k.SimulationAttempt)
                .WithMany(a => a.KpiResults)
                .HasForeignKey(k => k.SimulationAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            // ==========================
            // PRECISIONES DECIMALES
            // ==========================

            modelBuilder.Entity<ScenarioPhaseSetting>()
                .Property(p => p.PhaseWeight)
                .HasPrecision(5, 2);

            modelBuilder.Entity<PhaseCriteriaSetting>()
                .Property(c => c.CriterionWeight)
                .HasPrecision(5, 2);

            modelBuilder.Entity<ScenarioOption>()
                .Property(o => o.Score)
                .HasPrecision(5, 2);

            modelBuilder.Entity<SimulationAttempt>()
                .Property(a => a.FinalScore)
                .HasPrecision(5, 2);

            modelBuilder.Entity<SimulationPhaseResponse>()
                .Property(r => r.Score)
                .HasPrecision(5, 2);

            modelBuilder.Entity<SimulationAnswer>()
                .Property(a => a.Score)
                .HasPrecision(5, 2);

            modelBuilder.Entity<SimulationKpiResult>()
                .Property(k => k.InitialValue)
                .HasPrecision(10, 2);

            modelBuilder.Entity<SimulationKpiResult>()
                .Property(k => k.FinalValue)
                .HasPrecision(10, 2);
        }
    }
}