using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Umbraco.Commerce.DemoStore.Web.Component
{
    public class FashionDaysPreprocessor
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
            ["rosie"] = "Rosu",
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
            ["animal print"] = "Animal Print",
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
            string title = item.Element("title")?.Value?.Trim();
            string description = item.Element("description")?.Value?.Trim() ?? "";
            string price = item.Element("price")?.Value ?? "0";
            string affCode = item.Element("aff_code")?.Value ?? "";
            string campaignName = item.Element("campaign_name")?.Value ?? "";
            string xmlCategory = item.Element("category")?.Value?.Trim() ?? "";
            string product_id = item.Element("product_id")?.Value ?? "";

            var imageUrls = item.Element("image_urls")?.Value?
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim()).ToList() ?? new();

            var (gender, category) = ExtractGenderAndCategoryFromXmlCategory(xmlCategory);

            if (category == "necunoscut")
                return null;

            string finalCategory = $"{category}-{gender}";

            string color = ExtractColorFromDescription(description);

            string material = ExtractMaterialFromDescription(description);

            string brand = "fashiondays.ro";
            
            return new XElement("item",
                new XElement("sku", product_id),
                new XElement("collection", "FashionDays"),
                new XElement("title", title),
                new XElement("price", price),
                new XElement("brand", brand),
                new XElement("color", color),
                new XElement("material", material),
                new XElement("gender", gender),
                new XElement("category", finalCategory),
                new XElement("aff_code", affCode),
                new XElement("image1", imageUrls.ElementAtOrDefault(0) ?? ""),
                new XElement("image2", imageUrls.ElementAtOrDefault(1) ?? ""),
                new XElement("image3", imageUrls.ElementAtOrDefault(2) ?? ""),
                new XElement("description", description)
            );
        }

        private (string gender, string category) ExtractGenderAndCategoryFromXmlCategory(string xmlCategory)
        {
            if (string.IsNullOrEmpty(xmlCategory))
                return ("Unisex", "necunoscut");

            var parts = xmlCategory.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
                return ("Unisex", "necunoscut");

            string genderPart = parts[0].Trim();
            string gender = MapGenderFromCategory(genderPart);

            string categoryPart = parts[parts.Length - 1].Trim();
            string category = MapCategoryFromXml(categoryPart);

            return (gender, category);
        }

        private string MapGenderFromCategory(string genderPart)
        {
            var genderMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["FEMEI"] = "Femei",
                ["BARBATI"] = "Barbati",
                ["FETITE"] = "Fetite",
                ["BAIETI"] = "Baieti",
                ["COPII"] = "Copii",
                ["UNISEX"] = "Unisex"
            };

            return genderMapping.TryGetValue(genderPart, out var mappedGender) ? mappedGender : "Unisex";
        }

        private string MapCategoryFromXml(string categoryPart)
        {
            var xmlCategoryMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Camasi"] = "Camasi",
                ["Tricouri"] = "Tricouri",
                ["Bluze"] = "Bluze",
                ["Rochii"] = "Rochii",
                ["Pantaloni"] = "Pantaloni",
                ["Fuste"] = "Fuste",
                ["Jachete"] = "Jachete",
                ["Pulovere"] = "Pulovere",
                ["Costume"] = "Costume",
                ["Salopete"] = "Salopete",
                ["Cardigane"] = "Cardigane",
                ["Topuri"] = "Topuri",
                ["Bermude"] = "Bermude",
                ["Shorturi"] = "Shorturi",
                ["Veste"] = "Veste",
                ["Blazere"] = "Blazere",
                ["Combinezon"] = "Combinezon",
                ["Tenisi"] = "Tenisi",
                ["Adidasi"] = "Adidasi",
                ["Pantofi"] = "Pantofi",
                ["Sandale"] = "Sandale",
                ["Papuci"] = "Papuci",
                ["Cizme"] = "Cizme",
                ["Ghete"] = "Ghete",
                ["Mocasini"] = "Mocasini",
                ["Espadrile"] = "Espadrile",
                ["Balerini"] = "Balerini",
                ["Botine"] = "Botine",
                ["Slapi"] = "Slapi"
            };

            return xmlCategoryMapping.TryGetValue(categoryPart, out var mappedCategory) ? mappedCategory : "necunoscut";
        }

        private string ExtractColorFromDescription(string description)
        {
            if (string.IsNullOrEmpty(description))
                return "necunoscut";

            var colorMatch = Regex.Match(description, @"Culoare:\s*([^\s]+)", RegexOptions.IgnoreCase);
            if (colorMatch.Success)
            {
                string extractedColor = colorMatch.Groups[1].Value.Trim();
                string normalizedColor = NormalizeText(extractedColor);

                if (ColorMap.TryGetValue(normalizedColor, out var mappedColor))
                    return mappedColor;

                return extractedColor;
            }

            return "necunoscut";
        }

        private string ExtractMaterialFromDescription(string description)
        {
            if (string.IsNullOrEmpty(description))
                return "necunoscut";

            var materialMatch = Regex.Match(description, @"Material:\s*([^\s]+)", RegexOptions.IgnoreCase);
            if (materialMatch.Success)
            {
                return materialMatch.Groups[1].Value.Trim();
            }

            var materialPatterns = new[]
            {
                @"Partea superioara:\s*([^;]+)",
                @"Material interior:\s*([^;]+)",
                @"Compozitie[^:]*:\s*([^;]+)"
            };

            foreach (var pattern in materialPatterns)
            {
                var match = Regex.Match(description, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }
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
                Timeout = TimeSpan.FromMinutes(10) 
            };

            var xmlContent = await httpClient.GetStringAsync(url);

            var doc = XDocument.Parse(xmlContent);
            var processedItems = new XElement("items");

            foreach (var item in doc.Descendants("item"))
            {
                var campaignName = item.Element("campaign_name")?.Value;
                if (campaignName?.Contains("fashiondays.ro", StringComparison.OrdinalIgnoreCase) == true)
                {
                    var processed = Preprocess(item);
                    if (processed != null)
                    {
                        processedItems.Add(processed);
                    }
                }
            }

            var finalDoc = new XDocument(processedItems);

            var projectRoot = Directory.GetCurrentDirectory();
            var outputPath = Path.Combine(projectRoot, "wwwroot", "data", "preprocessed-fashiondays.xml");

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            finalDoc.Save(outputPath);
            return outputPath;
        }
    }
}