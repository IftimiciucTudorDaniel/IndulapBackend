//using Umbraco.Cms.Core.Composing;
//using Umbraco.Cms.Core.DependencyInjection;
//using Umbraco.Cms.Core.Services;
//using Umbraco.Cms.Core.Models;
//using Microsoft.Extensions.Logging;
//using System.IO;
//using System.Linq;
//using System;
//using System.Net.Http;
//using System.Threading.Tasks;
//using System.Xml.Linq;
//using Umbraco.Cms.Core;
//using Umbraco.Cms.Core.IO;
//using File = System.IO.File;
//using Umbraco.Extensions;
//using Umbraco.Cms.Core.PropertyEditors;
//using MimeKit.Text;

//public class ProductImportComposer : IComposer
//{
//    public void Compose(IUmbracoBuilder builder)
//    {
//        builder.Components().Append<ProductImporterComponent>();
//    }
//}

//public class ProductImporterComponent : IComponent
//{
//    private readonly IContentService _contentService;
//    private readonly ILogger<ProductImporterComponent> _logger;
//    private readonly IMediaService _mediaService;
//    private readonly MediaFileManager _mediaFileManager;
//    private readonly IDataTypeService _dataTypeService;
//    private readonly IEntityService _entityService;

//    public ProductImporterComponent(
//        IContentService contentService,
//        ILogger<ProductImporterComponent> logger,
//        IMediaService mediaService,
//        MediaFileManager mediaFileManager,
//        IDataTypeService dataTypeService,
//        IEntityService entityService)
//    {
//        _mediaService = mediaService;
//        _contentService = contentService;
//        _logger = logger;
//        _mediaFileManager = mediaFileManager;
//        _dataTypeService = dataTypeService;
//        _entityService = entityService;
//    }

//    public async void Initialize()
//    {
//        try
//        {
//            var xmlPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "firsttest.xml");
//            var xmlContent = File.ReadAllText(xmlPath);
//            xmlContent = XmlUtils.FixInvalidAmpersands(xmlContent);
//            var doc = XDocument.Parse(xmlContent);
//            var items = doc.Descendants("item").ToList();

//            if (!items.Any())
//            {
//                _logger.LogWarning("Nu s-au găsit produse în fișierul XML.");
//                return;
//            }

//            var root = _contentService.GetRootContent().FirstOrDefault();
//            if (root == null)
//            {
//                _logger.LogWarning("Nu există niciun nod root în Content.");
//                return;
//            }

//            var productsPage = _contentService
//                .GetPagedChildren(root.Id, 0, 100, out _)
//                .FirstOrDefault(x => x.ContentType.Alias == "productsPage");

//            if (productsPage == null)
//            {
//                _logger.LogWarning("Nodul 'Products Page' nu a fost găsit.");
//                return;
//            }

//            var collectionPage = _contentService
//                .GetPagedChildren(productsPage.Id, 0, 100, out _)
//                .FirstOrDefault(x => x.ContentType.Alias == "collectionPage");

//            if (collectionPage == null)
//            {
//                collectionPage = _contentService.Create("Default Collection", productsPage.Id, "collectionPage");
//                _contentService.SaveAndPublish(collectionPage);
//            }

//            var categoriesPage = _contentService
//                .GetPagedChildren(root.Id, 0, int.MaxValue, out _)
//                .FirstOrDefault(x => x.ContentType.Alias == "categoriesPage");

//            if (categoriesPage != null)
//            {
//                _logger.LogInformation("Categorie 'categoriesPage' găsită: {0} (ID: {1})", categoriesPage.Name, categoriesPage.Id);
//            }
//            else
//            {
//                _logger.LogWarning("❌ Nu a fost găsită pagina 'categoriesPage' în ierarhia de conținut.");
//            }
//            foreach (var item in items)
//            {
//                string name = item.Element("title")?.Value?.Trim();
//                if (string.IsNullOrWhiteSpace(name))
//                {
//                    _logger.LogWarning("Produsul are titlu gol. Se sare peste.");
//                    continue;
//                }

//                bool exists = _contentService
//                    .GetPagedChildren(collectionPage.Id, 0, int.MaxValue, out _)
//                    .Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

//                if (exists)
//                    continue;

//                var product = _contentService.Create(name, collectionPage.Id, "productPage");

//                product.SetValue("sku", item.Element("product_id")?.Value);
//                product.SetValue("Price", decimal.TryParse(item.Element("price")?.Value, out var price) ? price : 0);
//                product.SetValue("shortDescription", item.Element("subcategory")?.Value);
//                product.SetValue("longDescription", item.Element("category")?.Value);

