using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blagodat.Models
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
           
            builder.ToTable("orders");
            builder.HasKey(o => o.OrderId).HasName("orders_pkey");
            
            


            builder.Property(o => o.OrderId).HasColumnName("order_id");
            builder.Property(o => o.OrderCode).HasColumnName("order_code");
            builder.Property(o => o.OrderDate).HasColumnName("order_date");
            builder.Property(o => o.ClientCode).HasColumnName("client_code");
            builder.Property(o => o.Status).HasColumnName("status");
            builder.Property(o => o.OrderTime).HasColumnName("order_time");
            builder.Property(o => o.TotalCost).HasColumnName("total_cost");
            builder.Property(o => o.ClosingDate).HasColumnName("closing_date");
            builder.Property(o => o.RentalTime).HasColumnName("rental_time");
            
            
            builder.Ignore(o => o.Id);  
            builder.Ignore(o => o.ClientId); 
            builder.Ignore(o => o.ServicesList); 
            
            // Связи
            builder.HasOne(o => o.ClientCodeNavigation)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.ClientCode)
                .HasPrincipalKey(c => c.Code);
                
            builder.HasMany(o => o.OrderServices)
                .WithOne(os => os.Order)
                .HasForeignKey(os => os.OrderId);
        }
    }
}
