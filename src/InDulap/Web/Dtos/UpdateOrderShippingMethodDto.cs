using System;

namespace InDulap.Web.Dtos
{
    public class UpdateOrderShippingMethodDto
    {
        public Guid ShippingMethod { get; set; }
        public string ShippingOptionId { get; set; }

        public Guid? NextStep { get; set; }
    }
}
