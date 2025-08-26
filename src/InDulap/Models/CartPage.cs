using Umbraco.Commerce.Core.Models;

namespace InDulap.Models
{
    public partial class CartPage
    {
        public CheckoutPage CheckoutPage => this.GetHomePage().CheckoutPage;

        public OrderReadOnly Order => this.GetCurrentOrder();
    }
}
