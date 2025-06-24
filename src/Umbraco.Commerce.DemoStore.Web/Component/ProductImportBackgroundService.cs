using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Umbraco.Commerce.DemoStore.Web.Component;

public class ProductImportBackgroundService : BackgroundService
{
    private readonly ProductImportService _importService;
    private readonly ILogger<ProductImportBackgroundService> _logger;

    public ProductImportBackgroundService(
        ProductImportService importService,
        ILogger<ProductImportBackgroundService> logger)
    {
        _importService = importService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🕒 Pornim importul din background service...");
        await _importService.RunAllImportsAsync();
    }
}
