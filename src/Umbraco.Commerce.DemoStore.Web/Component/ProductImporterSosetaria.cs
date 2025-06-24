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

public class ProductImporterSosetaria
{
    private readonly IContentService _contentService;
    private readonly ILogger<ProductImporterSosetaria> _logger;
    private readonly IMediaService _mediaService;
    private readonly MediaFileManager _mediaFileManager;

    public ProductImporterSosetaria(
        IContentService contentService,
        ILogger<ProductImporterSosetaria> logger,
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
            var xmlPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "sosetaria......xml");
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

            foreach (var item in items)
{
    string title = item.Element("title")?.Value?.Trim();
    if (string.IsNullOrWhiteSpace(title))
    {
        _logger.LogWarning("❌ Titlu gol. Se omite produsul.");
        continue;
    }

    string sku = $"SOS-{GetSkuFromTitle(title)}";

    string description = item.Element("description")?.Value?.Trim();
    string imageUrls = item.Element("image_urls")?.Value?.Trim();
    string affLink = item.Element("aff_code")?.Value?.Trim();
    string priceStr = item.Element("price")?.Value?.Trim();
    decimal.TryParse(priceStr, out var price);

    string campaignName = item.Element("campaign_name")?.Value?.Trim();
    string collectionName = !string.IsNullOrWhiteSpace(campaignName)
        ? campaignName.Split('.')[0]
        : "Colecție necunoscută";

    var collectionPage = _contentService
        .GetPagedChildren(productsPage.Id, 0, int.MaxValue, out _)
        .FirstOrDefault(x =>
            x.ContentType.Alias == "collectionPage" &&
            x.Name.Equals(collectionName, StringComparison.OrdinalIgnoreCase));

    if (collectionPage == null)
    {
        collectionPage = _contentService.Create(collectionName, productsPage.Id, "collectionPage");
        _contentService.SaveAndPublish(collectionPage);
        _logger.LogInformation("✅ Colecție creată: {0}", collectionName);
    }

    var product = _contentService.Create(title, collectionPage.Id, "productPage");

    product.SetValue("sku", sku); // ✅ setăm SKU-ul generat
    product.SetValue("longDescription", description);
    product.SetValue("Price", price);
    product.SetValue("affLink", affLink);
    product.SetValue("gen", "Femei");

    if (!string.IsNullOrWhiteSpace(imageUrls))
    {
        var urls = imageUrls.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
        if (urls.Count > 0) product.SetValue("image1", urls[0]);
        if (urls.Count > 1) product.SetValue("image2", urls[1]);
        if (urls.Count > 2) product.SetValue("image3", urls[2]);
    }

    string categoryName = $"{title.Split(' ').FirstOrDefault()}-Femei";
    var categoryPage = _contentService
        .GetPagedChildren(categoriesPage.Id, 0, int.MaxValue, out _)
        .FirstOrDefault(x =>
            x.ContentType.Alias == "categoryPage" &&
            x.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

    if (categoryPage == null)
    {
        categoryPage = _contentService.Create(categoryName, categoriesPage.Id, "categoryPage");
        if (!string.IsNullOrWhiteSpace(imageUrls))
        {
            var urls = imageUrls.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(u => u.Trim()).ToList();
            if (urls.Count > 0)
            {
                categoryPage.SetValue("image1", urls[0]);
            }
        }

        _contentService.SaveAndPublish(categoryPage);
        _logger.LogInformation("✅ Categorie creată: {0}", categoryName);
    }

    var categoryUdi = Udi.Create(Constants.UdiEntityType.Document, categoryPage.Key).ToString();
    product.SetValue("categories", categoryUdi);

    _contentService.SaveAndPublish(product);
    _logger.LogInformation("✅ Produs importat: {0}", title);
}


            _logger.LogInformation("✅ Import finalizat.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Eroare la import.");
        }
    }
    private string GetSkuFromTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(); // fallback

        // Normalizează titlul (doar litere și cifre)
        var cleaned = new string(title
                .Where(char.IsLetterOrDigit)
                .ToArray())
            .ToUpper();

        // Hash simplu din titlu
        int hash = cleaned.GetHashCode();
        return $"{cleaned.Substring(0, Math.Min(5, cleaned.Length))}-{Math.Abs(hash % 10000):D4}";
    }

}