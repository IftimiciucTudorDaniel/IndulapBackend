using Umbraco.Cms.Core.Models.PublishedContent;
using InDulap.Models;
using Umbraco.Commerce.Cms.Extractors;
using Umbraco.Commerce.Cms.Helpers;

namespace InDulap.Web.Extractors
{
    public class CompositeProductNameExtractor : UmbracoProductNameExtractor
    {
        public CompositeProductNameExtractor(PublishedContentHelperAccessor publishedContentHelperAccessor)
            : base(publishedContentHelperAccessor)
        { }

        public override string ExtractProductName(IPublishedContent content, IPublishedElement variant, string languageIsoCode)
        {
            var productNamePrefix = content.ContentType.Alias == ProductVariant.ModelTypeAlias
                ? $"{content.Parent.Parent.Name} - {content.Parent.Name}"
                : content.Parent.Name;

            return $"{productNamePrefix} - {base.ExtractProductName(content, variant, languageIsoCode)}";
        }
    }
}
