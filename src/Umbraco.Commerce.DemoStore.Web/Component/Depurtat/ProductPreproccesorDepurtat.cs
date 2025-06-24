﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Umbraco.Commerce.DemoStore.Web.Component
{
    public class ProductPreprocessorDepurtat
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
            ["animal print"] = "Aniaml Print",
            ["fuchsia"] = "Fucsia",
            ["khaki"] = "Khaki",
            ["multi"] = "Multicolor",
            ["bleumarin"] = "Bleumarin",
            ["print"] = "Print",
            ["aurie"] = "Auriu",
            ["beige"] = "bej",
            ["zebra"] = "Zebra"
        };

        public XElement Preprocess(XElement item)
        {
            string title = item.Element("title")?.Value?.Trim();
            string description = item.Element("description")?.Value?.Trim() ?? "";
            string price = item.Element("price")?.Value ?? "0";
            string brand = item.Element("brand")?.Value ?? "";
            string category = item.Element("category")?.Value ?? "necunoscut";
            string affCode = item.Element("aff_code")?.Value ?? "";
            string gender = "Femei";
            string product_id = item.Element("product_id").Value ?? "";

            var imageUrls = item.Element("image_urls")?.Value?
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim()).ToList() ?? new();

            string color = ExtractColor(title + " " + description);

            return new XElement("item",
                new XElement("sku", product_id),
                new XElement("collection", "Depurtat"),
                new XElement("title", title),
                new XElement("price", price),
                new XElement("brand", brand),
                new XElement("color", color),
                new XElement("gen", gender),
                new XElement("category", $"{category}-{gender}"),
                new XElement("aff_code", affCode),
                new XElement("image1", imageUrls.ElementAtOrDefault(0) ?? ""),
                new XElement("image2", imageUrls.ElementAtOrDefault(1) ?? ""),
                new XElement("image3", imageUrls.ElementAtOrDefault(2) ?? ""),
                new XElement("description", description)
            );
        }

        private string ExtractColor(string text)
        {
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words)
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
            var url = "https://api.2performant.com/feed/2050d2bfb.xml";

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
            var outputPath = Path.Combine(projectRoot, "wwwroot", "data", "preprocessed-depurtat.xml");

            finalDoc.Save(outputPath);
            return outputPath;
        }
    }
}
