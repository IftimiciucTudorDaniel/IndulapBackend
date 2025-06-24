using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Umbraco.Commerce.DemoStore.Web.Component;

namespace Umbraco.Commerce.DemoStore.Web.Component
{
    public class BackgroundImportService
    {
        private readonly ProductImporterLogic _importer;
        private readonly ILogger<BackgroundImportService> _logger;
        private bool _isRunning = false;
        private string _lastResult = "";
        private DateTime? _lastRunTime = null;

        public BackgroundImportService(ProductImporterLogic importer, ILogger<BackgroundImportService> logger)
        {
            _importer = importer;
            _logger = logger;
        }

        public bool IsRunning => _isRunning;
        public string LastResult => _lastResult;
        public DateTime? LastRunTime => _lastRunTime;

        public async Task StartFullProcessAsync()
        {
            if (_isRunning)
            {
                _logger.LogWarning("Import deja în curs...");
                return;
            }

            _isRunning = true;
            _lastResult = "În curs...";

            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("🚀 Începe procesul complet în background");

                    // 1. Preprocesare XML-uri
                    _logger.LogInformation("📝 Preprocesare XML-uri...");
                    var otterPreprocessor = new ProductPreprocessorOtterDays();
                    var depurtatPreprocessor = new ProductPreprocessorDepurtat();
                    var sosetariaPreprocessor = new ProductPreprocessorSosetaria();
                    var fashionDaysPreprocessor = new FashionDaysPreprocessor();

                    await otterPreprocessor.GeneratePreprocessedXmlAsync();
                    await depurtatPreprocessor.GeneratePreprocessedXmlAsync();
                    await sosetariaPreprocessor.GeneratePreprocessedXmlAsync();
                    await fashionDaysPreprocessor.GeneratePreprocessedXmlAsync();

                    _logger.LogInformation("✅ Preprocesare completă");

                    // 2. Import produse
                    _logger.LogInformation("📦 Import produse...");
                    await _importer.RunImportAsync();

                    _lastResult = "✅ Procesul complet finalizat cu succes!";
                    _lastRunTime = DateTime.Now;
                    _logger.LogInformation("🎉 Procesul complet finalizat cu succes!");
                }
                catch (Exception ex)
                {
                    _lastResult = $"❌ Eroare: {ex.Message}";
                    _lastRunTime = DateTime.Now;
                    _logger.LogError(ex, "❌ Eroare în procesul background");
                }
                finally
                {
                    _isRunning = false;
                }
            });
        }

        public async Task StartImportOnlyAsync()
        {
            if (_isRunning)
            {
                _logger.LogWarning("Import deja în curs...");
                return;
            }

            _isRunning = true;
            _lastResult = "Import în curs...";

            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("📦 Începe importul în background");
                    await _importer.RunImportAsync();

                    _lastResult = "✅ Import finalizat cu succes!";
                    _lastRunTime = DateTime.Now;
                    _logger.LogInformation("✅ Import background finalizat");
                }
                catch (Exception ex)
                {
                    _lastResult = $"❌ Eroare import: {ex.Message}";
                    _lastRunTime = DateTime.Now;
                    _logger.LogError(ex, "❌ Eroare import background");
                }
                finally
                {
                    _isRunning = false;
                }
            });
        }
    }
}