using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

// [Route("api/products")]
// public class ProductsApiController : UmbracoApiController
// {
//     private readonly IPublishedContentQuery _contentQuery;
//
//     public ProductsApiController(IPublishedContentQuery contentQuery)
//     {
//         _contentQuery = contentQuery;
//     }
//
//     [HttpGet]
//     public IActionResult GetFilteredProducts(
//         string? gender = null,
//         string? category = null,
//         string? brand = null,
//         string? collection = null)
//     {
//         var rootNodes = _contentQuery.ContentAtRoot();
//         var root = rootNodes.FirstOrDefault();
//
//         if (root == null)
//             return NotFound("Root content not found");
//
//         var allProducts = root.DescendantsOrSelfOfType("productPage");
//
//         var filtered = allProducts
//             .Where(p =>
//                 (string.IsNullOrEmpty(gender) || (p.Value<string>("gender")?.ToLower() == gender.ToLower())) &&
//                 (string.IsNullOrEmpty(category) || (p.Value<string>("category")?.ToLower() == category.ToLower())) &&
//                 (string.IsNullOrEmpty(brand) || (p.Value<string>("brand")?.ToLower() == brand.ToLower())) &&
//                 (string.IsNullOrEmpty(collection) || (p.Value<string>("collection")?.ToLower() == collection.ToLower()))
//             )
//             .Select(p => new
//             {
//                 id = p.Id,
//                 title = p.Name,
//                 imageUrl1 = p.Value<string>("image1") ?? "",
//                 imageUrl2 = p.Value<string>("image2") ?? "",
//                 alt = p.Name,
//                 affLink = p.Value<string>("affLink") ?? "",
//                 price = p.Value<decimal?>("price") ?? 0,
//                 brands = p.Value<string>("brand") ?? "",
//                 color = p.Value<string>("color") ?? "",
//                 category = p.Value<IEnumerable<IPublishedContent>>("categories")?.FirstOrDefault()?.Name ?? "",
//                 // collection = p.AncestorsOrSelf().FirstOrDefault(a => a.ContentType.Alias == "collectionPage")
//                 //     ?.UrlSegment ?? ""
//                 collection = p.AncestorsOrSelf().FirstOrDefault(a => a.ContentType.Alias == "collectionPage")?.Url()?.Trim('/').Split('/').Last() ?? ""
//
//             });
//
//         return Ok(filtered);
//     }
// }

// [Route("/umbraco/delivery/api/products")]
// public class ProductsApiController : UmbracoApiController
// {
//     private readonly IPublishedContentQuery _contentQuery;
//
//     public ProductsApiController(IPublishedContentQuery contentQuery)
//     {
//         _contentQuery = contentQuery;
//     }
//
//     [HttpGet]
//     public IActionResult GetFilteredProducts(
//         string? gen = null,
//         string? category = null,
//         string? brand = null,
//         string? collection = null)
//     {
//         var root = _contentQuery.ContentAtRoot().FirstOrDefault();
//         if (root == null)
//             return NotFound("Root content not found");
//
//         var allProducts = root.DescendantsOrSelfOfType("productPage");
//
//         var filtered = allProducts
//             .Where(p =>
//                 (string.IsNullOrEmpty(gen) || (p.Value<string>("gen")?.ToLower() == gen.ToLower())) &&
//                 (string.IsNullOrEmpty(category) || (p.Value<IEnumerable<IPublishedContent>>("categories")?.Any(c => c.Name.ToLower().Contains(category.ToLower())) == true)) &&
//                 (string.IsNullOrEmpty(brand) || (p.Value<string>("brand")?.ToLower() == brand.ToLower())) &&
//                 (string.IsNullOrEmpty(collection) ||
//                  (p.AncestorsOrSelf()
//                      .Any(a => a.ContentType.Alias == "collectionPage" &&
//                                a.UrlSegment.ToLower() == collection.ToLower()))
//                 )
//             )
//             .Select(p => new
//             {
//                 id = p.Key,
//                 title = p.Name,
//                 imageUrl1 = p.Value<string>("image1") ?? "",
//                 imageUrl2 = p.Value<string>("image2") ?? "",
//                 alt = p.Name,
//                 affLink = p.Value<string>("affLink") ?? "",
//                 price = p.Value<decimal?>("price") ?? 0,
//                 brands = p.Value<string>("brand") ?? "",
//                 color = p.Value<string>("color") ?? "",
//                 material = p.Value<string>("material") ?? "",
//                 category = p.Value<IEnumerable<IPublishedContent>>("categories")?.FirstOrDefault()?.Name ?? "",
//                 collection = p.AncestorsOrSelf()
//                     .FirstOrDefault(a => a.ContentType.Alias == "collectionPage")?
//                     .UrlSegment ?? "",
//                 gen = p.Value<string>("gen") ?? ""
//             });
//
//
//         return Ok(filtered);
//     }
//     
// }
[Route("/umbraco/delivery/api/products")]
public class ProductsApiController : UmbracoApiController
{
    private readonly IPublishedContentQuery _contentQuery;

