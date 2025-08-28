using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core;
using Umbraco.Extensions;

namespace InDulap.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NavigationApiController : UmbracoApiController
    {
        private readonly IPublishedContentQuery _contentQuery;

        public NavigationApiController(IPublishedContentQuery contentQuery)
        {
            _contentQuery = contentQuery;
        }

        [HttpGet("menu-data")]
        public IActionResult GetNavigationMenuData()
        {
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

            return Ok(navigationData);
        }

        [HttpGet("top-products")]
        public IActionResult GetTopProductsForMenu(int take = 4)
        {
            var root = _contentQuery.ContentAtRoot().FirstOrDefault();
            if (root == null)
                return NotFound("Root content not found");

            var topProducts = root.DescendantsOrSelfOfType("productPage")
                .Take(take)
                .Select(MapProduct)
                .ToList();

            return Ok(topProducts);
        }

        [HttpGet("all-time-top-products")]
        public IActionResult GetAllTimeTopProducts(int take = 4)
        {
            var root = _contentQuery.ContentAtRoot().FirstOrDefault();
            if (root == null)
                return NotFound("Root content not found");

            var allTimeTopProducts = root.DescendantsOrSelfOfType("productPage")
                .Take(take)
                .Select(MapProduct)
                .ToList();

            return Ok(allTimeTopProducts);
        }

        #region Helpers

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
