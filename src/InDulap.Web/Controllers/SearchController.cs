using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;
using System.Linq;
using System.Collections.Generic;
using Umbraco.Cms.Core;

namespace MyProject.Controllers
{
    [ApiController]
    [Route("/umbraco/delivery/api/search")]
    public class SearchProductsController : UmbracoApiController
    {
        private readonly IPublishedContentQuery _contentQuery;

        public SearchProductsController(IPublishedContentQuery contentQuery)
        {
            _contentQuery = contentQuery;
        }

        [HttpGet]
        public IActionResult Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest("Query cannot be empty.");

            var terms = q.ToLower().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

            var root = _contentQuery.ContentAtRoot().FirstOrDefault();
            if (root == null)
                return NotFound("Root content not found");

            var allProducts = root.DescendantsOrSelfOfType("productPage");

            var results = allProducts.Where(p =>
            {
                var name = p.Name?.ToLower() ?? "";
                var brand = p.Value<string>("brand")?.ToLower() ?? "";
                var color = p.Value<string>("color")?.ToLower() ?? "";
                var categoryNames = p.Value<IEnumerable<IPublishedContent>>("categories")?.Select(c => c.Name.ToLower()) ?? Enumerable.Empty<string>();

                return terms.All(term =>
                    name.Contains(term) ||
                    brand.Contains(term) ||
                    color.Contains(term) ||
                    categoryNames.Any(cat => cat.Contains(term))
                );
            });

            var data = results.Take(32).Select(p => new
            {
                id = p.Key,
                title = p.Name,
                imageUrl1 = p.Value<string>("image1") ?? "",
                imageUrl2 = p.Value<string>("image2") ?? "",
                alt = p.Name,
                affLink = p.Value<string>("affLink") ?? "",
                price = p.Value<decimal?>("price") ?? 0,
                brands = p.Value<string>("brand") ?? "",
                color = p.Value<string>("color") ?? "",
                category = p.Value<IEnumerable<IPublishedContent>>("categories")?.FirstOrDefault()?.Name ?? "",
                collection = p.AncestorsOrSelf()
                    .FirstOrDefault(a => a.ContentType.Alias == "collectionPage")?.UrlSegment ?? "",
                gen = p.Value<string>("gen") ?? ""
            });

            return Ok(data);
        }
    }
}
