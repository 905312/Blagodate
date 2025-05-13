using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Blagodat.Models;

public partial class User21Context : DbContext
{
    public User21Context()
    {
    }

    public User21Context(DbContextOptions<User21Context> options)
        : base(options)
    {
    }

    public virtual DbSet<Agent> Agents { get; set; }

    public virtual DbSet<Agenttype> Agenttypes { get; set; }

    public virtual DbSet<Client> Clients { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<LoginHistory> LoginHistories { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<OrderService> OrderServices { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<Productsale> Productsales { get; set; }

    public virtual DbSet<Producttype> Producttypes { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=45.67.56.214;Port=5421;Database=user21;Username=user21;Password=ee16lojZ");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Применяем конфигурации для моделей
        modelBuilder.ApplyConfiguration(new ClientConfiguration());
        modelBuilder.ApplyConfiguration(new OrderConfiguration());
        
        // Явно указываем игнорировать свойство Phone у Client (дополнительная гарантия)
        modelBuilder.Entity<Client>().Ignore(c => c.Phone);
        modelBuilder.Entity<Agent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("agent_pkey");

            entity.ToTable("agent");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(300)
                .HasColumnName("address");
            entity.Property(e => e.Agenttypeid).HasColumnName("agenttypeid");
            entity.Property(e => e.Directorname)
                .HasMaxLength(100)
                .HasColumnName("directorname");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.Inn)
                .HasMaxLength(12)
                .HasColumnName("inn");
            entity.Property(e => e.Kpp)
                .HasMaxLength(9)
                .HasColumnName("kpp");
            entity.Property(e => e.Logo)
                .HasMaxLength(100)
                .HasColumnName("logo");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.Priority).HasColumnName("priority");
            entity.Property(e => e.Title)
                .HasMaxLength(150)
                .HasColumnName("title");

            entity.HasOne(d => d.Agenttype).WithMany(p => p.Agents)
                .HasForeignKey(d => d.Agenttypeid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_agent_agenttype");
        });

        modelBuilder.Entity<Agenttype>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("agenttype_pkey");

            entity.ToTable("agenttype");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Image)
                .HasMaxLength(100)
                .HasColumnName("image");
            entity.Property(e => e.Title)
                .HasMaxLength(50)
                .HasColumnName("title");
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("clients_pkey");

            entity.ToTable("clients");

            entity.HasIndex(e => e.Code, "clients_code_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("client_id");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.BirthDate).HasColumnName("birth_date");
            entity.Property(e => e.Code)
                .HasMaxLength(8)
                .HasColumnName("code");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.PassportData)
                .HasMaxLength(50)
                .HasColumnName("passport_data");
            entity.Property(e => e.Password)
                .HasMaxLength(50)
                .HasColumnName("password");
            

            entity.Property(e => e.FullName)
                .HasColumnName("full_name")
                .HasMaxLength(100);
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("employees_pkey");

            entity.ToTable("employees");

            entity.HasIndex(e => e.Code, "employees_code_key").IsUnique();

