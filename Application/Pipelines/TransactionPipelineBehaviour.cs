using Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines
{
    public class TransactionPipelineBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
            where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<TransactionPipelineBehaviour<TRequest, TResponse>> _logger;
        // DbContext'i direkt alıyoruz çünkü BeginTransactionAsync metodu IUnitOfWork'te yok.
        private readonly ApplicationDbContext _dbContext;

        public TransactionPipelineBehaviour(
            ILogger<TransactionPipelineBehaviour<TRequest, TResponse>> logger,
            ApplicationDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            // İsteğin ICommand veya ICommand<T> arayüzlerinden birini uygulayıp uygulamadığını kontrol et
            var isCommand = typeof(TRequest).GetInterfaces().Any(i =>
                i == typeof(ICommand) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>)));

            // Eğer bir Command değilse (örn: bir Query ise), transaction başlatma, devam et.
            if (!isCommand)
            {
                return await next();
            }

            // Zaten var olan bir transaction varsa ona katıl, yoksa yenisini başlatma.
            // (DbContext, SaveChangesAsync çağrıldığında ambient transaction'ı kullanır)
            // Bu basit senaryoda, iç içe transaction'ları önlemek için mevcut bir
            // transaction olup olmadığını kontrol edebiliriz.
            if (_dbContext.Database.CurrentTransaction != null)
            {
                return await next();
            }

            var requestName = typeof(TRequest).Name;

            try
            {
                // Transaction başlat
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                _logger.LogInformation("Transaction Başlatıldı: {RequestName}", requestName);

                // Asıl Handler'ı (ve içindeki SaveChangesAsync'i) çalıştır
                var response = await next();

                // Handler başarılı olursa transaction'ı onayla (Commit)
                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Transaction Onaylandı (Commit): {RequestName}", requestName);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transaction Sırasında Hata: {RequestName}. Transaction Geri Alınıyor (Rollback)...", requestName);

                // Hata durumunda transaction'ı geri al (Rollback)
                await _dbContext.Database.RollbackTransactionAsync(cancellationToken);

                throw; // Hatayı fırlat ki üst katmanlar (ve GlobalExceptionMiddleware) yakalasın
            }
        }
    }
}
