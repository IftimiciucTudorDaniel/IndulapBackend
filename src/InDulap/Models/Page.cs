using Umbraco.Extensions;

namespace InDulap.Models
{
    public partial class Page
    {
        public HomePage HomePage => this.AncestorOrSelf<HomePage>();

        public string MetaTitle
        {
            get
            {
                if (!PageTitle.IsNullOrWhiteSpace())
                    return PageTitle;

                if (Id == HomePage.Id)
                    return $"{HomePage.SiteName} - {HomePage.SiteDescription}";

                return $"{Name} | {HomePage.SiteName}";
            }
        }
    }
}
