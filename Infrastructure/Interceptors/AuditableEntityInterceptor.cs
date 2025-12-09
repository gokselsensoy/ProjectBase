using Application.Abstractions.Services;
using Domain.SeedWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Interceptors
{
    public class AuditableEntityInterceptor : SaveChangesInterceptor
    {
        private readonly ICurrentUserService _currentUserService;

        public AuditableEntityInterceptor(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            UpdateEntities(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            UpdateEntities(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void UpdateEntities(DbContext? context)
        {
            if (context == null) return;

            var userId = _currentUserService.UserId;
            var utcNow = DateTime.UtcNow;

            foreach (var entry in context.ChangeTracker.Entries<Entity>())
            {
                // 1. AUDIT (Oluşturma/Güncelleme Zamanları)
                if (entry.Entity is IAuditableEntity auditable)
                {
                    if (entry.State == EntityState.Added)
                    {
                        auditable.CreatedAt = utcNow;
                        auditable.CreatedBy = userId != Guid.Empty ? userId : null;
                    }

                    if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
                    {
                        auditable.LastModifiedAt = utcNow;
                        auditable.LastModifiedBy = userId != Guid.Empty ? userId : null;
                    }
                }

                // 2. SOFT DELETE
                // Not: Sadece ISoftDeletableEntity olanları yakalıyoruz
                if (entry.State == EntityState.Deleted && entry.Entity is ISoftDeletableEntity softDelete)
                {
                    entry.State = EntityState.Modified; // Silmeyi iptal et, güncellemeye çevir

                    softDelete.IsDeleted = true;
                    softDelete.DeletedAt = utcNow;
                    softDelete.DeletedBy = userId != Guid.Empty ? userId : null;

                    // Eğer FullAuditedEntity ise IsActive özelliğini de kapatabiliriz
                    if (entry.Entity is FullAuditedEntity fullAudit)
                    {
                        fullAudit.IsActive = false;
                    }
                }
            }
        }
    }
}
