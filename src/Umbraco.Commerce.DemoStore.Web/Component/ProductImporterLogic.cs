//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Net.Http;
//using System.Threading.Tasks;
//using System.Xml.Linq;
//using Umbraco.Cms.Core;
//using Umbraco.Cms.Core.IO;
//using Umbraco.Cms.Core.Models;
//using Umbraco.Cms.Core.Services;
//using Umbraco.Extensions;
//using System.Diagnostics;
///// <summary>
///// Bun pentru Depurtat.ro
///// </summary>

//public class ProductImporterLogic
//{
//    private readonly IContentService _contentService;
//    private readonly ILogger<ProductImporterLogic> _logger;
//    private readonly IMediaService _mediaService;
//    private readonly MediaFileManager _mediaFileManager;

//    public ProductImporterLogic(
//        IContentService contentService,
//        ILogger<ProductImporterLogic> logger,
//        IMediaService mediaService,
//        MediaFileManager mediaFileManager)
//    {
//        _contentService = contentService;
//        _logger = logger;
//        _mediaService = mediaService;
//        _mediaFileManager = mediaFileManager;
//    }

//    public async Task RunImportAsync()
//    {
//        var stopwatch = Stopwatch.StartNew();
//        try
//        {
//            var xmlPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "Depurtatstopwatch.....xml");
//            if (!System.IO.File.Exists(xmlPath))
//            {
//                _logger.LogWarning("❌ Fișierul XML nu există la: {0}", xmlPath);
//                return;
//            }

//            var xmlContent = System.IO.File.ReadAllText(xmlPath);
//            xmlContent = XmlUtils.FixInvalidAmpersands(xmlContent);
//            var doc = XDocument.Parse(xmlContent);
//            var items = doc.Descendants("item").ToList();

//            if (!items.Any())
//            {
//                _logger.LogWarning("❌ Nu s-au găsit produse în fișierul XML.");
//                return;
//            }

//            var root = _contentService.GetRootContent().FirstOrDefault();
//            if (root == null)
//            {
//                _logger.LogWarning("❌ Nu există niciun nod root în Content.");
//                return;
//            }

//            var productsPage = _contentService
//                .GetPagedChildren(root.Id, 0, 100, out _)
//                .FirstOrDefault(x => x.ContentType.Alias == "productsPage");

//            if (productsPage == null)
//            {
//                _logger.LogWarning("❌ Nodul 'Products Page' nu a fost găsit.");
//                return;
//            }

//            var categoriesPage = _contentService
//                .GetPagedChildren(root.Id, 0, int.MaxValue, out _)
//                .FirstOrDefault(x => x.ContentType.Alias == "categoriesPage");

//            if (categoriesPage == null)
//            {
//                _logger.LogWarning("❌ Nu a fost găsită pagina 'categoriesPage' în ierarhia de conținut.");
//                return;
//            }

//            foreach (var item in items)
//            {
//                var colorMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
//                {
//                    { "negru", "negru" },
//                    { "negri", "negru" },
//                    { "neagra", "negru" },
//                    { "negre", "negru" },

//                    { "alb", "alb" },
//                    { "albi", "alb" },
//                    { "alba", "alb" },
//                    { "albe", "alb" },

//                    { "rosu", "rosu" },
//                    { "rosii", "rosu" },

//                    { "verde", "verde" },
//                    { "verzi", "verde" },

//                    { "bej", "bej" },
//                    { "bleumarin", "bleumarin" },

//                    { "portocaliu", "portocaliu" },
//                    { "portocalii", "portocaliu" },

//                    { "galben", "galben" },
//                    { "galbeni", "galben" },
//                    { "galbene", "galben" },

//                    { "visiniu", "visiniu" },
//                    { "visinii", "visiniu" },

//                    { "gri", "gri" },
//                    { "roz", "roz" },
//                    { "mov", "mov" },
//                    { "albastru", "albastru" },
//                    { "albastri", "albastru" },
//                    { "albastre", "albastru" },
//                    { "albastra", "albastru" },

//                    { "turcoaz", "turcoaz" },
//                    { "auriu", "auriu" },
//                    { "aurii", "auriu" },
//                    { "argintiu", "argintiu" },
//                    { "argintii", "argintiu" },

//                    { "multicolor", "multicolor" },
//                    { "kaki", "kaki" },
//                    { "nude", "nude" },
//                    { "bronz", "bronz" },
//                    { "coral", "coral" },
//                    { "indigo", "indigo" },
//                    { "lila", "lila" },
//                    { "fucsia", "fucsia" },
//                    { "lavanda", "lavanda" },
//                    { "menta", "menta" },
//                    { "crem", "crem" },
//                    { "camel", "camel" },
//                    { "caramiziu", "caramiziu" },
//                    { "grena", "grena" },
//                    { "ciocolatiu", "ciocolatiu" },
//                    { "sampanie", "sampanie" },
//                    { "petrol", "petrol" },
//                    { "burgundy", "burgundy" },
//                    { "maro,", "maro"},
//                    { "maro", "maro"},
//                    { "alb-negru", "alb-negru" },
//                    { "alb-negri", "alb-negru" }
//                };


//                string originalTitle = item.Element("title")?.Value?.Trim();

//                if (string.IsNullOrWhiteSpace(originalTitle))
//                {
//                    _logger.LogWarning("❌ Produsul are titlu gol. Se sare peste.");
//                    continue;
//                }
//                string campaignRaw = item.Element("campaign_name")?.Value?.Trim();
//                string collectionName = !string.IsNullOrWhiteSpace(campaignRaw)
//                    ? campaignRaw.Split('.')[0]
//                    : "Colecție necunoscută";

//                var collectionPage = _contentService
//                    .GetPagedChildren(productsPage.Id, 0, int.MaxValue, out _)
//                    .FirstOrDefault(x => x.ContentType.Alias == "collectionPage" && x.Name.Equals(collectionName, StringComparison.OrdinalIgnoreCase));

//                if (collectionPage == null)
//                {
//                    collectionPage = _contentService.Create(collectionName, productsPage.Id, "collectionPage");
//                    _contentService.SaveAndPublish(collectionPage);
//                    _logger.LogInformation("✅ CollectionPage creat: {0}", collectionName);
//                }


