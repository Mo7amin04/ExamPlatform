using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ExamPlatform.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Stamps CreatedAt/CreatedBy/UpdatedAt/UpdatedBy (UTC) from the authenticated identity on every save.
/// Client-supplied audit values are never trusted.
/// </summary>
public sealed class AuditableEntityInterceptor(ICurrentUserService currentUser, TimeProvider timeProvider)
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
            return;

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var userId = currentUser.UserId;

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userId;
                    entry.Entity.UpdatedAt = null;
                    entry.Entity.UpdatedBy = null;
                    break;

                case EntityState.Modified:
                case EntityState.Unchanged when HasChangedOwnedCollections(entry):
                    entry.Property(e => e.CreatedAt).IsModified = false;
                    entry.Property(e => e.CreatedBy).IsModified = false;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    break;
            }
        }
    }

    /// <summary>Treats changes to child rows (e.g. question options, exam questions) as an update of the parent.</summary>
    private static bool HasChangedOwnedCollections(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry) =>
        entry.Collections.Any(c =>
            c.CurrentValue is not null &&
            c.CurrentValue.Cast<object>().Any(child =>
            {
                var state = entry.Context.Entry(child).State;
                return state is EntityState.Added or EntityState.Modified or EntityState.Deleted;
            }));
}
