using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;

namespace Umbraco.Commerce.DemoStore.Web.Component
{
    public class ProductPreprocessorOtterDays
    {
        private static readonly Dictionary<string, string> ColorMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["alb"] = "Alb",
            ["albi"] = "Alb",
            ["alba"] = "Alb",
            ["albe"] = "Alb",
            ["negru"] = "Negru",
            ["negri"] = "Negru",
            ["neagra"] = "Negru",
            ["negre"] = "Negru",
            ["rosu"] = "Rosu",
            ["rosii"] = "Rosu",
            ["rosie"] ="Rosu",
            ["verde"] = "Verde",
            ["verzi"] = "Verde",
            ["albastru"] = "Albastru",
            ["albastri"] = "Albastru",
            ["albastre"] = "Albastru",
            ["albastra"] = "Albastru",
            ["gri"] = "Gri",
            ["bej"] = "Bej",
            ["maro"] = "Maro",
            ["roz"] = "Roz",
            ["galben"] = "Galben",
            ["galbeni"] = "Galben",
            ["galbene"] = "Galben",
            ["portocaliu"] = "Portocaliu",
            ["portocalii"] = "Portocaliu",
            ["visiniu"] = "Visiniu",
            ["visinii"] = "Visiniu",
            ["mov"] = "Mov",
            ["turcoaz"] = "Turcoaz",
            ["auriu"] = "Auriu",
            ["aurii"] = "Auriu",
            ["argintiu"] = "Argintiu",
            ["argintie"] = "Argintiu",
            ["argintii"] = "Argintiu",
            ["multicolor"] = "Multicolor",
            ["kaki"] = "Kaki",
            ["nude"] = "Nude",
            ["bronz"] = "Bronz",
            ["coral"] = "Coral",
            ["indigo"] = "Indigo",
            ["lila"] = "Lila",
            ["fucsia"] = "Fucsia",
            ["lavanda"] = "Lavanda",
            ["menta"] = "Menta",
            ["crem"] = "Crem",
            ["camel"] = "Camel",
            ["caramiziu"] = "Caramiziu",
            ["grena"] = "Grena",
            ["ciocolatiu"] = "Ciocolatiu",
            ["sampanie"] = "Sampanie",
            ["petrol"] = "Petrol",
            ["burgundy"] = "Burgundy",
            ["alb-negru"] = "Alb-Negru",
            ["alb-negri"] = "Alb-Negru",
            ["roze"] = "Roz",
            ["animal print"] = "Aniaml Print",
            ["fuchsia"] = "Fucsia",
            ["khaki"] = "Khaki",
            ["multi"] = "Multicolor",
            ["bleumarin"] = "Bleumarin",
            ["print"] = "Print",
            ["aurie"] = "Auriu",
            ["beige"] = "Bej",
            ["zebra"] = "Zebra"
        };

        public XElement Preprocess(XElement item)
        {
            var originalTitle = item.Element("title")?.Value?.Trim();
            var description = item.Element("description")?.Value?.Trim() ?? "";
            string product_id = item.Element("product_id")?.Value ?? "";
            if (string.IsNullOrWhiteSpace(originalTitle))
                return null;

            string normalizedColor = ExtractColor(originalTitle, description);

            var words = originalTitle.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int uppercaseIndex = Array.FindIndex(words, w => w.All(char.IsUpper));
            string name = uppercaseIndex >= 0
                ? string.Join(' ', words.Take(uppercaseIndex + 1))
                : string.Join(' ', words.Take(3));

            string gender = item.Element("category")?.Value ?? "necunoscut";
            string subCategory = !string.IsNullOrWhiteSpace(name) ? $"{name.Split(' ')[0]}-{gender}" : $"necunoscut-{gender}";

            string material = originalTitle.Contains("din")
                ? originalTitle.Split(new[] { "din" }, StringSplitOptions.None).Skip(1).FirstOrDefault()?.Split(',')?.FirstOrDefault()?.Trim()
                : "necunoscut";

            var imageUrls = item.Element("image_urls")?.Value?
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(u => u.Trim()).ToList() ?? new();

            return new XElement("item",
                new XElement("sku", product_id),
                new XElement("collection", "OtterDays"),
                new XElement("title", originalTitle),
                new XElement("price", item.Element("price")?.Value ?? "0"),
                new XElement("brand", item.Element("brand")?.Value ?? ""),
                new XElement("color", normalizedColor),
                new XElement("gen", gender),
                new XElement("category", subCategory),
                new XElement("aff_code", item.Element("aff_code")?.Value ?? ""),
                new XElement("image1", imageUrls.ElementAtOrDefault(0) ?? ""),
                new XElement("image2", imageUrls.ElementAtOrDefault(1) ?? ""),
                new XElement("image3", imageUrls.ElementAtOrDefault(2) ?? ""),
                new XElement("description", description),
                new XElement("material", material)
            );
        }

        private string ExtractColor(string title, string description)
        {
            var texts = (title + " " + description).Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var word in texts)
            {
                string cleaned = NormalizeText(word.Trim(',', '.', ';', ':', '!', '?'));
                if (ColorMap.TryGetValue(cleaned, out var baseColor))
                    return baseColor;
            }

            return "necunoscut";
        }

        private string NormalizeText(string input)
        {
            if (input == null) return null;

            var normalized = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (char c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        }

        public async Task<string> GeneratePreprocessedXmlAsync()
        {
            var url = "https://api.2performant.com/feed/20dc1745f.xml";

            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5) 
            };

            var xmlContent = await httpClient.GetStringAsync(url);

            var doc = XDocument.Parse(xmlContent);
            var processedItems = new XElement("items");

            foreach (var item in doc.Descendants("item"))
            {
                var processed = Preprocess(item);
                if (processed != null)
                {
                    processedItems.Add(processed);
                }
            }

            var finalDoc = new XDocument(processedItems);

            var projectRoot = Directory.GetCurrentDirectory();
            var outputPath = Path.Combine(projectRoot, "wwwroot", "data", "otter-days-preprocessed.xml");

            finalDoc.Save(outputPath);

            return outputPath;
        }
    }
}
