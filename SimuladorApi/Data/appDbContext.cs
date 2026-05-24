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

        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

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

        public DbSet<Course> Courses { get; set; }

        public DbSet<CourseEnrollment> CourseEnrollments { get; set; }

        public DbSet<CourseScenario> CourseScenarios { get; set; }

        public DbSet<Methodology> Methodologies { get; set; }

        public DbSet<MethodologyPhase> MethodologyPhases { get; set; }

        public DbSet<MethodologyPhaseCriteria> MethodologyPhaseCriteria { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // ==========================
            // RECUPERACIÓN DE CONTRASEÑA
            // ==========================

            modelBuilder.Entity<PasswordResetToken>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PasswordResetToken>()
                .HasIndex(t => t.Token)
                .IsUnique();

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
            // MÓDULO METODOLOGÍAS
            // ==========================

            modelBuilder.Entity<Methodology>()
                .HasIndex(m => m.Code)
                .IsUnique();

            modelBuilder.Entity<MethodologyPhase>()
                .HasOne(p => p.Methodology)
                .WithMany(m => m.Phases)
                .HasForeignKey(p => p.MethodologyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MethodologyPhase>()
                .HasIndex(p => new { p.MethodologyId, p.PhaseOrder })
                .IsUnique();

            modelBuilder.Entity<MethodologyPhaseCriteria>()
                .HasOne(c => c.MethodologyPhase)
                .WithMany(p => p.Criteria)
                .HasForeignKey(c => c.MethodologyPhaseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Scenario>()
                .HasOne(s => s.MethodologyCatalog)
                .WithMany(m => m.Scenarios)
                .HasForeignKey(s => s.MethodologyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ScenarioPhaseSetting>()
                .HasOne(p => p.MethodologyPhase)
                .WithMany()
                .HasForeignKey(p => p.MethodologyPhaseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PhaseCriteriaSetting>()
                .HasOne(c => c.MethodologyPhaseCriteria)
                .WithMany()
                .HasForeignKey(c => c.MethodologyPhaseCriteriaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ScenarioOption>()
                .HasOne(o => o.MethodologyPhase)
                .WithMany()
                .HasForeignKey(o => o.MethodologyPhaseId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================
            // MÓDULO CURSOS
            // ==========================

            modelBuilder.Entity<Course>()
                .HasOne(c => c.Teacher)
                .WithMany()
                .HasForeignKey(c => c.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CourseEnrollment>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CourseEnrollment>()
                .HasOne(e => e.Student)
                .WithMany()
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CourseScenario>()
                .HasOne(cs => cs.Course)
                .WithMany(c => c.CourseScenarios)
                .HasForeignKey(cs => cs.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CourseScenario>()
                .HasOne(cs => cs.Scenario)
                .WithMany()
                .HasForeignKey(cs => cs.ScenarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SimulationAttempt>()
                .HasOne(a => a.Course)
                .WithMany()
                .HasForeignKey(a => a.CourseId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Course>()
                .HasIndex(c => c.Code)
                .IsUnique();

            modelBuilder.Entity<CourseEnrollment>()
                .HasIndex(e => new { e.CourseId, e.StudentId })
                .IsUnique();

            modelBuilder.Entity<CourseScenario>()
                .HasIndex(cs => new { cs.CourseId, cs.ScenarioId })
                .IsUnique();

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
            modelBuilder.Entity<SimulationAttempt>()
    .Property(a => a.InitialBudget)
    .HasPrecision(10, 2);

            modelBuilder.Entity<SimulationAttempt>()
                .Property(a => a.RemainingBudget)
                .HasPrecision(10, 2);

            modelBuilder.Entity<SimulationAttempt>()
                .Property(a => a.InitialTimeWeeks)
                .HasPrecision(10, 2);

            modelBuilder.Entity<SimulationAttempt>()
                .Property(a => a.RemainingTimeWeeks)
                .HasPrecision(10, 2);

            modelBuilder.Entity<SimulationAttempt>()
                .Property(a => a.RiskLevel)
                .HasPrecision(10, 2);

            modelBuilder.Entity<ScenarioOption>()
                .Property(o => o.Cost)
                .HasPrecision(10, 2);

            modelBuilder.Entity<ScenarioOption>()
                .Property(o => o.TimeCost)
                .HasPrecision(10, 2);

            modelBuilder.Entity<ScenarioOption>()
                .Property(o => o.RiskImpact)
                .HasPrecision(10, 2);

            modelBuilder.Entity<MethodologyPhase>()
    .Property(p => p.DefaultWeight)
    .HasPrecision(5, 2);

            modelBuilder.Entity<MethodologyPhaseCriteria>()
                .Property(c => c.DefaultWeight)
                .HasPrecision(5, 2);
        }
    }
}