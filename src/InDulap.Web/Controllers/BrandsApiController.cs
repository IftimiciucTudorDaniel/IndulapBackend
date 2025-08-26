using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System;
using Umbraco.Cms.Core;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Extensions;

[Route("/umbraco/delivery/api/brands")]
public class BrandsApiController : UmbracoApiController
{
    private readonly IPublishedContentQuery _contentQuery;

    public BrandsApiController(IPublishedContentQuery contentQuery)
    {
        _contentQuery = contentQuery;
    }

    [HttpGet]
    public IActionResult GetBrands([FromQuery] int take = 30)
    {
        if (take <= 0)
            take = 30; // fallback la default

        var root = _contentQuery.ContentAtRoot().FirstOrDefault();
        if (root == null)
            return NotFound("Root content not found");

        var products = root.DescendantsOrSelfOfType("productPage");

        var brandList = products
            .Select(p => p.Value<string>("brand"))
            .Where(b => !string.IsNullOrWhiteSpace(b))
            .Select(Normalize)
            .Distinct()
            .Take(take)
            .ToList();

        int mid = (int)Math.Ceiling(brandList.Count / 2.0);
        var group1 = brandList.Take(mid).ToList();
        var group2 = brandList.Skip(mid).ToList();

        return Ok(new
        {
            group1 = group1.Select(b => new
            {
                name = b,
                link = $"/brand/{b.Replace(" ", "-")}"
            }),
            group2 = group2.Select(b => new
            {
                name = b,
                link = $"/brand/{b.Replace(" ", "-")}"
            })
        });
    }

    private string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "";

        return input.ToLowerInvariant()
            .Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
            .Aggregate("", (s, c) => s + c)
            .Replace("-", " ")
            .Replace("_", " ")
            .Trim();
    }
}
