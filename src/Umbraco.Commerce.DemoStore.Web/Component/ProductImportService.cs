using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Umbraco.Commerce.DemoStore.Web.Component;

public class ProductImportService
{
    private readonly ProductImporterLogic _depurtatLogic;
    private readonly ProductImporterLogicOtterDays _otterDaysLogic;
    private readonly ProductImporterSosetaria _sosetariaLogic;

    public ProductImportService(
        ProductImporterLogic depurtatLogic,
        ProductImporterLogicOtterDays otterDaysLogic,
        ProductImporterSosetaria sosetariaLogic)
    {
        _depurtatLogic = depurtatLogic;
        _otterDaysLogic = otterDaysLogic;
        _sosetariaLogic = sosetariaLogic;
    }

    public async Task RunAllImportsAsync()
    {
        await _depurtatLogic.RunImportAsync();
        await _otterDaysLogic.RunImportAsync();
        await _sosetariaLogic.RunImportAsync();
    }
}