            entity.HasIndex(e => e.Login, "employees_login_key").IsUnique();

            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.Code)
                .HasMaxLength(10)
                .HasColumnName("code");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.LastLogin)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("last_login");
            entity.Property(e => e.Login)
                .HasMaxLength(50)
                .HasColumnName("login");
            entity.Property(e => e.LoginType)
                .HasMaxLength(20)
                .HasColumnName("login_type");
            entity.Property(e => e.Password)
                .HasMaxLength(50)
                .HasColumnName("password");
            entity.Property(e => e.Position)
                .HasMaxLength(100)
                .HasColumnName("position");
        });

        modelBuilder.Entity<LoginHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("login_history_pkey");

            entity.ToTable("login_history");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.LoginTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("login_time");
            entity.Property(e => e.Success).HasColumnName("success");
            entity.Property(e => e.UserLogin)
                .HasMaxLength(50)
                .HasColumnName("user_login");

            entity.HasOne(d => d.UserLoginNavigation).WithMany(p => p.LoginHistories)
                .HasPrincipalKey(p => p.Login)
                .HasForeignKey(d => d.UserLogin)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_user");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("orders_pkey");

            entity.ToTable("orders");

            entity.HasIndex(e => e.OrderCode, "orders_order_code_key").IsUnique();

            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.ClientCode)
                .HasMaxLength(8)
                .HasColumnName("client_code");
            entity.Property(e => e.ClosingDate).HasColumnName("closing_date");
            entity.Property(e => e.CreationDate).HasColumnName("creation_date");
            entity.Property(e => e.OrderCode)
                .HasMaxLength(50)
                .HasColumnName("order_code");
            entity.Property(e => e.OrderTime).HasColumnName("order_time");
            entity.Property(e => e.RentalTime)
                .HasMaxLength(50)
                .HasColumnName("rental_time");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");

            entity.HasOne(d => d.ClientCodeNavigation).WithMany(p => p.Orders)
                .HasPrincipalKey(p => p.Code)
                .HasForeignKey(d => d.ClientCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_client");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("order_details");

            entity.Property(e => e.ClientCode)
                .HasMaxLength(8)
                .HasColumnName("client_code");
            entity.Property(e => e.ClientName)
                .HasMaxLength(255)
                .HasColumnName("client_name");
            entity.Property(e => e.ClosingDate).HasColumnName("closing_date");
            entity.Property(e => e.CreationDate).HasColumnName("creation_date");
            entity.Property(e => e.OrderCode)
                .HasMaxLength(50)
                .HasColumnName("order_code");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.OrderTime).HasColumnName("order_time");
            entity.Property(e => e.RentalTime)
                .HasMaxLength(50)
                .HasColumnName("rental_time");
            entity.Property(e => e.ServiceNames).HasColumnName("service_names");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.TotalCost).HasColumnName("total_cost");
        });

        modelBuilder.Entity<OrderService>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("order_services_pkey");

            entity.ToTable("order_services");

            entity.HasIndex(e => new { e.Id, e.ServiceId }, "unique_order_service").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderServices)
                .HasForeignKey(d => d.Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_order");

            entity.HasOne(d => d.Service).WithMany(p => p.OrderServices)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_service");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("product_pkey");

            entity.ToTable("product");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Articlenumber)
                .HasMaxLength(10)
                .HasColumnName("articlenumber");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Image)
                .HasMaxLength(100)
                .HasColumnName("image");
            entity.Property(e => e.Mincostforagent)
                .HasPrecision(10, 2)
                .HasColumnName("mincostforagent");
            entity.Property(e => e.Productionpersoncount).HasColumnName("productionpersoncount");
            entity.Property(e => e.Productionworkshopnumber).HasColumnName("productionworkshopnumber");
            entity.Property(e => e.Producttypeid).HasColumnName("producttypeid");
            entity.Property(e => e.Title)
                .HasMaxLength(100)
                .HasColumnName("title");

            entity.HasOne(d => d.Producttype).WithMany(p => p.Products)
                .HasForeignKey(d => d.Producttypeid)
                .HasConstraintName("fk_product_producttype");
        });

        modelBuilder.Entity<Productsale>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("productsale_pkey");

            entity.ToTable("productsale");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Agentid).HasColumnName("agentid");
            entity.Property(e => e.Productcount).HasColumnName("productcount");
            entity.Property(e => e.Productid).HasColumnName("productid");
            entity.Property(e => e.Saledate).HasColumnName("saledate");

            entity.HasOne(d => d.Agent).WithMany(p => p.Productsales)
                .HasForeignKey(d => d.Agentid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_productsale_agent");

            entity.HasOne(d => d.Product).WithMany(p => p.Productsales)
                .HasForeignKey(d => d.Productid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_productsale_product");
        });

        modelBuilder.Entity<Producttype>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("producttype_pkey");

            entity.ToTable("producttype");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Defectedpercent).HasColumnName("defectedpercent");
            entity.Property(e => e.Title)
                .HasMaxLength(50)
                .HasColumnName("title");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("services_pkey");

            entity.ToTable("services");

            entity.HasIndex(e => e.Code, "services_code_key").IsUnique();

            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .HasColumnName("code");
            entity.Property(e => e.CostPerHour)
                .HasPrecision(10, 2)
                .HasColumnName("cost_per_hour");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Login, "users_login_key").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.Login)
                .HasMaxLength(50)
                .HasColumnName("login");
            entity.Property(e => e.Password)
                .HasMaxLength(50)
                .HasColumnName("password");
            entity.Property(e => e.PhotoPath)
                .HasMaxLength(255)
                .HasColumnName("photo_path");
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .HasColumnName("role");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
