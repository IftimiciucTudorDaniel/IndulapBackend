using InDulap.Models;
using Microsoft.EntityFrameworkCore;

namespace InDulap.Data
{
    public class InDulapContext: DbContext
    {
        public InDulapContext(DbContextOptions<InDulapContext> options)
            : base(options)
        {
        }
        
        public DbSet<ProductClickModel> ProductClicks { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<ProductClickModel>(entity =>
            {
                entity.ToTable("ProductClicks");
                entity.HasKey(e => e.ProductId);
                entity.Property(e => e.ProductId).HasColumnName("ProductId");
                entity.Property(e => e.Title).HasColumnName("Title");
                entity.Property(e => e.Clicks).HasColumnName("Clicks");
                entity.Property(e => e.ClickDate).HasColumnName("ClickDate");
            });
    }
}

