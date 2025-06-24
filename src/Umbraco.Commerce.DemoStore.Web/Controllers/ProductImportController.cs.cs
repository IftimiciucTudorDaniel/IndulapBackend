using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Umbraco.Commerce.DemoStore.Web.Component;

namespace Umbraco.Commerce.DemoStore.Web.Controllers
{
    [Route("api/importTest/[controller]")]
    [ApiController]
    public class ProductImportController : ControllerBase
    {
        private readonly BackgroundImportService _backgroundService;

        public ProductImportController(BackgroundImportService backgroundService)
        {
            _backgroundService = backgroundService;
        }

        /// <summary>
        /// Pornește procesul complet: preprocesare + import în background
        /// </summary>
        [HttpGet("start-full-process")]
        public async Task<IActionResult> StartFullProcess()
        {
            if (_backgroundService.IsRunning)
            {
                return Ok(new
                {
                    success = false,
                    message = "Un proces este deja în curs...",
                    isRunning = true,
                    currentStatus = _backgroundService.LastResult
                });
            }

            await _backgroundService.StartFullProcessAsync();

            return Ok(new
            {
                success = true,
                message = "Procesul complet (preprocesare + import) a început în background!",
                isRunning = true,
                info = "Folosește /status pentru a verifica progresul"
            });
        }

        /// <summary>
        /// Pornește doar importul în background (presupune că XML-urile sunt deja procesate)
        /// </summary>
        [HttpGet("start-import-only")]
        public async Task<IActionResult> StartImportOnly()
        {
            if (_backgroundService.IsRunning)
            {
                return Ok(new
                {
                    success = false,
                    message = "Un import este deja în curs...",
                    isRunning = true,
                    currentStatus = _backgroundService.LastResult
                });
            }

            await _backgroundService.StartImportOnlyAsync();

            return Ok(new
            {
                success = true,
                message = "Importul a început în background!",
                isRunning = true,
                info = "Folosește /status pentru a verifica progresul"
            });
        }

        /// <summary>
        /// Verifică statusul procesului curent
        /// </summary>
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok(new
            {
                isRunning = _backgroundService.IsRunning,
                lastResult = _backgroundService.LastResult,
                lastRunTime = _backgroundService.LastRunTime,
                status = _backgroundService.IsRunning ? "În progres..." : "Inactiv"
            });
        }

        /// <summary>
        /// Endpoint pentru testare rapidă
        /// </summary>
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                message = "ProductImport API funcționează!",
                timestamp = System.DateTime.Now,
                availableEndpoints = new[]
                {
                    "/start-full-process - Pornește preprocesare + import",
                    "/start-import-only - Pornește doar import",
                    "/status - Verifică statusul",
                    "/health - Health check"
                }
            });
        }
    }
}