using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

[Route("api/[controller]")]
public class CategoriesController : UmbracoApiController
{
    private readonly IUmbracoContextFactory _contextFactory;

    public CategoriesController(IUmbracoContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    [HttpGet]
    public IActionResult Get()
    {
        using var cref = _contextFactory.EnsureUmbracoContext();
        var root = cref.UmbracoContext.Content.GetAtRoot().FirstOrDefault();

        if (root == null) return NotFound();

        // Găsește pagina care conține categoriile (presupunem un alias "categories")
        var categoriesPage = root.DescendantsOrSelf("categories").FirstOrDefault();

        if (categoriesPage == null) return NotFound();

        var categories = categoriesPage.Children.Select(x => new {
            title = x.Value<string>("title"),
            imgSrc = x.Value<string>("image"), // URL absolut sau relativ
            count = x.Value<int>("productCount"),
            alt = x.Value<string>("altText") ?? x.Name
        });

        return Ok(categories);
    }
}