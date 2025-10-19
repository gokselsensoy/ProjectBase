using Domain.Entities;
using Domain.SeedWork;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Infrastructure.Persistence.Context
{
    public class ApplicationDbContext : DbContext, IUnitOfWork
    {
        private readonly IPublisher _publisher;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IPublisher publisher) : base(options)
        {
            _publisher = publisher;
        }

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            base.OnModelCreating(modelBuilder);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // 1. Domain Event'leri Fırlat
            await PublishDomainEventsAsync(cancellationToken);

            // 2. Değişiklikleri veritabanına kaydet
            return await base.SaveChangesAsync(cancellationToken);
        }

        private async Task PublishDomainEventsAsync(CancellationToken cancellationToken)
        {
            // Takip edilen (tracked) Entity'leri bul
            var domainEntities = ChangeTracker
                .Entries<Entity>()
                .Select(entry => entry.Entity)
                .Where(entity => entity.DomainEvents.Any())
                .ToList();

            // Tüm event'leri topla
            var domainEvents = domainEntities
                .SelectMany(entity => entity.DomainEvents)
                .ToList();

            // Event'leri temizle ki tekrar fırlatılmasın
            domainEntities.ForEach(entity => entity.ClearDomainEvents());

            // MediatR aracılığıyla event'leri "yayınla" (publish)
            // Bu event'leri dinleyen Handler'lar (örn: OrderCreatedEmailHandler) çalışır
            foreach (var domainEvent in domainEvents)
            {
                await _publisher.Publish(domainEvent, cancellationToken);
            }
        }
    }
}
