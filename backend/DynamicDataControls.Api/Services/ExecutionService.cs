using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using DynamicDataControls.Api.Data;
using DynamicDataControls.Api.Models.Domain;
using DynamicDataControls.Api.Models.DTOs;
using DynamicDataControls.Api.Models.Entities;
using DynamicDataControls.Api.Services.Exceptions;
using DynamicDataControls.Api.Services.Interfaces;

namespace DynamicDataControls.Api.Services;

/// <summary>
/// Orchestrates the full lifecycle of a control execution:
/// SQL generation → ADO.NET execution against the target DB → result storage → history.
/// <para>
/// Results are kept in a process-level <see cref="ConcurrentDictionary{TKey,TValue}"/>
/// so they survive across HTTP requests within the same server process, while the
/// service itself is registered as Scoped (allowing <see cref="AppDbContext"/> injection).
/// </para>
/// Requirements: 4.1, 4.2, 4.3, 4.5, 4.8, 4.9, 5.7, 8.3
/// </summary>
public class ExecutionService : IExecutionService
{
    // ── Constants ─────────────────────────────────────────────────────────────

    /// <summary>Maximum rows returned to the caller; the SQL already requests TOP 10001.</summary>
    private const int RecordCap = 10_000;

    /// <summary>ADO.NET command timeout in seconds (Requirement 4.2).</summary>
    private const int CommandTimeoutSeconds = 60;

    // ── Process-level result cache (shared across scoped instances) ───────────
    // Key   = ControlExecution.Id (executionId)
    // Value = StoredExecution containing columns, records, and truncation flag
    private static readonly ConcurrentDictionary<int, StoredExecution> ResultCache = new();

    // ── Concurrent-execution guard (Requirement 4.8) ─────────────────────────
    // Key   = controlId
    // Value = true (placeholder; only presence matters)
    // A control is "active" from the moment ExecuteAsync acquires the slot until
    // the method exits (success, error, or cancellation).
    private static readonly ConcurrentDictionary<int, bool> _activeControls = new();

    // ── Per-execution CancellationTokenSource map (Requirements 4.5, 4.9) ────
    // Key   = ControlExecution.Id (executionId)
    // Value = CancellationTokenSource that can abort the in-flight SQL command
    // Populated after the execution row is persisted (so we have the executionId).
    // Removed and disposed on every exit path (success, error, cancel).
    private static readonly ConcurrentDictionary<int, CancellationTokenSource> _ctsMap = new();

    // ── Dependencies ──────────────────────────────────────────────────────────

    private readonly IControlsService         _controlsService;
    private readonly IMetadataService          _metadataService;
    private readonly IQueryBuilderService      _queryBuilderService;
    private readonly AppDbContext              _dbContext;
    private readonly IConfiguration            _configuration;
    private readonly ILogger<ExecutionService> _logger;