    public ProductsApiController(IPublishedContentQuery contentQuery)
    {
        _contentQuery = contentQuery;
    }

    [HttpGet]
    public IActionResult GetFilteredProducts(
        string? gen = null,
        string? category = null,
        string? brand = null,
        string? collection = null)
    {
        var root = _contentQuery.ContentAtRoot().FirstOrDefault();
        if (root == null)
            return NotFound("Root content not found");

        var allProducts = root.DescendantsOrSelfOfType("productPage");

        var filtered = allProducts
            .Where(p =>
                (string.IsNullOrEmpty(gen) || Normalize(p.Value<string>("gen")) == Normalize(gen)) &&
                (string.IsNullOrEmpty(category) || p.Value<IEnumerable<IPublishedContent>>("categories")?
                    .Any(c => Normalize(c.Name).Contains(Normalize(category))) == true) &&
                (string.IsNullOrEmpty(brand) || Normalize(p.Value<string>("brand")) == Normalize(brand))  &&
                (string.IsNullOrEmpty(collection) ||
                 p.AncestorsOrSelf()
                     .Any(a => a.ContentType.Alias == "collectionPage" &&
                               Normalize(a.UrlSegment) == Normalize(collection))
                )
            )
            .Select(p => new
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
                material = p.Value<string>("material") ?? "",
                category = p.Value<IEnumerable<IPublishedContent>>("categories")?.FirstOrDefault()?.Name ?? "",
                collection = p.AncestorsOrSelf()
                    .FirstOrDefault(a => a.ContentType.Alias == "collectionPage")?
                    .UrlSegment ?? "",
                gen = p.Value<string>("gen") ?? ""
            });


        return Ok(filtered);
    }
    [HttpGet("slug/{slug}")]
    public IActionResult GetProductBySlug(string slug)
    {
        var root = _contentQuery.ContentAtRoot().FirstOrDefault();
        if (root == null)
            return NotFound("Root content not found");

        var product = root.DescendantsOfType("productPage")
            .FirstOrDefault(p => Normalize(p.UrlSegment) == Normalize(slug));

        if (product == null)
            return NotFound("Product not found");

        var result = new
        {
            id = product.Key,
            title = product.Name,
            imageUrl1 = product.Value<string>("image1") ?? "",
            imageUrl2 = product.Value<string>("image2") ?? "",
            imageUrl3 = product.Value<string>("image3") ?? "",
            alt = product.Name,
            affLink = product.Value<string>("affLink") ?? "",
            price = product.Value<decimal?>("price") ?? 0,
            brands = product.Value<string>("brand") ?? "",
            color = product.Value<string>("color") ?? "",
            material = product.Value<string>("material") ?? "",
            category = product.Value<IEnumerable<IPublishedContent>>("categories")?.FirstOrDefault()?.Name ?? "",
            collection = product.AncestorsOrSelf()
                .FirstOrDefault(a => a.ContentType.Alias == "collectionPage")?
                .UrlSegment ?? "",
            gen = product.Value<string>("gen") ?? ""
        };

        return Ok(result);
    }

    private string Normalize(string? input)
    {
        return input?.ToLowerInvariant()
            .Replace("-", " ")
            .Replace("_", " ")
            .Trim() ?? "";
    }
}