//                if (string.IsNullOrWhiteSpace(originalTitle))
//                {
//                    _logger.LogWarning("❌ Produsul are titlu gol. Se sare peste.");
//                    continue;
//                }

//                var words = originalTitle.Split(' ', StringSplitOptions.RemoveEmptyEntries);
//                if (words.Length < 2)
//                {
//                    _logger.LogWarning("❌ Titlu invalid pentru extragerea culorii: {0}", originalTitle);
//                    continue;
//                }
//                string detectedColor = words
//                    .Select(w => w.Trim(',', '.', ';').ToLowerInvariant())
//                    .FirstOrDefault(w => colorMap.ContainsKey(w));

//                string color = detectedColor != null ? colorMap[detectedColor] : "necunoscut";
//                if (color == null)
//                {
//                    _logger.LogWarning("Nu s-a găsit culoare validă în titlu: {0}", originalTitle);
//                    color = "necunoscut";
//                }
//                string name = string.Join(' ', words.Take(words.Length - 1));

//                if (string.IsNullOrWhiteSpace(name))
//                {
//                    _logger.LogWarning("❌ Nume gol după extragerea culorii. Se sare peste.");
//                    continue;
//                }

//                string sku = item.Element("product_id")?.Value?.Trim();

//                if (string.IsNullOrWhiteSpace(sku))
//                {
//                    _logger.LogWarning("❌ Produsul nu are SKU. Se sare peste.");
//                    continue;
//                }

//                var allCollections = _contentService
//                    .GetPagedChildren(productsPage.Id, 0, int.MaxValue, out _)
//                    .Where(x => x.ContentType.Alias == "collectionPage")
//                    .SelectMany(c => _contentService.GetPagedChildren(c.Id, 0, int.MaxValue, out _));

//                bool exists = allCollections
//                    .Any(x => x.GetValue<string>("sku")?.Equals(sku, StringComparison.OrdinalIgnoreCase) == true);

//                if (exists)
//                {
//                    _logger.LogInformation("⚠️ Produsul cu SKU {0} există deja. Se omite.", sku);
//                    continue;
//                }


//                var product = _contentService.Create(name, collectionPage.Id, "productPage");

//                product.SetValue("color", color);

//                product.SetValue("brand", item.Element("brand")?.Value.ToLower());
//                product.SetValue("sku", item.Element("product_id")?.Value);
//                product.SetValue("Price", decimal.TryParse(item.Element("price")?.Value, out var price) ? price : 0);
//                product.SetValue("shortDescription", item.Element("subcategory")?.Value);
//                product.SetValue("longDescription", item.Element("category")?.Value);
//                product.SetValue("gen", "Femei");

//                string affLink = item.Element("aff_code")?.Value?.Trim();
//                if (!string.IsNullOrWhiteSpace(affLink))
//                {
//                    product.SetValue("affLink", affLink);
//                }

//                var categoryRaw = item.Element("title")?.Value?.Trim();
//                var gen = "Femei";
//                product.SetValue("gen", gen);
//                var firstWord = !string.IsNullOrWhiteSpace(categoryRaw)
//                    ? categoryRaw.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
//                    : null;
//                var categoryName = !string.IsNullOrWhiteSpace(firstWord)
//                    ? $"{firstWord}-{gen}"
//                    : null;
//                var imageUrlsText = item.Element("image_urls")?.Value;
//                if (!string.IsNullOrWhiteSpace(categoryName))
//                {
//                    var categoryPage = _contentService
//                        .GetPagedChildren(categoriesPage.Id, 0, int.MaxValue, out _)
//                        .FirstOrDefault(x => x.ContentType.Alias == "categoryPage" && x.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
//                    if (categoryPage == null)
//                    {
//                        categoryPage = _contentService.Create(categoryName, categoriesPage.Id, "categoryPage");
//                        if (!string.IsNullOrWhiteSpace(imageUrlsText))
//                        {
//                            var urls = imageUrlsText.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(u => u.Trim())
//                                .ToList();
//                            if (urls.Count > 0)
//                            {
//                                categoryPage.SetValue("image1", urls[0]);
//                            }
//                        }
//                        _contentService.SaveAndPublish(categoryPage);
//                        _logger.LogInformation("✅ Categorie creată: {0}", categoryName);
//                    }

//                    var categoryUdi = Udi.Create(Constants.UdiEntityType.Document, categoryPage.Key).ToString();
//                    product.SetValue("categories", categoryUdi);
//                }

//                if (!string.IsNullOrWhiteSpace(imageUrlsText))
//                {
//                    var urls = imageUrlsText.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(u => u.Trim()).ToList();
//                    if (urls.Count > 0)
//                        product.SetValue("image1", urls[0]);
//                    if (urls.Count > 1)
//                        product.SetValue("image2", urls[1]);
//                    if (urls.Count > 2)
//                        product.SetValue("image3", urls[2]);
//                }

//                _contentService.SaveAndPublish(product);
//                _logger.LogInformation("✅ Produs adăugat: {0}", name);
//            }

//            _logger.LogInformation("✅ Import produse complet.");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Eroare la importul produselor.");
//        }
//        finally
//        {
//            stopwatch.Stop();
//            _logger.LogInformation("⏱️ Import finalizat în {0} secunde", stopwatch.Elapsed.TotalSeconds.ToString("0.00"));
//        }
//    }
//}
//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Net.Http;
//using System.Threading.Tasks;
//using System.Xml.Linq;
//using Umbraco.Cms.Core;
//using Umbraco.Cms.Core.IO;
//using Umbraco.Cms.Core.Models;
//using Umbraco.Cms.Core.Services;
//using Umbraco.Extensions;
//using System.Diagnostics;
///// <summary>
///// Bun pentru Depurtat.ro
///// </summary>

//public class ProductImporterLogic
//{
//    private readonly IContentService _contentService;
//    private readonly ILogger<ProductImporterLogic> _logger;
//    private readonly IMediaService _mediaService;
//    private readonly MediaFileManager _mediaFileManager;

