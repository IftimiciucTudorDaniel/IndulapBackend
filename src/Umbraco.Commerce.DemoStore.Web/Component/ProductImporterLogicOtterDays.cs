using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;
using File = System.IO.File;

/// <summary>
/// Bun pentru Depurtat.ro
/// </summary>
public class ProductImporterLogicOtterDays
{
    private readonly IContentService _contentService;
    private readonly ILogger<ProductImporterLogic> _logger;
    private readonly IMediaService _mediaService;
    private readonly MediaFileManager _mediaFileManager;

    public ProductImporterLogicOtterDays(
        IContentService contentService,
        ILogger<ProductImporterLogic> logger,
        IMediaService mediaService,
        MediaFileManager mediaFileManager)
    {
        _contentService = contentService;
        _logger = logger;
        _mediaService = mediaService;
        _mediaFileManager = mediaFileManager;
    }

    public async Task RunImportAsync()
    {
        try
        {
            var xmlPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "first......xml");
            if (!File.Exists(xmlPath))
            {
                _logger.LogWarning("❌ Fișierul XML nu există la: {0}", xmlPath);
                return;
            }

            var xmlContent = File.ReadAllText(xmlPath);
            xmlContent = XmlUtils.FixInvalidAmpersands(xmlContent);
            var doc = XDocument.Parse(xmlContent);
            var items = doc.Descendants("item").ToList();

            if (!items.Any())
            {
                _logger.LogWarning("❌ Nu s-au găsit produse în fișierul XML.");
                return;
            }

            var root = _contentService.GetRootContent().FirstOrDefault();
            if (root == null)
            {
                _logger.LogWarning("❌ Nu există niciun nod root în Content.");
                return;
            }

            var productsPage = _contentService
                .GetPagedChildren(root.Id, 0, 100, out _)
                .FirstOrDefault(x => x.ContentType.Alias == "productsPage");

            if (productsPage == null)
            {
                _logger.LogWarning("❌ Nodul 'Products Page' nu a fost găsit.");
                return;
            }

            var categoriesPage = _contentService
                .GetPagedChildren(root.Id, 0, int.MaxValue, out _)
                .FirstOrDefault(x => x.ContentType.Alias == "categoriesPage");

            if (categoriesPage == null)
            {
                _logger.LogWarning("❌ Nu a fost găsită pagina 'categoriesPage' în ierarhia de conținut.");
                return;
            }

            var colorMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "negru", "negru" },
                    { "negri", "negru" },
                    { "neagra", "negru" },
                    { "negre", "negru" },

                    { "alb", "alb" },
                    { "albi", "alb" },
                    { "alba", "alb" },
                    { "albe", "alb" },

                    { "rosu", "rosu" },
                    { "rosii", "rosu" },

                    { "verde", "verde" },
                    { "verzi", "verde" },

                    { "bej", "bej" },
                    { "bleumarin", "bleumarin" },

                    { "portocaliu", "portocaliu" },
                    { "portocalii", "portocaliu" },

                    { "galben", "galben" },
                    { "galbeni", "galben" },
                    { "galbene", "galben" },

                    { "visiniu", "visiniu" },
                    { "visinii", "visiniu" },

                    { "gri", "gri" },
                    { "roz", "roz" },
                    { "mov", "mov" },
                    { "albastru", "albastru" },
                    { "albastri", "albastru" },
                    { "albastre", "albastru" },
                    { "albastra", "albastru" },

                    { "turcoaz", "turcoaz" },
                    { "auriu", "auriu" },
                    { "aurii", "auriu" },
                    { "argintiu", "argintiu" },
                    { "argintii", "argintiu" },

