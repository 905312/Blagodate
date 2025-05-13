using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blagodat.Models
{
    public class ClientConfiguration : IEntityTypeConfiguration<Client>
    {
        public void Configure(EntityTypeBuilder<Client> builder)
        {
            
            builder.ToTable("clients");
            builder.HasKey(c => c.Id).HasName("clients_pkey");
            
            

            builder.Property(c => c.Id).HasColumnName("client_id");
            builder.Property(c => c.Code).HasColumnName("code");
            builder.Property(c => c.Email).HasColumnName("email");
            builder.Property(c => c.Address).HasColumnName("address");
            builder.Property(c => c.BirthDate).HasColumnName("birth_date");
            builder.Property(c => c.PassportData).HasColumnName("passport_data");
            builder.Property(c => c.Password).HasColumnName("password");
            builder.Property(c => c.FullName).HasColumnName("full_name");
            
            
            builder.Ignore(c => c.Phone);
            
            
            builder.HasMany(c => c.Orders)
                .WithOne(o => o.ClientCodeNavigation)
                .HasForeignKey(o => o.ClientCode)
                .HasPrincipalKey(c => c.Code)
                .OnDelete(DeleteBehavior.ClientSetNull);
        }
    }
}