//    public ProductImporterLogic(
//        IContentService contentService,
//        ILogger<ProductImporterLogic> logger,
//        IMediaService mediaService,
//        MediaFileManager mediaFileManager)
//    {
//        _contentService = contentService;
//        _logger = logger;
//        _mediaService = mediaService;
//        _mediaFileManager = mediaFileManager;
//    }

//    public async Task RunImportAsync()
//    {
//        var stopwatch = Stopwatch.StartNew();
//        try
//        {
//            var xmlPath = Path.Combine(Directory.GetCurrentDirectory(), "Component", "Depurtat", "preprocessed-depurtat.xml");
//            if (!System.IO.File.Exists(xmlPath))
//            {
//                _logger.LogWarning("❌ Fișierul XML nu există la: {0}", xmlPath);
//                return;
//            }

//            var xmlContent = System.IO.File.ReadAllText(xmlPath);
//            var doc = XDocument.Parse(xmlContent);
//            var items = doc.Descendants("item").ToList();

//            if (!items.Any())
//            {
//                _logger.LogWarning("❌ Nu s-au găsit produse în fișierul XML.");
//                return;
//            }

//            var root = _contentService.GetRootContent().FirstOrDefault();
//            if (root == null)
//            {
//                _logger.LogWarning("❌ Nu există niciun nod root în Content.");
//                return;
//            }

//            var productsPage = _contentService
//                .GetPagedChildren(root.Id, 0, 100, out _)
//                .FirstOrDefault(x => x.ContentType.Alias == "productsPage");

//            if (productsPage == null)
//            {
//                _logger.LogWarning("❌ Nodul 'Products Page' nu a fost găsit.");
//                return;
//            }

//            var categoriesPage = _contentService
//                .GetPagedChildren(root.Id, 0, int.MaxValue, out _)
//                .FirstOrDefault(x => x.ContentType.Alias == "categoriesPage");

//            if (categoriesPage == null)
//            {
//                _logger.LogWarning("❌ Nu a fost găsită pagina 'categoriesPage' în ierarhia de conținut.");
//                return;
//            }

//            foreach (var item in items)
//            {
//                string title = item.Element("title")?.Value?.Trim();
//                string brand = item.Element("brand")?.Value?.Trim();
//                string color = item.Element("color")?.Value?.Trim();
//                string gen = item.Element("gen")?.Value?.Trim();
//                string category = item.Element("category")?.Value?.Trim();
//                string affCode = item.Element("aff_code")?.Value?.Trim();
//                string priceText = item.Element("price")?.Value?.Trim();
//                string description = item.Element("description")?.Value?.Trim();

//                if (string.IsNullOrWhiteSpace(title))
//                {
//                    _logger.LogWarning("❌ Produsul are titlu gol. Se sare peste.");
//                    continue;
//                }

//                // Generare SKU din titlu (sau poți folosi alt câmp dacă ai)
//                string sku = title.Replace(" ", "_").ToLowerInvariant();

//                // Verificare dacă produsul există deja
//                var allCollections = _contentService
//                    .GetPagedChildren(productsPage.Id, 0, int.MaxValue, out _)
//                    .Where(x => x.ContentType.Alias == "collectionPage")
//                    .SelectMany(c => _contentService.GetPagedChildren(c.Id, 0, int.MaxValue, out _));

//                bool exists = allCollections
//                    .Any(x => x.GetValue<string>("sku")?.Equals(sku, StringComparison.OrdinalIgnoreCase) == true);

//                if (exists)
//                {
//                    _logger.LogInformation("⚠️ Produsul cu SKU {0} există deja. Se omite.", sku);
//                    continue;
//                }

//                // Creare/găsire collection page pe baza brand-ului
//                string collectionName = !string.IsNullOrWhiteSpace("Depurtat") ? "Depurtat" : "Colecție necunoscută";

//                var collectionPage = _contentService
//                    .GetPagedChildren(productsPage.Id, 0, int.MaxValue, out _)
//                    .FirstOrDefault(x => x.ContentType.Alias == "collectionPage" && x.Name.Equals(collectionName, StringComparison.OrdinalIgnoreCase));

//                if (collectionPage == null)
//                {
//                    collectionPage = _contentService.Create(collectionName, productsPage.Id, "collectionPage");
//                    _contentService.SaveAndPublish(collectionPage);
//                    _logger.LogInformation("✅ CollectionPage creat: {0}", collectionName);
//                }

//                // Creare/găsire category page
//                IContent categoryPage = null;
//                if (!string.IsNullOrWhiteSpace(category))
//                {
//                    categoryPage = _contentService
//                        .GetPagedChildren(categoriesPage.Id, 0, int.MaxValue, out _)
//                        .FirstOrDefault(x => x.ContentType.Alias == "categoryPage" && x.Name.Equals(category, StringComparison.OrdinalIgnoreCase));

//                    if (categoryPage == null)
//                    {
//                        categoryPage = _contentService.Create(category, categoriesPage.Id, "categoryPage");

//                        // Setează prima imagine ca imagine pentru categorie
//                        string image1 = item.Element("image1")?.Value?.Trim();
//                        if (!string.IsNullOrWhiteSpace(image1))
//                        {
//                            categoryPage.SetValue("image1", image1);
//                        }

//                        _contentService.SaveAndPublish(categoryPage);
//                        _logger.LogInformation("✅ Categorie creată: {0}", category);
//                    }
//                }

//                // Creare produs
//                var product = _contentService.Create(title, collectionPage.Id, "productPage");

//                // Setare proprietăți
//                product.SetValue("color", color ?? "necunoscut");
//                product.SetValue("brand", brand?.ToLower());
//                product.SetValue("sku", sku);

//                if (decimal.TryParse(priceText, out var price))
//                {
//                    product.SetValue("Price", price);
//                }

//                product.SetValue("gen", gen ?? "Femei");
//                product.SetValue("longDescription", description);

//                if (!string.IsNullOrWhiteSpace(affCode))
//                {
//                    product.SetValue("affLink", affCode);
//                }

//                // Setare categorie
//                if (categoryPage != null)
//                {
//                    var categoryUdi = Udi.Create(Constants.UdiEntityType.Document, categoryPage.Key).ToString();
//                    product.SetValue("categories", categoryUdi);
//                }

