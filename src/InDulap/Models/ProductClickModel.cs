using System;

namespace InDulap.Models
{
    public class ProductClickModel
    {
        public Guid ProductId { get; set; }
        public string Title { get; set; }
        public int Clicks { get; set; }
        public DateTime ClickDate { get; set; }
    }
}

