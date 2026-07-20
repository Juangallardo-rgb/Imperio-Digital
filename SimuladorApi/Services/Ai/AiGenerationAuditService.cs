using Microsoft.EntityFrameworkCore;
using SimuladorApi.Data;
using SimuladorApi.Models;

namespace SimuladorApi.Services.Ai;

public sealed class AiGenerationAuditService
{
    private readonly AppDbContext _context;

    public AiGenerationAuditService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AiGenerationRecord> StartAsync(
        int requestedByUserId,
        string operationType,
        string requestedModel,
        string promptVersion,
        int? scenarioId = null,
        CancellationToken cancellationToken = default,
        string? methodologyCode = null,
        DateTime? expiresAt = null)
    {
        var record = new AiGenerationRecord
        {
            ScenarioId = scenarioId,
            RequestedByUserId = requestedByUserId,
            OperationType = operationType,
            MethodologyCode = methodologyCode,
            RequestedModel = requestedModel,
            PromptVersion = promptVersion,
            Status = "Started",
            StartedAt = DateTime.UtcNow,
            CorrelationId = Guid.NewGuid(),
            ExpiresAt = expiresAt
        };
        _context.AiGenerationRecords.Add(record);
        await _context.SaveChangesAsync(cancellationToken);
        return record;
    }

    public async Task CompleteAsync(
        AiGenerationRecord record,
        bool success,
        string? effectiveModel,
        int retryCount,
        string? promptHash,
        string? responseHash = null,
        string? errorCode = null,
        string? errorMessage = null,
        string responseFormat = "none",
        CancellationToken cancellationToken = default)
    {
        record.Status = success ? "Succeeded" : "Failed";
        record.CompletedAt = DateTime.UtcNow;
        record.EffectiveModel = effectiveModel;
        record.RetryCount = retryCount;
        record.PromptHash = promptHash;
        record.ResponseHash = responseHash;
        record.ErrorCode = errorCode;
        record.ErrorMessage = Sanitize(errorMessage);
        record.ResponseFormat = string.IsNullOrWhiteSpace(responseFormat) ? "none" : responseFormat;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteDraftSuccessAsync(
        AiGenerationRecord record,
        string? effectiveModel,
        int retryCount,
        string? promptHash,
        string? responseHash,
        string responseFormat,
        CancellationToken cancellationToken = default)
    {
        var previousDrafts = await _context.AiGenerationRecords
            .Where(candidate => candidate.Id != record.Id &&
                candidate.RequestedByUserId == record.RequestedByUserId &&
                candidate.OperationType == "ScenarioDraft" &&
                candidate.MethodologyCode == record.MethodologyCode &&
                candidate.Status == "Succeeded" &&
                candidate.ScenarioId == null &&
                candidate.ConsumedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var previous in previousDrafts)
        {
            previous.Status = "Superseded";
        }

        record.Status = "Succeeded";
        record.CompletedAt = DateTime.UtcNow;
        record.EffectiveModel = effectiveModel;
        record.RetryCount = retryCount;
        record.PromptHash = promptHash;
        record.ResponseHash = responseHash;
        record.ResponseFormat = responseFormat;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<AiGenerationRecord?> FindSuccessfulDraftAsync(
        Guid correlationId,
        int requestedByUserId,
        string methodologyCode,
        CancellationToken cancellationToken = default)
    {
        var record = await _context.AiGenerationRecords.FirstOrDefaultAsync(
            candidate => candidate.CorrelationId == correlationId,
            cancellationToken);
        return record is not null && IsUsableDraft(
            record,
            correlationId,
            requestedByUserId,
            methodologyCode,
            DateTime.UtcNow)
            ? record
            : null;
    }

    public static bool IsUsableDraft(
        AiGenerationRecord record,
        Guid correlationId,
        int requestedByUserId,
        string methodologyCode,
        DateTime utcNow) =>
        record.CorrelationId == correlationId &&
        record.RequestedByUserId == requestedByUserId &&
        string.Equals(record.MethodologyCode, methodologyCode, StringComparison.Ordinal) &&
        record.ScenarioId == null &&
        record.OperationType == "ScenarioDraft" &&
        record.Status == "Succeeded" &&
        record.ConsumedAt == null &&
        record.ExpiresAt.HasValue &&
        record.ExpiresAt.Value > utcNow;

    private static string? Sanitize(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }
        return message.Trim().Length <= 300
            ? message.Trim()
            : message.Trim()[..300];
    }
}