//                // Setare imagini
//                string image1Url = item.Element("image1")?.Value?.Trim();
//                string image2Url = item.Element("image2")?.Value?.Trim();
//                string image3Url = item.Element("image3")?.Value?.Trim();

//                if (!string.IsNullOrWhiteSpace(image1Url))
//                    product.SetValue("image1", image1Url);
//                if (!string.IsNullOrWhiteSpace(image2Url))
//                    product.SetValue("image2", image2Url);
//                if (!string.IsNullOrWhiteSpace(image3Url))
//                    product.SetValue("image3", image3Url);

//                _contentService.SaveAndPublish(product);
//                _logger.LogInformation("✅ Produs adăugat: {0}", title);
//            }

//            _logger.LogInformation("✅ Import produse complet.");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Eroare la importul produselor.");
//        }
//        finally
//        {
//            stopwatch.Stop();
//            _logger.LogInformation("⏱️ Import finalizat în {0} secunde", stopwatch.Elapsed.TotalSeconds.ToString("0.00"));
//        }
//    }
//}

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;
using System.Diagnostics;

/// <summary>
/// Multi-XML Product Importer cu sincronizare completă
/// </summary>
public class ProductImporterLogic
{
    private readonly IContentService _contentService;
    private readonly ILogger<ProductImporterLogic> _logger;
    private readonly IMediaService _mediaService;

    // Configurează aici path-urile către XML-uri
    private readonly string[] _xmlPaths = {
        "wwwroot/data/preprocessed-depurtat.xml",
        "wwwroot/data/preprocessed-fashiondays.xml",
        "wwwroot/data/otter-days-preprocessed.xml",
        "wwwroot/data/preprocessed-sosetaria.xml"
    };

    public ProductImporterLogic(
        IContentService contentService,
        ILogger<ProductImporterLogic> logger,
        IMediaService mediaService)
    {
        _contentService = contentService;
        _logger = logger;
        _mediaService = mediaService;
    }

    public async Task RunImportAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("🚀 Începe sincronizarea produselor din {0} XML-uri", _xmlPaths.Length);

            // 1. Colectează toate produsele din XML-uri
            var allXmlProducts = await CollectAllXmlProductsAsync();
            if (!allXmlProducts.Any())
            {
                _logger.LogWarning("❌ Nu s-au găsit produse în niciun XML.");
                return;
            }

            _logger.LogInformation("📊 Total produse găsite în XML-uri: {0}", allXmlProducts.Count);

            // 2. Obține structura Umbraco
            var (productsPage, categoriesPage) = GetUmbracoStructure();
            if (productsPage == null || categoriesPage == null) return;

            // 3. Obține toate produsele existente din Umbraco
            var existingProducts = GetAllExistingProducts(productsPage);
            _logger.LogInformation("📊 Produse existente în Umbraco: {0}", existingProducts.Count);

            // 4. Sincronizează produsele
            await SyncProductsAsync(allXmlProducts, existingProducts, productsPage, categoriesPage);

