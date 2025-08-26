using Umbraco.Commerce.Core.Models;

namespace InDulap.Models
{
    public interface IOrderReviewPage
    {
        OrderReadOnly Order { get; }

        CountryReadOnly PaymentCountry { get; }

        CountryReadOnly ShippingCountry { get; }
    }
}