//                string affLink = item.Element("aff_code")?.Value?.Trim();
//                if (!string.IsNullOrWhiteSpace(affLink))
//                {
//                    product.SetValue("affLink", affLink);
//                }

//                // Verificăm dacă categoria există
//                var categoryName = item.Element("category")?.Value?.Trim();
//                if (!string.IsNullOrWhiteSpace(categoryName))
//                {
//                    // Căutăm categoria în lista de categoryPage
//                    var categoryPage = _contentService
//                        .GetPagedChildren(categoriesPage.Id, 0, int.MaxValue, out _)
//                        .FirstOrDefault(x => x.ContentType.Alias == "categoryPage" && x.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

//                    // Dacă categoria nu există, o creăm
//                    if (categoryPage == null)
//                    {
//                        _logger.LogWarning("❌ Categorie '{0}' nu a fost găsită. O voi crea acum.", categoryName);

//                        // Creăm categoria ca un nou page
//                        categoryPage = _contentService.Create(categoryName, categoriesPage.Id, "categoryPage");
//                        _contentService.SaveAndPublish(categoryPage);

//                        _logger.LogInformation("✅ Categorie creată: {0}", categoryName);
//                    }
//                    else
//                    {
//                        _logger.LogInformation("Categorie găsită: {0} pentru produsul {1}", categoryName, name);
//                    }

//                    // Creăm UDI pentru categoria găsită
//                    var categoryUdi = Udi.Create(Constants.UdiEntityType.Document, categoryPage.Key).ToString();

//                    // Setăm UDI-ul în produs
//                    product.SetValue("categories", categoryUdi);
//                }
//                else
//                {
//                    _logger.LogWarning("❌ Nu s-a găsit categoria pentru produsul '{0}'.", name);
//                }


//                var imageUrlsText = item.Element("image_urls")?.Value;
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

//                //var imageUrlsText = item.Element("image_urls")?.Value;
//                //if (!string.IsNullOrWhiteSpace(imageUrlsText))
//                //{
//                //    var urls = imageUrlsText.Split(',', StringSplitOptions.RemoveEmptyEntries)
//                //        .Select(u => u.Trim())
//                //        .ToList();

//                //    if (urls.Any())
//                //    {
//                //        var udi = await DownloadImageToMedia(urls.First(), name);
//                //        if (!string.IsNullOrWhiteSpace(udi))
//                //        {
//                //            product.SetValue("images", udi); // Salvează ca UDI string
//                //        }
//                //    }
//                //}



//                _contentService.SaveAndPublish(product);
//                _logger.LogInformation("✅ Produs adăugat: {0}", name);
//            }

//            _logger.LogInformation("✅ Import produse complet.");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Eroare la importul produselor.");
//        }
//    }

//    public void Terminate() { }

//    //private async Task<string> DownloadImageToMedia(string imageUrl, string name)
//    //{
//    //    try
//    //    {
//    //        using var client = new HttpClient();
//    //        var data = await client.GetByteArrayAsync(imageUrl);
//    //        var extension = Path.GetExtension(imageUrl).Split('?')[0];
//    //        var filename = $"{Guid.NewGuid()}{extension}";

//    //        var folder = _mediaService.GetRootMedia().FirstOrDefault(x => x.Name == "Product Images")
//    //                      ?? _mediaService.CreateMedia("Product Images", -1, Constants.Conventions.MediaTypes.Folder);
//    //        _mediaService.Save(folder);

//    //        var media = _mediaService.CreateMedia(name, folder.Id, Constants.Conventions.MediaTypes.Image);

//    //        using var stream = new MemoryStream(data);
//    //        var filepath = _mediaFileManager.FileSystem.GetRelativePath(filename);
//    //        _mediaFileManager.FileSystem.AddFile(filepath, stream, true);

//    //        media.SetValue("umbracoFile", _mediaFileManager.FileSystem.GetUrl(filepath));
//    //        _mediaService.Save(media);

//    //        _logger.LogInformation("✅ Imagine salvată pentru {0} la {1}", name, filepath);

//    //        return media.GetUdi().ToString();
//    //    }
//    //    catch (Exception ex)
//    //    {
//    //        _logger.LogWarning("❌ Eroare la descărcarea imaginii: {0}", ex.Message);
//    //        return null;
//    //    }
//    //}
//}