            _logger.LogInformation("✅ Sincronizare completă finalizată.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Eroare la sincronizarea produselor.");
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation("⏱️ Sincronizare finalizată în {0} secunde", stopwatch.Elapsed.TotalSeconds.ToString("0.00"));
        }
    }

    private async Task<List<XmlProductData>> CollectAllXmlProductsAsync()
    {
        var allProducts = new List<XmlProductData>();

        foreach (var xmlPath in _xmlPaths)
        {
            try
            {
                if (!System.IO.File.Exists(xmlPath))
                {
                    _logger.LogWarning("⚠️ Fișierul XML nu există: {0}", xmlPath);
                    continue;
                }

                var xmlContent = await System.IO.File.ReadAllTextAsync(xmlPath);
                var doc = XDocument.Parse(xmlContent);
                var items = doc.Descendants("item").ToList();

                _logger.LogInformation("📂 XML: {0} - Produse găsite: {1}", Path.GetFileName(xmlPath), items.Count);

                foreach (var item in items)
                {
                    var productData = ParseXmlItem(item, xmlPath);
                    if (productData != null)
                    {
                        allProducts.Add(productData);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Eroare la procesarea XML-ului: {0}", xmlPath);
            }
        }

        return allProducts;
    }

    private XmlProductData ParseXmlItem(XElement item, string sourceXml)
    {
        try
        {
            string sku = item.Element("sku")?.Value?.Trim();
            string title = item.Element("title")?.Value?.Trim();

            if (string.IsNullOrWhiteSpace(sku) || string.IsNullOrWhiteSpace(title))
            {
                _logger.LogWarning("⚠️ Produs invalid (SKU sau titlu lipsă) în {0}", Path.GetFileName(sourceXml));
                return null;
            }

            return new XmlProductData
            {
                Sku = sku,
                Collection = item.Element("collection")?.Value?.Trim() ?? "Colecție necunoscută",
                Title = title,
                Price = decimal.TryParse(item.Element("price")?.Value?.Trim(), out var price) ? price : 0,
                Brand = item.Element("brand")?.Value?.Trim(),
                Color = item.Element("color")?.Value?.Trim() ?? "necunoscut",
                Gen = item.Element("gen")?.Value?.Trim()
                ?? item.Element("gender")?.Value?.Trim()
                ?? "Femei",
                Category = item.Element("category")?.Value?.Trim(),
                AffCode = item.Element("aff_code")?.Value?.Trim(),
                Image1 = item.Element("image1")?.Value?.Trim(),
                Image2 = item.Element("image2")?.Value?.Trim(),
                Image3 = item.Element("image3")?.Value?.Trim(),
                Description = item.Element("description")?.Value?.Trim(),
                SourceXml = sourceXml
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Eroare la parsarea produsului din {0}", Path.GetFileName(sourceXml));
            return null;
        }
    }

    private (IContent productsPage, IContent categoriesPage) GetUmbracoStructure()
    {
        var root = _contentService.GetRootContent().FirstOrDefault();
        if (root == null)
        {
            _logger.LogError("❌ Nu există niciun nod root în Content.");
            return (null, null);
        }

        var productsPage = _contentService
            .GetPagedChildren(root.Id, 0, 100, out _)
            .FirstOrDefault(x => x.ContentType.Alias == "productsPage");

        var categoriesPage = _contentService
            .GetPagedChildren(root.Id, 0, int.MaxValue, out _)
            .FirstOrDefault(x => x.ContentType.Alias == "categoriesPage");

        if (productsPage == null)
        {
            _logger.LogError("❌ Nodul 'productsPage' nu a fost găsit.");
        }

        if (categoriesPage == null)
        {
            _logger.LogError("❌ Nodul 'categoriesPage' nu a fost găsit.");
        }

        return (productsPage, categoriesPage);
    }

    private Dictionary<string, IContent> GetAllExistingProducts(IContent productsPage)
    {
        var existingProducts = new Dictionary<string, IContent>(StringComparer.OrdinalIgnoreCase);

        var allCollections = _contentService
            .GetPagedChildren(productsPage.Id, 0, int.MaxValue, out _)
            .Where(x => x.ContentType.Alias == "collectionPage");

        foreach (var collection in allCollections)
        {
            var products = _contentService
                .GetPagedChildren(collection.Id, 0, int.MaxValue, out _)
                .Where(x => x.ContentType.Alias == "productPage");

            foreach (var product in products)
            {
                var sku = product.GetValue<string>("sku");
                if (!string.IsNullOrWhiteSpace(sku))
                {
                    existingProducts[sku] = product;
                }
            }
        }

        return existingProducts;
    }

    private async Task SyncProductsAsync(
        List<XmlProductData> xmlProducts,
        Dictionary<string, IContent> existingProducts,
        IContent productsPage,
        IContent categoriesPage)
    {
        var xmlSkus = xmlProducts.Select(p => p.Sku).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingSkus = existingProducts.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Statistici
        int updatedCount = 0, addedCount = 0, deletedCount = 0;

        // 1. Verifică și actualizează produsele existente
        foreach (var xmlProduct in xmlProducts)
        {
            if (existingProducts.TryGetValue(xmlProduct.Sku, out var existingProduct))
            {
                if (await UpdateProductIfNeededAsync(existingProduct, xmlProduct, categoriesPage))
                {
                    updatedCount++;
                }
            }
            else
            {
                // 2. Adaugă produse noi
                if (await CreateNewProductAsync(xmlProduct, productsPage, categoriesPage))
                {
                    addedCount++;
                }
            }
        }

        // 3. Șterge produsele care nu mai există în XML-uri
        var skusToDelete = existingSkus.Except(xmlSkus, StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var skuToDelete in skusToDelete)
        {
            if (existingProducts.TryGetValue(skuToDelete, out var productToDelete))
            {
                _contentService.Delete(productToDelete);
                _logger.LogInformation("🗑️ Produs șters: {0} (SKU: {1})", productToDelete.Name, skuToDelete);
                deletedCount++;
            }
        }

        _logger.LogInformation("📊 Sincronizare finalizată - Adăugate: {0}, Actualizate: {1}, Șterse: {2}",
            addedCount, updatedCount, deletedCount);
    }

    private async Task<bool> UpdateProductIfNeededAsync(IContent existingProduct, XmlProductData xmlProduct, IContent categoriesPage)
    {
        // SUPER EFICIENT: Verifică primul și cel mai important câmp - prețul
        var currentPrice = existingProduct.GetValue<decimal>("Price");

        // Dacă prețul este identic, sare peste toate verificările
        if (currentPrice == xmlProduct.Price)
        {
            // Skip complet - produsul este la zi
            return false;
        }

        // Dacă ajunge aici, prețul s-a schimbat - fă update complet
        bool needsUpdate = true;

        existingProduct.SetValue("Price", xmlProduct.Price);
        _logger.LogInformation("💰 Preț actualizat pentru {0}: {1} → {2}",
            xmlProduct.Sku, currentPrice, xmlProduct.Price);

        // Actualizează și alte câmpuri (doar dacă facem update oricum)
        existingProduct.SetValue("color", xmlProduct.Color);
        existingProduct.SetValue("longDescription", xmlProduct.Description);

        // Verifică categoria
        if (!string.IsNullOrWhiteSpace(xmlProduct.Category))
        {
            var categoryPage = await GetOrCreateCategoryAsync(xmlProduct.Category, categoriesPage, xmlProduct.Image1);
            var categoryUdi = Udi.Create(Constants.UdiEntityType.Document, categoryPage.Key).ToString();
            existingProduct.SetValue("categories", categoryUdi);
        }

        // Actualizează imaginile
        if (!string.IsNullOrWhiteSpace(xmlProduct.Image1))
            existingProduct.SetValue("image1", xmlProduct.Image1);
        if (!string.IsNullOrWhiteSpace(xmlProduct.Image2))
            existingProduct.SetValue("image2", xmlProduct.Image2);
        if (!string.IsNullOrWhiteSpace(xmlProduct.Image3))
            existingProduct.SetValue("image3", xmlProduct.Image3);

        _contentService.SaveAndPublish(existingProduct);
        return true;
    }

    private async Task<bool> CreateNewProductAsync(XmlProductData xmlProduct, IContent productsPage, IContent categoriesPage)
    {
        try
        {
            // Obține/creează collection page
            var collectionPage = await GetOrCreateCollectionAsync(xmlProduct.Collection, productsPage);

            // Creează produsul
            var product = _contentService.Create(xmlProduct.Title, collectionPage.Id, "productPage");

            // Setează proprietățile
            product.SetValue("sku", xmlProduct.Sku);
            product.SetValue("color", xmlProduct.Color);
            product.SetValue("brand", xmlProduct.Brand?.ToLower());
            product.SetValue("Price", xmlProduct.Price);
            product.SetValue("gen", xmlProduct.Gen);
            product.SetValue("longDescription", xmlProduct.Description);

            if (!string.IsNullOrWhiteSpace(xmlProduct.AffCode))
            {
                product.SetValue("affLink", xmlProduct.AffCode);
            }

            // Setează categoria
            if (!string.IsNullOrWhiteSpace(xmlProduct.Category))
            {
                var categoryPage = await GetOrCreateCategoryAsync(xmlProduct.Category, categoriesPage, xmlProduct.Image1);
                var categoryUdi = Udi.Create(Constants.UdiEntityType.Document, categoryPage.Key).ToString();
                product.SetValue("categories", categoryUdi);
            }

            // Setează imaginile
            if (!string.IsNullOrWhiteSpace(xmlProduct.Image1))
                product.SetValue("image1", xmlProduct.Image1);
            if (!string.IsNullOrWhiteSpace(xmlProduct.Image2))
                product.SetValue("image2", xmlProduct.Image2);
            if (!string.IsNullOrWhiteSpace(xmlProduct.Image3))
                product.SetValue("image3", xmlProduct.Image3);

            _contentService.SaveAndPublish(product);
            _logger.LogInformation("✅ Produs nou adăugat: {0} (SKU: {1})", xmlProduct.Title, xmlProduct.Sku);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Eroare la crearea produsului: {0}", xmlProduct.Sku);
            return false;
        }
    }

    private async Task<IContent> GetOrCreateCollectionAsync(string collectionName, IContent productsPage)
    {
        var collectionPage = _contentService
            .GetPagedChildren(productsPage.Id, 0, int.MaxValue, out _)
            .FirstOrDefault(x => x.ContentType.Alias == "collectionPage" &&
                               x.Name.Equals(collectionName, StringComparison.OrdinalIgnoreCase));

        if (collectionPage == null)
        {
            collectionPage = _contentService.Create(collectionName, productsPage.Id, "collectionPage");
            _contentService.SaveAndPublish(collectionPage);
            _logger.LogInformation("🏷️ CollectionPage creată: {0}", collectionName);
        }

        return collectionPage;
    }

    private async Task<IContent> GetOrCreateCategoryAsync(string categoryName, IContent categoriesPage, string imageUrl)
    {
        var categoryPage = _contentService
            .GetPagedChildren(categoriesPage.Id, 0, int.MaxValue, out _)
            .FirstOrDefault(x => x.ContentType.Alias == "categoryPage" &&
                               x.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

        if (categoryPage == null)
        {
            categoryPage = _contentService.Create(categoryName, categoriesPage.Id, "categoryPage");

            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                categoryPage.SetValue("image1", imageUrl);
            }

            _contentService.SaveAndPublish(categoryPage);
            _logger.LogInformation("🏷️ Categorie creată: {0}", categoryName);
        }

        return categoryPage;
    }
}

/// <summary>
/// Model pentru datele produsului din XML
/// </summary>
public class XmlProductData
{
    public string Sku { get; set; }
    public string Collection { get; set; }
    public string Title { get; set; }
    public decimal Price { get; set; }
    public string Brand { get; set; }
    public string Color { get; set; }
    public string Gen { get; set; }
    public string Category { get; set; }
    public string AffCode { get; set; }
    public string Image1 { get; set; }
    public string Image2 { get; set; }
    public string Image3 { get; set; }
    public string Description { get; set; }
    public string SourceXml { get; set; }
}

// using Microsoft.Extensions.Logging;
// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using System.Threading.Tasks;
// using System.Xml.Linq;
// using Umbraco.Cms.Core;
// using Umbraco.Cms.Core.Models;
// using Umbraco.Cms.Core.Services;
// using Umbraco.Extensions;
// using System.Diagnostics;
//
// public class ProductImporterLogic
// {
//     private readonly IContentService _contentService;
//     private readonly ILogger<ProductImporterLogic> _logger;
//     private readonly IMediaService _mediaService;
//
//     private readonly string[] _xmlPaths = {
//         "wwwroot/data/preprocessed-depurtat.xml",
//         "wwwroot/data/preprocessed-fashiondays.xml",
//         "wwwroot/data/otter-days-preprocessed.xml",
//         "wwwroot/data/preprocessed-sosetaria.xml"
//     };
//
//     public ProductImporterLogic(
//         IContentService contentService,
//         ILogger<ProductImporterLogic> logger,
//         IMediaService mediaService)
//     {
//         _contentService = contentService;
//         _logger = logger;
//         _mediaService = mediaService;
//     }
//
//     public async Task RunImportAsync()
//     {
//         var stopwatch = Stopwatch.StartNew();
//         try
//         {
//             _logger.LogInformation("🚀 Începe sincronizarea optimizată pentru RAM limitat din {0} XML-uri", _xmlPaths.Length);
//
//             var (productsPage, categoriesPage) = GetUmbracoStructure();
//             if (productsPage == null || categoriesPage == null) return;
//
//             var existingProducts = GetAllExistingProducts(productsPage);
//             _logger.LogInformation("📊 Produse existente în Umbraco: {0}", existingProducts.Count);
//
//             int totalProcessed = 0;
//             var processedSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
//
//             foreach (var xmlPath in _xmlPaths)
//             {
//                 _logger.LogInformation("📂 Procesează: {0}", Path.GetFileName(xmlPath));
//
//                 var processed = await ProcessSingleXmlFileAsync(xmlPath, existingProducts, productsPage, categoriesPage);
//                 totalProcessed += processed;
//
//                 var xmlProducts = await GetXmlProductsFromFile(xmlPath);
//                 foreach (var product in xmlProducts)
//                 {
//                     processedSkus.Add(product.Sku);
//                 }
//
//                 GC.Collect();
//                 GC.WaitForPendingFinalizers();
//
//                 _logger.LogInformation("✅ Finalizat {0} - Produse procesate: {1}", Path.GetFileName(xmlPath), processed);
//             }
//
//             await DeleteRemovedProductsAsync(existingProducts, processedSkus);
//
//             _logger.LogInformation("✅ Sincronizare completă finalizată. Total procesate: {0}", totalProcessed);
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "❌ Eroare la sincronizarea produselor.");
//         }
//         finally
//         {
//             stopwatch.Stop();
//             _logger.LogInformation("⏱️ Sincronizare finalizată în {0} secunde", stopwatch.Elapsed.TotalSeconds.ToString("0.00"));
//         }
//     }
//
//     private async Task<int> ProcessSingleXmlFileAsync(
//         string xmlPath,
//         Dictionary<string, IContent> existingProducts,
//         IContent productsPage,
//         IContent categoriesPage)
//     {
//         try
//         {
//             if (!System.IO.File.Exists(xmlPath))
//             {
//                 _logger.LogWarning(" Fișierul XML nu există: {0}", xmlPath);
//                 return 0;
//             }
//
//             var xmlProducts = await GetXmlProductsFromFile(xmlPath);
//             if (!xmlProducts.Any())
//             {
//                 _logger.LogWarning(" Nu s-au găsit produse în: {0}", Path.GetFileName(xmlPath));
//                 return 0;
//             }
//
//             _logger.LogInformation(" {0} - Produse găsite: {1}", Path.GetFileName(xmlPath), xmlProducts.Count);
//
//             int processedCount = 0;
//
//             const int batchSize = 10;
//             for (int i = 0; i < xmlProducts.Count; i += batchSize)
//             {
//                 var batch = xmlProducts.Skip(i).Take(batchSize).ToList();
//
//                 foreach (var xmlProduct in batch)
//                 {
//                     try
//                     {
//                         if (existingProducts.TryGetValue(xmlProduct.Sku, out var existingProduct))
//                         {
//                             await UpdateProductIfNeededAsync(existingProduct, xmlProduct, categoriesPage);
//                         }
//                         else
//                         {
//                             await CreateNewProductAsync(xmlProduct, productsPage, categoriesPage);
//                         }
//                         processedCount++;
//
//                         await Task.Delay(200);
//                     }
//                     catch (Exception ex)
//                     {
//                         _logger.LogError(ex, " Eroare la procesarea produsului {0}", xmlProduct.Sku);
//                         await Task.Delay(1000);
//                     }
//                 }
//
//                 await Task.Delay(2000);
//
//                 if (i % (batchSize * 4) == 0) 
//                 {
//                     GC.Collect();
//                 }
//             }
//
//             return processedCount;
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, " Eroare la procesarea fișierului: {0}", xmlPath);
//             return 0;
//         }
//     }
//
//     private async Task<List<XmlProductData>> GetXmlProductsFromFile(string xmlPath)
//     {
//         var products = new List<XmlProductData>();
//
//         try
//         {
//             var xmlContent = await System.IO.File.ReadAllTextAsync(xmlPath);
//             var doc = XDocument.Parse(xmlContent);
//             var items = doc.Descendants("item").ToList();
//
//             foreach (var item in items)
//             {
//                 var productData = ParseXmlItem(item, xmlPath);
//                 if (productData != null)
//                 {
//                     products.Add(productData);
//                 }
//             }
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, " Eroare la citirea XML-ului: {0}", xmlPath);
//         }
//
//         return products;
//     }
//
//     private async Task DeleteRemovedProductsAsync(Dictionary<string, IContent> existingProducts, HashSet<string> processedSkus)
//     {
//         var skusToDelete = existingProducts.Keys.Where(sku => !processedSkus.Contains(sku)).ToList();
//
//         _logger.LogInformation(" Produse de șters: {0}", skusToDelete.Count);
//
//         int deletedCount = 0;
//         foreach (var skuToDelete in skusToDelete)
//         {
//             try
//             {
//                 if (existingProducts.TryGetValue(skuToDelete, out var productToDelete))
//                 {
//                     _contentService.Delete(productToDelete);
//                     deletedCount++;
//
//                     if (deletedCount % 10 == 0)
//                     {
//                         await Task.Delay(50);
//                     }
//                 }
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, " Eroare la ștergerea produsului {0}", skuToDelete);
//             }
//         }
//
//         _logger.LogInformation(" Produse șterse: {0}", deletedCount);
//     }
//
//     private XmlProductData ParseXmlItem(XElement item, string sourceXml)
//     {
//         try
//         {
//             string sku = item.Element("sku")?.Value?.Trim();
//             string title = item.Element("title")?.Value?.Trim();
//
//             if (string.IsNullOrWhiteSpace(sku) || string.IsNullOrWhiteSpace(title))
//             {
//                 return null; 
//             }
//
//             return new XmlProductData
//             {
//                 Sku = sku,
//                 Collection = item.Element("collection")?.Value?.Trim() ?? "Colecție necunoscută",
//                 Title = title,
//                 Price = decimal.TryParse(item.Element("price")?.Value?.Trim(), out var price) ? price : 0,
//                 Brand = item.Element("brand")?.Value?.Trim(),
//                 Color = item.Element("color")?.Value?.Trim() ?? "necunoscut",
//                 Gen = item.Element("gen")?.Value?.Trim()
//                 ?? item.Element("gender")?.Value?.Trim()
//                 ?? "Femei",
//                 Category = item.Element("category")?.Value?.Trim(),
//                 AffCode = item.Element("aff_code")?.Value?.Trim(),
//                 Image1 = item.Element("image1")?.Value?.Trim(),
//                 Image2 = item.Element("image2")?.Value?.Trim(),
//                 Image3 = item.Element("image3")?.Value?.Trim(),
//                 Description = item.Element("description")?.Value?.Trim(),
//                 SourceXml = sourceXml
//             };
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, " Eroare la parsarea produsului din {0}", Path.GetFileName(sourceXml));
//             return null;
//         }
//     }
//
//     private (IContent productsPage, IContent categoriesPage) GetUmbracoStructure()
//     {
//         var root = _contentService.GetRootContent().FirstOrDefault();
//         if (root == null)
//         {
//             _logger.LogError(" Nu există niciun nod root în Content.");
//             return (null, null);
//         }
//
//         var productsPage = _contentService
//             .GetPagedChildren(root.Id, 0, 100, out _)
//             .FirstOrDefault(x => x.ContentType.Alias == "productsPage");
//
//         var categoriesPage = _contentService
//             .GetPagedChildren(root.Id, 0, int.MaxValue, out _)
//             .FirstOrDefault(x => x.ContentType.Alias == "categoriesPage");
//
//         if (productsPage == null)
//         {
//             _logger.LogError(" Nodul 'productsPage' nu a fost găsit.");
//         }
//
//         if (categoriesPage == null)
//         {
//             _logger.LogError(" Nodul 'categoriesPage' nu a fost găsit.");
//         }
//
//         return (productsPage, categoriesPage);
//     }
//
//     private Dictionary<string, IContent> GetAllExistingProducts(IContent productsPage)
//     {
//         var existingProducts = new Dictionary<string, IContent>(StringComparer.OrdinalIgnoreCase);
//
//         var allCollections = _contentService
//             .GetPagedChildren(productsPage.Id, 0, int.MaxValue, out _)
//             .Where(x => x.ContentType.Alias == "collectionPage");
//
//         foreach (var collection in allCollections)
//         {
//             var products = _contentService
//                 .GetPagedChildren(collection.Id, 0, int.MaxValue, out _)
//                 .Where(x => x.ContentType.Alias == "productPage");
//
//             foreach (var product in products)
//             {
//                 var sku = product.GetValue<string>("sku");
//                 if (!string.IsNullOrWhiteSpace(sku))
//                 {
//                     existingProducts[sku] = product;
//                 }
//             }
//         }
//
//         return existingProducts;
//     }
//
//     private async Task<bool> UpdateProductIfNeededAsync(IContent existingProduct, XmlProductData xmlProduct, IContent categoriesPage)
//     {
//         var currentPrice = existingProduct.GetValue<decimal>("Price");
//
//         if (currentPrice == xmlProduct.Price)
//         {
//             return false;
//         }
//
//         existingProduct.SetValue("Price", xmlProduct.Price);
//
//         existingProduct.SetValue("color", xmlProduct.Color);
//         existingProduct.SetValue("longDescription", xmlProduct.Description);
//
//         if (!string.IsNullOrWhiteSpace(xmlProduct.Category))
//         {
//             var categoryPage = await GetOrCreateCategoryAsync(xmlProduct.Category, categoriesPage, xmlProduct.Image1);
//             var categoryUdi = Udi.Create(Constants.UdiEntityType.Document, categoryPage.Key).ToString();
//             existingProduct.SetValue("categories", categoryUdi);
//         }
//
//         if (!string.IsNullOrWhiteSpace(xmlProduct.Image1))
//             existingProduct.SetValue("image1", xmlProduct.Image1);
//         if (!string.IsNullOrWhiteSpace(xmlProduct.Image2))
//             existingProduct.SetValue("image2", xmlProduct.Image2);
//         if (!string.IsNullOrWhiteSpace(xmlProduct.Image3))
//             existingProduct.SetValue("image3", xmlProduct.Image3);
//
//         _contentService.SaveAndPublish(existingProduct);
//         return true;
//     }
//
//     private async Task<bool> CreateNewProductAsync(XmlProductData xmlProduct, IContent productsPage, IContent categoriesPage)
//     {
//         try
//         {
//             var collectionPage = await GetOrCreateCollectionAsync(xmlProduct.Collection, productsPage);
//
//             var product = _contentService.Create(xmlProduct.Title, collectionPage.Id, "productPage");
//
//             product.SetValue("sku", xmlProduct.Sku);
//             product.SetValue("color", xmlProduct.Color);
//             product.SetValue("brand", xmlProduct.Brand?.ToLower());
//             product.SetValue("Price", xmlProduct.Price);
//             product.SetValue("gen", xmlProduct.Gen);
//             product.SetValue("longDescription", xmlProduct.Description);
//
//             if (!string.IsNullOrWhiteSpace(xmlProduct.AffCode))
//             {
//                 product.SetValue("affLink", xmlProduct.AffCode);
//             }
//
//             if (!string.IsNullOrWhiteSpace(xmlProduct.Category))
//             {
//                 var categoryPage = await GetOrCreateCategoryAsync(xmlProduct.Category, categoriesPage, xmlProduct.Image1);
//                 var categoryUdi = Udi.Create(Constants.UdiEntityType.Document, categoryPage.Key).ToString();
//                 product.SetValue("categories", categoryUdi);
//             }
//
//             if (!string.IsNullOrWhiteSpace(xmlProduct.Image1))
//                 product.SetValue("image1", xmlProduct.Image1);
//             if (!string.IsNullOrWhiteSpace(xmlProduct.Image2))
//                 product.SetValue("image2", xmlProduct.Image2);
//             if (!string.IsNullOrWhiteSpace(xmlProduct.Image3))
//                 product.SetValue("image3", xmlProduct.Image3);
//
//             _contentService.SaveAndPublish(product);
//             return true;
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, " Eroare la crearea produsului: {0}", xmlProduct.Sku);
//             return false;
//         }
//     }
//
//     private async Task<IContent> GetOrCreateCollectionAsync(string collectionName, IContent productsPage)
//     {
//         var collectionPage = _contentService
//             .GetPagedChildren(productsPage.Id, 0, int.MaxValue, out _)
//             .FirstOrDefault(x => x.ContentType.Alias == "collectionPage" &&
//                                x.Name.Equals(collectionName, StringComparison.OrdinalIgnoreCase));
//
//         if (collectionPage == null)
//         {
//             collectionPage = _contentService.Create(collectionName, productsPage.Id, "collectionPage");
//             _contentService.SaveAndPublish(collectionPage);
//         }
//
//         return collectionPage;
//     }
//
//     private async Task<IContent> GetOrCreateCategoryAsync(string categoryName, IContent categoriesPage, string imageUrl)
//     {
//         var categoryPage = _contentService
//             .GetPagedChildren(categoriesPage.Id, 0, int.MaxValue, out _)
//             .FirstOrDefault(x => x.ContentType.Alias == "categoryPage" &&
//                                x.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
//
//         if (categoryPage == null)
//         {
//             categoryPage = _contentService.Create(categoryName, categoriesPage.Id, "categoryPage");
//
//             if (!string.IsNullOrWhiteSpace(imageUrl))
//             {
//                 categoryPage.SetValue("image1", imageUrl);
//             }
//
//             _contentService.SaveAndPublish(categoryPage);
//         }
//
//         return categoryPage;
//     }
// }
//
// public class XmlProductData
// {
//     public string Sku { get; set; }
//     public string Collection { get; set; }
//     public string Title { get; set; }
//     public decimal Price { get; set; }
//     public string Brand { get; set; }
//     public string Color { get; set; }
//     public string Gen { get; set; }
//     public string Category { get; set; }
//     public string AffCode { get; set; }
//     public string Image1 { get; set; }
//     public string Image2 { get; set; }
//     public string Image3 { get; set; }
//     public string Description { get; set; }
//     public string SourceXml { get; set; }
// }