                    { "multicolor", "multicolor" },
                    { "kaki", "kaki" },
                    { "nude", "nude" },
                    { "bronz", "bronz" },
                    { "coral", "coral" },
                    { "indigo", "indigo" },
                    { "lila", "lila" },
                    { "fucsia", "fucsia" },
                    { "lavanda", "lavanda" },
                    { "menta", "menta" },
                    { "crem", "crem" },
                    { "camel", "camel" },
                    { "caramiziu", "caramiziu" },
                    { "grena", "grena" },
                    { "ciocolatiu", "ciocolatiu" },
                    { "sampanie", "sampanie" },
                    { "petrol", "petrol" },
                    { "burgundy", "burgundy" },
                    { "maro,", "maro"},
                    { "maro", "maro"},
                    { "alb-negru", "alb-negru" },
                    { "alb-negri", "alb-negru" }
                };

            foreach (var item in items)
            {
                string widgetName = item.Element("widget_name")?.Value?.Trim();

                if (string.IsNullOrWhiteSpace(widgetName))
                {
                    _logger.LogWarning("❌ widget_name lipsă. Se sare peste produs.");
                    continue;
                }

                var collectionPage = _contentService
                    .GetPagedChildren(productsPage.Id, 0, int.MaxValue, out _)
                    .FirstOrDefault(x => x.ContentType.Alias == "collectionPage" && x.Name.Equals(widgetName, StringComparison.OrdinalIgnoreCase));

                if (collectionPage == null)
                {
                    collectionPage = _contentService.Create(widgetName, productsPage.Id, "collectionPage");
                    _contentService.SaveAndPublish(collectionPage);
                    _logger.LogInformation("collectionPage creat: {0}", widgetName);
                }

                string originalTitle = item.Element("title")?.Value?.Trim();

                if (string.IsNullOrWhiteSpace(originalTitle))
                {
                    _logger.LogWarning("Produsul are titlu gol. Se sare peste.");
                    continue;
                }
                

                var words = originalTitle.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                int uppercaseIndex = Array.FindIndex(words, w => w.All(char.IsUpper));
                int lastUppercaseIndex = Array.FindLastIndex(words, w => w.All(char.IsUpper));

                string detectedColor = words
                    .Select(w => w.Trim(',', '.', ';').ToLowerInvariant())
                    .FirstOrDefault(w => colorMap.ContainsKey(w));

                string color = detectedColor != null ? colorMap[detectedColor] : "necunoscut";

                if (color == "necunoscut")
                {
                    _logger.LogWarning("❓ Culoare necunoscută pentru titlul: {0}", originalTitle);
                }
                
                
                string name = string.Join(' ', words.Take(uppercaseIndex + 1));
                
                if (uppercaseIndex == -1)
                {
                    name = string.Join(' ', words.Take(3));
                }
                else
                {
                    name = string.Join(' ', words.Take(uppercaseIndex + 1));
                }
                string material = null;
                var titleAfterDin = originalTitle.Split(new[] { "din" }, StringSplitOptions.None).Skip(1).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(titleAfterDin))
                {
                    material = titleAfterDin.Split(',').FirstOrDefault()?.Trim();
                }
                
                bool exists = _contentService
                    .GetPagedChildren(collectionPage.Id, 0, int.MaxValue, out _)
                    .Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                if (exists)
                    continue;

                var product = _contentService.Create(name, collectionPage.Id, "productPage");

                string gen = item.Element("category")?.Value ?? "Gen necunoscut";
                product.SetValue("gen", gen);

                string firstWord = name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                string subCategoryName = !string.IsNullOrWhiteSpace(firstWord)
                    ? $"{firstWord}-{gen}"
                    : $"Categorie necunoscută - {gen}";
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(color) || string.IsNullOrWhiteSpace(subCategoryName))
                {
                    _logger.LogWarning("❌ Titlu invalid. name: {0}, color: {1}, subCategory: {2}", name, color, subCategoryName);
                    continue;
                }
                product.SetValue("color", color);
                product.SetValue("material", material ?? "necunoscut");
                product.SetValue("brand", item.Element("brand")?.Value);
                product.SetValue("sku", item.Element("product_id")?.Value);
                product.SetValue("Price", decimal.TryParse(item.Element("price")?.Value, out var price) ? price : 0);
                product.SetValue("shortDescription", item.Element("subcategory")?.Value);
                product.SetValue("longDescription", item.Element("category")?.Value);
                product.SetValue("gen", item.Element("category")?.Value);

                string affLink = item.Element("aff_code")?.Value?.Trim();
                if (!string.IsNullOrWhiteSpace(affLink))
                {
                    product.SetValue("affLink", affLink);
                }
                var imageUrlsText = item.Element("image_urls")?.Value;
                subCategoryName = subCategoryName.Trim();

                var categoryPage = _contentService
                    .GetPagedChildren(categoriesPage.Id, 0, int.MaxValue, out _)
                    .FirstOrDefault(x => x.ContentType.Alias == "categoryPage" && 
                                         x.Name.Equals(subCategoryName, StringComparison.OrdinalIgnoreCase));
                if (categoryPage == null)
                {
                    categoryPage = _contentService.Create(subCategoryName, categoriesPage.Id, "categoryPage");
                    _logger.LogInformation("✅ categoryPage creat sub 'categoriesPage': {0}", subCategoryName);
                    if (!string.IsNullOrWhiteSpace(imageUrlsText))
                    {
                        var urls = imageUrlsText.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(u => u.Trim())
                            .ToList();
                        if (urls.Count > 0)
                        {
                            categoryPage.SetValue("image1", urls[0]);
                        }
                    }
                    _contentService.SaveAndPublish(categoryPage);
                }
                else
                {
                    if (categoryPage.ParentId != categoriesPage.Id)
                    {
                        _logger.LogWarning("⚠️ Nodul '{0}' a fost găsit dar nu e copilul lui 'categoriesPage'. Se omite.", subCategoryName);
                        continue;
                    }
                }
                
                var categoryUdi = Udi.Create(Constants.UdiEntityType.Document, categoryPage.Key).ToString();
                product.SetValue("categories", categoryUdi);

                
                if (!string.IsNullOrWhiteSpace(imageUrlsText))
                {
                    var urls = imageUrlsText.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(u => u.Trim()).ToList();
                    if (urls.Count > 0)
                        product.SetValue("image1", urls[0]);
                    if (urls.Count > 1)
                        product.SetValue("image2", urls[1]);
                    if (urls.Count > 2)
                        product.SetValue("image3", urls[2]);
                }

                _contentService.SaveAndPublish(product);
                _logger.LogInformation("✅ Produs adăugat: {0}", name);
            }

            _logger.LogInformation("✅ Import produse complet.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Eroare la importul produselor.");
        }
    }
}