    public ExecutionService(
        IControlsService          controlsService,
        IMetadataService           metadataService,
        IQueryBuilderService       queryBuilderService,
        AppDbContext               dbContext,
        IConfiguration             configuration,
        ILogger<ExecutionService>  logger)
    {
        _controlsService     = controlsService;
        _metadataService     = metadataService;
        _queryBuilderService = queryBuilderService;
        _dbContext           = dbContext;
        _configuration       = configuration;
        _logger              = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ExecuteAsync
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<ExecutionResult> ExecuteAsync(int controlId, CancellationToken ct = default)
    {
        // 1. Load control definition from the system DB via ControlsService
        var (_, definition) = await _controlsService.GetByIdAsync(controlId);

        // 2. Load metadata (entities + fields, relationships)
        var entities      = await _metadataService.GetAllEntitiesWithFieldsAsync();
        var relationships = await _metadataService.GetAllRelationshipsAsync();

        // 3. Build parameterised SQL — fail fast if the definition cannot be resolved
        var buildResult = _queryBuilderService.Build(definition, entities, relationships);
        if (!buildResult.IsSuccess)
            throw new InvalidOperationException(
                $"בנייה של שאילתת SQL נכשלה: {buildResult.ErrorMessage}");

        // 4. (Req 4.8) Concurrent-execution guard — reject immediately if this control
        //    is already running in another request on this server instance.
        if (!_activeControls.TryAdd(controlId, true))
            throw new ConflictException("הרצה כבר פעילה עבור בקרה זו");

        // From this point on every exit path (success, error, cancellation) must
        // call _activeControls.TryRemove(controlId) — enforced by the outer finally.

        ControlExecution? execution = null;
        var sw = Stopwatch.StartNew();

        try
        {
            // 5. Persist audit row with Status = "Running" before touching the target DB
            execution = new ControlExecution
            {
                ControlId  = controlId,
                ExecutedAt = DateTime.UtcNow,
                Status     = "Running",
            };
            _dbContext.ControlExecutions.Add(execution);
            await _dbContext.SaveChangesAsync(ct);

            // 6. (Req 4.5 / 4.9) Create a per-execution CTS.
            //    Storing it in _ctsMap lets CancelAsync() signal cancellation from
            //    outside; linking the caller's token means the HTTP request abort also
            //    cancels the in-flight SQL command.
            var internalCts = new CancellationTokenSource();
            _ctsMap[execution.Id] = internalCts;
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, internalCts.Token);
            var linkedToken = linkedCts.Token;

            // 7. Build column-name → ColumnDefinition lookup from the loaded metadata
            var colDefLookup = BuildColumnLookup(definition, entities);

            // 8. Open ADO.NET connection to the TARGET database (not the system DB)
            var targetConnStr = _configuration.GetConnectionString("TargetDb")
                ?? throw new InvalidOperationException(
                    "מחרוזת חיבור 'TargetDb' אינה מוגדרת בהגדרות האפליקציה.");

            await using var conn = new SqlConnection(targetConnStr);
            await conn.OpenAsync(linkedToken);

            await using var cmd = new SqlCommand(buildResult.Sql!, conn)
            {
                CommandTimeout = CommandTimeoutSeconds
            };

            // Bind named parameters (@p0, @p1, …) — no string concatenation (Req 8.3)
            if (buildResult.Parameters is not null)
            {
                foreach (var (paramName, paramValue) in buildResult.Parameters)
                    cmd.Parameters.AddWithValue(paramName, paramValue);
            }

            // 9. Stream results; SQL already has TOP 10001 so the reader returns at most that many rows
            await using var reader = await cmd.ExecuteReaderAsync(linkedToken);

            List<ColumnDefinition>? columnDefs = null;
            var allRecords = new List<IDictionary<string, object?>>(RecordCap);

            while (await reader.ReadAsync(linkedToken))
            {
                // Build the ordered column-definition list from the actual reader schema
                // (done once on the first row, so it reflects the real result-set columns)
                if (columnDefs is null)
                    columnDefs = BuildColumnDefinitions(reader, colDefLookup);

                // Read one row beyond the cap to detect whether results were truncated;
                // stop reading after RecordCap + 1 rows to avoid unnecessary network traffic
                if (allRecords.Count > RecordCap)
                    break;

                var record = new Dictionary<string, object?>(reader.FieldCount);
                for (int c = 0; c < reader.FieldCount; c++)
                {
                    var key   = columnDefs[c].FriendlyName;
                    var value = reader.IsDBNull(c) ? null : reader.GetValue(c);
                    record[key] = value;
                }
                allRecords.Add(record);
            }

            sw.Stop();

            columnDefs ??= new List<ColumnDefinition>();

            // Enforce the 10,000-row cap and set the truncation flag (Req 5.7)
            bool isTruncated = allRecords.Count > RecordCap;
            if (isTruncated)
                allRecords.RemoveAt(allRecords.Count - 1); // keep exactly RecordCap rows

            int anomalyCount = allRecords.Count;

            // 10. Update audit row → Success
            execution.Status       = "Success";
            execution.AnomalyCount = anomalyCount;
            execution.RunTimeMs    = (int)sw.ElapsedMilliseconds;
            await _dbContext.SaveChangesAsync();

            // 11. Store results in the process-level cache keyed by executionId
            ResultCache[execution.Id] = new StoredExecution(
                ControlId:   controlId,
                Columns:     columnDefs,
                Records:     allRecords,
                IsTruncated: isTruncated);

            // 12. Return the execution summary to the caller
            return new ExecutionResult(
                ExecutionId:  execution.Id,
                ControlId:    controlId,
                Status:       ExecutionStatus.Success,
                AnomalyCount: anomalyCount,
                IsTruncated:  isTruncated);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            if (execution is not null)
                await UpdateExecutionStatusAsync(
                    execution, "Cancelled", errorMessage: null, (int)sw.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();

            // Log full details internally; do NOT propagate technical message to caller (Req 8.3)
            _logger.LogError(ex,
                "הרצת בקרה {ControlId} נכשלה (ExecutionId={ExecutionId})",
                controlId, execution?.Id ?? 0);

            if (execution is not null)
                await UpdateExecutionStatusAsync(
                    execution, "Error", ex.ToString(), (int)sw.ElapsedMilliseconds);
            throw;
        }
        finally
        {
            // Remove and dispose the per-execution CTS (Req 4.5 / 4.9).
            // TryRemove is a no-op if CancelAsync() already removed it.
            if (execution is not null && _ctsMap.TryRemove(execution.Id, out var removedCts))
            {
                removedCts.Cancel();
                removedCts.Dispose();
            }

            // Release the concurrent-execution slot (Req 4.8).
            _activeControls.TryRemove(controlId, out _);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CancelAsync
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task CancelAsync(int executionId)
    {
        // (Req 4.5) Signal the in-flight SQL command to abort immediately.
        // TryRemove is atomic; if ExecuteAsync's finally wins the race instead,
        // this is a no-op and the execution has already ended on its own.
        if (_ctsMap.TryRemove(executionId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }

        // Persist the Cancelled status regardless of whether the CTS was found,
        // so the audit log is always consistent (Req 4.9).
        var execution = await _dbContext.ControlExecutions.FindAsync(executionId);
        if (execution is null)
            throw new NotFoundException($"הרצה עם מזהה {executionId} לא נמצאה.");

        execution.Status = "Cancelled";
        await _dbContext.SaveChangesAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetResultsAsync
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Task<ResultsPage> GetResultsAsync(int executionId, int page, int pageSize = 100)
    {
        if (!ResultCache.TryGetValue(executionId, out var stored))
            throw new NotFoundException(
                $"תוצאות הרצה {executionId} אינן זמינות. ייתכן שהשרת הופעל מחדש לאחר ביצוע ההרצה.");

        var total     = stored.Records.Count;
        var skip      = (page - 1) * pageSize;
        var pageSlice = stored.Records.Skip(skip).Take(pageSize).ToList();

        return Task.FromResult(new ResultsPage(
            ExecutionId: executionId,
            ControlId:   stored.ControlId,
            TotalCount:  total,
            Page:        page,
            PageSize:    pageSize,
            IsTruncated: stored.IsTruncated,
            Columns:     stored.Columns,
            Records:     pageSlice));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetHistoryAsync
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ControlExecution>> GetHistoryAsync(int controlId)
    {
        return await _dbContext.ControlExecutions
            .Where(e => e.ControlId == controlId)
            .OrderByDescending(e => e.ExecutedAt)
            .ToListAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a mapping from raw SQL column name → <see cref="ColumnDefinition"/>
    /// by scanning the primary entity's fields first, then each additional entity's
    /// fields in declaration order.  <c>TryAdd</c> means the first entity wins on
    /// collision (same column name in two joined tables).
    /// </summary>
    private static Dictionary<string, ColumnDefinition> BuildColumnLookup(
        ControlDefinition                  definition,
        IReadOnlyList<BusinessEntityModel> entities)
    {
        var lookup = new Dictionary<string, ColumnDefinition>(StringComparer.OrdinalIgnoreCase);

        var primary = entities.FirstOrDefault(e =>
            string.Equals(e.FriendlyName, definition.PrimaryEntity,
                          StringComparison.OrdinalIgnoreCase));

        if (primary is not null)
            foreach (var field in primary.Fields)
                lookup.TryAdd(field.ColumnName, new ColumnDefinition(field.FriendlyName, field.DataType));

        foreach (var addName in definition.AdditionalEntities)
        {
            var addEntity = entities.FirstOrDefault(e =>
                string.Equals(e.FriendlyName, addName, StringComparison.OrdinalIgnoreCase));

            if (addEntity is null) continue;

            foreach (var field in addEntity.Fields)
                lookup.TryAdd(field.ColumnName, new ColumnDefinition(field.FriendlyName, field.DataType));
        }

        return lookup;
    }

    /// <summary>
    /// Reads the SqlDataReader's column schema to produce an ordered list of
    /// <see cref="ColumnDefinition"/> matching the actual result-set columns.
    /// Falls back to raw column name and data type "String" for any column
    /// not found in the metadata lookup.
    /// </summary>
    private static List<ColumnDefinition> BuildColumnDefinitions(
        SqlDataReader                        reader,
        Dictionary<string, ColumnDefinition> colDefLookup)
    {
        var defs = new List<ColumnDefinition>(reader.FieldCount);
        for (int c = 0; c < reader.FieldCount; c++)
        {
            var rawName = reader.GetName(c);
            defs.Add(colDefLookup.TryGetValue(rawName, out var colDef)
                ? colDef
                : new ColumnDefinition(rawName, "String"));
        }
        return defs;
    }

    /// <summary>
    /// Best-effort update of a <see cref="ControlExecution"/> terminal status.
    /// Swallows any <c>SaveChanges</c> failure to avoid masking the original exception.
    /// </summary>
    private async Task UpdateExecutionStatusAsync(
        ControlExecution execution,
        string           status,
        string?          errorMessage,
        int              runTimeMs)
    {
        execution.Status       = status;
        execution.ErrorMessage = errorMessage;
        execution.RunTimeMs    = runTimeMs;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception saveEx)
        {
            _logger.LogError(saveEx,
                "שמירת סטטוס '{Status}' נכשלה (ExecutionId={ExecutionId})",
                status, execution.Id);
        }
    }

    // ── Inner type ────────────────────────────────────────────────────────────

    /// <summary>
    /// Payload stored in <see cref="ResultCache"/> for a completed execution.
    /// </summary>
    private sealed record StoredExecution(
        int                                         ControlId,
        IReadOnlyList<ColumnDefinition>             Columns,
        IReadOnlyList<IDictionary<string, object?>> Records,
        bool                                        IsTruncated);
}
