using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core;
using Umbraco.Extensions;
using InDulap.Web.Services;
using System.Threading.Tasks;

namespace InDulap.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NavigationApiController : UmbracoApiController
    {

        #region Constructor

        public NavigationApiController(IPublishedContentQuery contentQuery,
                                       RedisService redisService)
        {
            _contentQuery = contentQuery;
            _redisService = redisService;
        }

        #endregion

        #region Properties

        private readonly IPublishedContentQuery _contentQuery;
        private readonly RedisService _redisService;

        #endregion

        #region Public methods

        /// <summary>
        /// Retrieves navigation menu data, including categories and collections, for the application.
        /// </summary>
        /// <remarks>This method first attempts to retrieve cached menu data from Redis. If no cached data
        /// is found,  it queries the content tree to generate the navigation menu data, which includes categories and 
        /// collections grouped by gender. The generated data is then cached in Redis for subsequent requests.</remarks>
        /// <returns>An <see cref="IActionResult"/> containing the navigation menu data in JSON format.  Returns <see
        /// cref="NotFoundResult"/> if the root content is not found.</returns>
        [HttpGet("menu-data")]
        public async Task<IActionResult> GetNavigationMenuData()
        {
            var cached = _redisService.GetStringAsync("INDULAP_MENU_DATA").Result;
            if (!string.IsNullOrEmpty(cached))
            {
                var cachedResult = System.Text.Json.JsonSerializer.Deserialize<object>(cached);
                return Ok(cachedResult);
            }
            var root = _contentQuery.ContentAtRoot().FirstOrDefault();
            if (root == null)
                return NotFound("Root content not found");

            // preload all content once
            var allProducts = root.DescendantsOrSelfOfType("productPage").ToList();
            var allCategories = root.DescendantsOrSelfOfType("categoryPage").ToList();
            var allCollections = root.DescendantsOrSelfOfType("collectionPage").ToList();

            var categories = GetCategoriesByGender(allCategories, allProducts);
            var collectionsWithGenders = GetCollectionsWithGenders(allCollections, allProducts);

            var navigationData = new
            {
                categories,
                collectionsWithGenders
            };
            await _redisService.SetStringAsync("INDULAP_MENU_DATA",
                System.Text.Json.JsonSerializer.Serialize(navigationData),
                TimeSpan.FromHours(12));
            return Ok(navigationData);
        }

        /// <summary>
        /// Retrieves the top products for display on the menu.
        /// </summary>
        /// <remarks>The method first attempts to retrieve the top products from a Redis cache. If no
        /// cached data is found,  it queries the content tree for product pages, maps the results, and caches them for
        /// 12 hours.  This method is designed to optimize performance by leveraging caching.</remarks>
        /// <param name="take">The maximum number of products to retrieve. Defaults to 4.</param>
        /// <returns>An <see cref="IActionResult"/> containing the top products as a JSON object.  If cached data is available,
        /// the cached result is returned. If no root content is found,  a 404 Not Found response is returned.</returns>
        [HttpGet("top-products")]
        public async Task<IActionResult> GetTopProductsForMenu(int take = 4)
        {
            var cached = _redisService.GetStringAsync("INDULAP_TOP_PRODUCTS").Result;
            if (!string.IsNullOrEmpty(cached))
            {
                var cachedResult = System.Text.Json.JsonSerializer.Deserialize<object>(cached);
                return Ok(cachedResult);
            }
            var root = _contentQuery.ContentAtRoot().FirstOrDefault();
            if (root == null)
                return NotFound("Root content not found");

            var topProducts = root.DescendantsOrSelfOfType("productPage")
                .Take(take)
                .Select(MapProduct)
                .ToList();
            await _redisService.SetStringAsync("INDULAP_TOP_PRODUCTS",
                System.Text.Json.JsonSerializer.Serialize(topProducts),
                TimeSpan.FromHours(12));
            return Ok(topProducts);
        }

        /// <summary>
        /// Retrieves the top products of all time, limited to a specified number of results.
        /// </summary>
        /// <remarks>The method retrieves the top products from the content hierarchy and caches the
        /// result for 12 hours  to improve performance. If cached data is available, it is returned directly without
        /// querying the content hierarchy.</remarks>
        /// <param name="take">The maximum number of top products to retrieve. Defaults to 4.</param>
        /// <returns>An <see cref="IActionResult"/> containing the list of top products.  If the data is cached, the cached
        /// result is returned. If no root content is found, a 404 response is returned.</returns>
        [HttpGet("all-time-top-products")]
        public async Task<IActionResult> GetAllTimeTopProducts(int take = 4)
        {
            var cached = _redisService.GetStringAsync("INDULAP_ALL_TIME_TOP_PRODUCTS").Result;
            if (!string.IsNullOrEmpty(cached))
            {
                var cachedResult = System.Text.Json.JsonSerializer.Deserialize<object>(cached);
                return Ok(cachedResult);
            }
            var root = _contentQuery.ContentAtRoot().FirstOrDefault();
            if (root == null)
                return NotFound("Root content not found");

            var allTimeTopProducts = root.DescendantsOrSelfOfType("productPage")
                .Take(take)
                .Select(MapProduct)
                .ToList();
            await _redisService.SetStringAsync("INDULAP_ALL_TIME_TOP_PRODUCTS",
                System.Text.Json.JsonSerializer.Serialize(allTimeTopProducts),
                TimeSpan.FromHours(12));
            return Ok(allTimeTopProducts);
        }
        #endregion

        #region Private methods

        private object MapProduct(IPublishedContent p) => new
        {
            id = p.Key,
            title = p.Name,
            link = p.Url(),
            imageUrl1 = p.Value<string>("image1") ?? "",
            imageUrl2 = p.Value<string>("image2") ?? "",
            price = p.Value<decimal?>("price") ?? 0
        };

        private object GetCategoriesByGender(List<IPublishedContent> allCategories, List<IPublishedContent> allProducts)
        {
            return new
            {
                femei = GetCategoriesForGender(allCategories, allProducts, "femei"),
                barbati = GetCategoriesForGender(allCategories, allProducts, "barbati"),
                fetite = GetCategoriesForGender(allCategories, allProducts, "fetite"),
                baieti = GetCategoriesForGender(allCategories, allProducts, "baieti")
            };
        }

        private List<object> GetCategoriesForGender(List<IPublishedContent> allCategories, List<IPublishedContent> allProducts, string gender)
        {
            var genderCategories = allCategories
                .Where(c => c.Name.ToLowerInvariant().Contains(gender))
                .Where(c => HasProducts(allProducts, c.Name))
                .Select(c => new
                {
                    name = c.Name,
                    href = $"/{gender}/{NormalizeSlug(c.Name.Replace($" - {gender}", "", StringComparison.OrdinalIgnoreCase))}"
                })
                .GroupBy(c => c.name.ToLowerInvariant())
                .Select(g => g.First())
                .ToList<object>();

            return genderCategories;
        }

        private List<object> GetCollectionsWithGenders(List<IPublishedContent> allCollections, List<IPublishedContent> allProducts)
        {
            var collections = allCollections
                .Select(collection => new
                {
                    name = collection.Name,
                    link = $"/colectii/{NormalizeSlug(collection.Name)}",
                    imageUrl = GetImageUrl(collection.Value<string>("image")),
                    alt = collection.Name,
                    description = collection.Value<string>("description") ?? "",
                    genders = GetGendersForCollection(allProducts, collection)
                })
                .Where(c => c.genders.Any())
                .GroupBy(c => c.name.ToLowerInvariant())
                .Select(g => g.First())
                .ToList<object>();

            return collections;
        }

        private List<object> GetGendersForCollection(List<IPublishedContent> allProducts, IPublishedContent collection)
        {
            var genders = new[] { "femei", "barbati", "fetite", "baieti" };

            return genders
                .Where(gender => allProducts.Any(p =>
                    p.AncestorsOrSelf().Any(a => a.Id == collection.Id) &&
                    p.Value<string>("gen")?.Equals(gender, StringComparison.OrdinalIgnoreCase) == true))
                .Select(gender => new
                {
                    name = char.ToUpper(gender[0]) + gender.Substring(1),
                    gender,
                    link = $"/colectii/{NormalizeSlug(collection.Name)}/{gender}",
                    productCount = allProducts.Count(p =>
                        p.AncestorsOrSelf().Any(a => a.Id == collection.Id) &&
                        p.Value<string>("gen")?.Equals(gender, StringComparison.OrdinalIgnoreCase) == true)
                })
                .ToList<object>();
        }

        private bool HasProducts(List<IPublishedContent> allProducts, string searchTerm)
        {
            var search = searchTerm.ToLowerInvariant();
            return allProducts.Any(p =>
                p.Name.ToLowerInvariant().Contains(search) ||
                p.Value<string>("brand")?.ToLowerInvariant().Contains(search) == true ||
                (p.Value<IEnumerable<IPublishedContent>>("categories")?.Any(c =>
                    c.Name.ToLowerInvariant().Contains(search)) ?? false)
            );
        }

        private string NormalizeSlug(string? input)
        {
            return input?.ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("ă", "a")
                .Replace("â", "a")
                .Replace("î", "i")
                .Replace("ș", "s")
                .Replace("ț", "t") ?? "";
        }

        private string GetImageUrl(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
                return null;

            return imagePath.StartsWith("http")
                ? imagePath
                : $"https://indulap-001-site1.mtempurl.com{imagePath}";
        }

        #endregion
    }
}
