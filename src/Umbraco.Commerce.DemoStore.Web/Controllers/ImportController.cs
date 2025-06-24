using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Commerce.DemoStore.Web.Component;

namespace Umbraco.Commerce.DemoStore.Web.Controllers
{
    [Route("umbraco/api/import")]
    public class ImportController : UmbracoApiController
    {
        private readonly ProductImportService _importService;

        public ImportController(ProductImportService importService)
        {
            _importService = importService;
        }

        [HttpGet("run")]
        public async Task<IActionResult> Run([FromQuery] string key)
        {
            if (key != "Y6d3pR7sX9tLmW2q") 
                return Unauthorized("Invalid key.");

            await _importService.RunAllImportsAsync();

            return Ok("✅ All imports completed.");
        }
    }
}
