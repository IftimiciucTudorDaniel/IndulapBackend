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
