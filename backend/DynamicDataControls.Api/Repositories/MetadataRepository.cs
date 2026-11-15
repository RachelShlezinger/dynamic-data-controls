using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using DynamicDataControls.Api.Data;
using DynamicDataControls.Api.Models.Entities;
using DynamicDataControls.Api.Repositories.Interfaces;

namespace DynamicDataControls.Api.Repositories;

/// <summary>
/// EF Core + ADO.NET implementation of <see cref="IMetadataRepository"/>.
/// <para>
/// Entity operations (BusinessEntities, BusinessFields, EntityRelationships) go
/// through <see cref="AppDbContext"/> so EF change tracking and migrations are
/// the single source of truth for the system DB schema.
/// </para>
/// <para>
/// Target-DB introspection (<c>TableExistsInTargetDbAsync</c>,
/// <c>ColumnExistsInTargetDbAsync</c>) uses raw ADO.NET against the separate
/// "TargetDb" connection string.  All parameters are passed via
/// <see cref="SqlParameter"/> — no string concatenation is used.
/// </para>
/// Requirements: 6.5, 6.7
/// </summary>
public class MetadataRepository : IMetadataRepository
{
    private readonly AppDbContext _db;
    private readonly string _targetConnectionString;

    public MetadataRepository(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        _targetConnectionString = configuration.GetConnectionString("TargetDb")
            ?? throw new InvalidOperationException(
                "Required connection string 'TargetDb' is not configured in appsettings.");
    }

    // ── BusinessEntities ──────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BusinessEntityModel>> GetAllEntitiesAsync()
        => await _db.BusinessEntities
                    .AsNoTracking()
                    .OrderBy(e => e.FriendlyName)
                    .ToListAsync();

    /// <inheritdoc/>
    public async Task<BusinessEntityModel?> GetEntityByIdAsync(int id)
        => await _db.BusinessEntities
                    .AsNoTracking()
                    .Include(e => e.Fields)
                    .FirstOrDefaultAsync(e => e.Id == id);

    /// <inheritdoc/>
    public async Task<BusinessEntityModel?> GetEntityByNameAsync(string friendlyName)
        => await _db.BusinessEntities
                    .AsNoTracking()
                    .Include(e => e.Fields)
                    .FirstOrDefaultAsync(e => e.FriendlyName == friendlyName);

    /// <inheritdoc/>
    public async Task<BusinessEntityModel> CreateEntityAsync(BusinessEntityModel entity)
    {
        _db.BusinessEntities.Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    // ── BusinessFields ─────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BusinessField>> GetFieldsByEntityIdAsync(int entityId)
        => await _db.BusinessFields
                    .AsNoTracking()
                    .Where(f => f.EntityId == entityId)
                    .OrderBy(f => f.FriendlyName)
                    .ToListAsync();

    /// <inheritdoc/>
    public async Task<BusinessField> CreateFieldAsync(BusinessField field)
    {
        _db.BusinessFields.Add(field);
        await _db.SaveChangesAsync();
        return field;
    }

    // ── EntityRelationships ────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<IReadOnlyList<EntityRelationship>> GetAllRelationshipsAsync()
        => await _db.EntityRelationships
                    .AsNoTracking()
                    .Include(r => r.SourceEntity)
                    .Include(r => r.TargetEntity)
                    .Include(r => r.SourceField)
                    .Include(r => r.TargetField)
                    .ToListAsync();

    /// <inheritdoc/>
    public async Task<EntityRelationship> CreateRelationshipAsync(EntityRelationship rel)
    {
        _db.EntityRelationships.Add(rel);
        await _db.SaveChangesAsync();
        return rel;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BusinessEntityModel>> GetAllEntitiesWithFieldsAsync()
        => await _db.BusinessEntities
                    .AsNoTracking()
                    .Include(e => e.Fields)
                    .OrderBy(e => e.FriendlyName)
                    .ToListAsync();

    // ── Target DB Introspection ────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<bool> TableExistsInTargetDbAsync(string tableName)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_NAME = @tableName
            """;

        await using var connection = new SqlConnection(_targetConnectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@tableName", System.Data.SqlDbType.NVarChar, 128)
        {
            Value = tableName
        });

        var count = (int)(await command.ExecuteScalarAsync() ?? 0);
        return count > 0;
    }

    /// <inheritdoc/>
    public async Task<bool> ColumnExistsInTargetDbAsync(string tableName, string columnName)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME  = @tableName
              AND COLUMN_NAME = @columnName
            """;

        await using var connection = new SqlConnection(_targetConnectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@tableName", System.Data.SqlDbType.NVarChar, 128)
        {
            Value = tableName
        });
        command.Parameters.Add(new SqlParameter("@columnName", System.Data.SqlDbType.NVarChar, 128)
        {
            Value = columnName
        });

        var count = (int)(await command.ExecuteScalarAsync() ?? 0);
        return count > 0;
    }
}
