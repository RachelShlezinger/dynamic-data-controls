using Microsoft.EntityFrameworkCore;
using DynamicDataControls.Api.Data;
using DynamicDataControls.Api.Models.DTOs;
using DynamicDataControls.Api.Models.Entities;
using DynamicDataControls.Api.Repositories.Interfaces;

namespace DynamicDataControls.Api.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IControlsRepository"/>.
/// <para>
/// <b>GetAllAsync</b> projects directly to <see cref="ControlListItem"/> so that
/// EF Core generates a single SQL statement with a correlated sub-select for the
/// most-recent execution timestamp — no N+1 queries.
/// </para>
/// <para>
/// Case-insensitive name matching relies on the <c>Hebrew_CI_AS</c> collation
/// defined on <c>Controls.Name</c> in the database schema; a plain <c>==</c>
/// comparison or <see cref="EF.Functions.Like"/> will produce a case-insensitive
/// predicate automatically.
/// </para>
/// Requirements: 1.5, 1.6
/// </summary>
public class ControlsRepository : IControlsRepository
{
    private readonly AppDbContext _db;

    public ControlsRepository(AppDbContext db) => _db = db;

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ControlListItem>> GetAllAsync(string? search)
    {
        var query = _db.Controls.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            // Hebrew_CI_AS collation on the Name column makes LIKE case-insensitive.
            // Description has no explicit collation, so it falls back to the database
            // default (also CI_AS in the reference schema).
            var pattern = $"%{search}%";
            query = query.Where(c =>
                EF.Functions.Like(c.Name, pattern) ||
                (c.Description != null && EF.Functions.Like(c.Description, pattern)));
        }

        return await query
            .OrderBy(c => c.Name)
            .Select(c => new ControlListItem(
                c.Id,
                c.Name,
                // Truncate to 100 chars in SQL — SUBSTRING(Description, 1, 100) on SQL Server.
                // SQL Server's SUBSTRING returns the full string when it is shorter than 100 chars.
                c.Description != null ? c.Description.Substring(0, 100) : null,
                c.CreatedAt,
                // Correlated sub-select: (SELECT TOP 1 ExecutedAt FROM ControlExecutions
                //   WHERE ControlId = c.Id ORDER BY ExecutedAt DESC)
                c.Executions
                    .OrderByDescending(e => e.ExecutedAt)
                    .Select(e => (DateTime?)e.ExecutedAt)
                    .FirstOrDefault()))
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<ControlEntity?> GetByIdAsync(int id)
        => await _db.Controls
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == id);

    // ── Commands ──────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<ControlEntity> CreateAsync(ControlEntity control)
    {
        _db.Controls.Add(control);
        await _db.SaveChangesAsync();
        return control;
    }

    /// <inheritdoc/>
    public async Task<ControlEntity> UpdateAsync(ControlEntity control)
    {
        // Always stamp UpdatedAt so callers never forget.
        control.UpdatedAt = DateTime.UtcNow;
        _db.Controls.Update(control);
        await _db.SaveChangesAsync();
        return control;
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(int id)
    {
        var control = await _db.Controls.FindAsync(id)
            ?? throw new KeyNotFoundException($"Control with id {id} was not found.");

        _db.Controls.Remove(control);
        await _db.SaveChangesAsync();
    }

    // ── Uniqueness check ──────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<bool> NameExistsAsync(string name, int? excludeId = null)
        // Hebrew_CI_AS collation on the Name column means == is already case-insensitive
        // at the database level — no ToLower() needed and no collation hint required.
        => await _db.Controls.AnyAsync(c =>
            c.Name == name &&
            (excludeId == null || c.Id != excludeId));
}
