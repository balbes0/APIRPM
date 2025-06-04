using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace APIRPM.Models;

public partial class Rpm2testContext : DbContext
{
    public Rpm2testContext()
    {
    }

    public Rpm2testContext(DbContextOptions<Rpm2testContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<Catalog> Catalogs { get; set; }

    public virtual DbSet<Delivery> Deliveries { get; set; }

    public virtual DbSet<DeliveryStatus> DeliveryStatuses { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }

    public virtual DbSet<PaymentStatus> PaymentStatuses { get; set; }

    public virtual DbSet<PosOrder> PosOrders { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<StatusOrder> StatusOrders { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.ProductId });  // Составной ключ из UserId и ProductId

            entity.ToTable("Cart");

            entity.Property(e => e.ProductId).HasColumnName("Product_ID");
            entity.Property(e => e.Quantity).HasDefaultValue(1);
            entity.Property(e => e.UserId).HasColumnName("User_ID");

            entity.HasOne(d => d.Product).WithMany()
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Cart__Product_ID__59063A47");

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Cart__User_ID__5812160E");
        });

        modelBuilder.Entity<Catalog>(entity =>
        {
            entity.HasKey(e => e.IdProduct).HasName("PK__Catalog__522DE49676AF4F50");

            entity.ToTable("Catalog");

            entity.Property(e => e.IdProduct).HasColumnName("ID_Product");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("Category_name");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.PathToImage)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasDefaultValue("нет картинка началника");
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ProductName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Product_name");
            entity.Property(e => e.Stock).HasDefaultValue(0);
        });

        modelBuilder.Entity<Delivery>(entity =>
        {
            entity.HasKey(e => e.IdDelivery).HasName("PK__Delivery__02E3B7D0D02C23A5");

            entity.ToTable("Delivery");

            entity.Property(e => e.IdDelivery).HasColumnName("ID_Delivery");
            entity.Property(e => e.DeliveryAddress)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Delivery_address");
            entity.Property(e => e.DeliveryDate).HasColumnName("Delivery_date");
            entity.Property(e => e.DeliveryStatusId).HasColumnName("Delivery_status_ID");
            entity.Property(e => e.OrderId).HasColumnName("Order_ID");

            entity.HasOne(d => d.DeliveryStatus).WithMany(p => p.Deliveries)
                .HasForeignKey(d => d.DeliveryStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Delivery__Delive__693CA210");

            entity.HasOne(d => d.Order).WithMany(p => p.Deliveries)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Delivery__Order___68487DD7");
        });

        modelBuilder.Entity<DeliveryStatus>(entity =>
        {
            entity.HasKey(e => e.IdDeliveryStatus).HasName("PK__Delivery__DD9936FC2A73652A");

            entity.ToTable("Delivery_status");

            entity.Property(e => e.IdDeliveryStatus).HasColumnName("ID_Delivery_status");
            entity.Property(e => e.DeliveryStatusName)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("Delivery_status_name");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.IdOrder).HasName("PK__Orders__EC9FA955ACB69891");

            entity.Property(e => e.IdOrder).HasColumnName("ID_Order");
            entity.Property(e => e.DeliveryDate).HasColumnName("Delivery_date");
            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("Order_date");
            entity.Property(e => e.StatusId).HasColumnName("Status_ID");
            entity.Property(e => e.TotalSum)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("Total_sum");
            entity.Property(e => e.UserId).HasColumnName("User_ID");

            entity.HasOne(d => d.Status).WithMany(p => p.Orders)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Orders__Status_I__52593CB8");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Orders__User_ID__5070F446");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.IdPayment).HasName("PK__Payment__C2118ADE19577524");

            entity.ToTable("Payment");

            entity.Property(e => e.IdPayment).HasColumnName("ID_Payment");
            entity.Property(e => e.Amount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.OrderId).HasColumnName("Order_ID");
            entity.Property(e => e.PaymentDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("Payment_date");
            entity.Property(e => e.PaymentMethodId).HasColumnName("Payment_method_ID");
            entity.Property(e => e.PaymentStatusId).HasColumnName("Payment_status_ID");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payment__Order_I__60A75C0F");

            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.Payments)
                .HasForeignKey(d => d.PaymentMethodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payment__Payment__628FA481");

            entity.HasOne(d => d.PaymentStatus).WithMany(p => p.Payments)
                .HasForeignKey(d => d.PaymentStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payment__Payment__6383C8BA");
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasKey(e => e.IdPaymentMethod).HasName("PK__Payment___863AB40792237197");

            entity.ToTable("Payment_methods");

            entity.Property(e => e.IdPaymentMethod).HasColumnName("ID_Payment_method");
            entity.Property(e => e.PaymentMethodName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("Payment_method_name");
        });

        modelBuilder.Entity<PaymentStatus>(entity =>
        {
            entity.HasKey(e => e.IdPaymentStatus).HasName("PK__Payment___5790CA4BF385664F");

            entity.ToTable("Payment_status");

            entity.Property(e => e.IdPaymentStatus).HasColumnName("ID_Payment_status");
            entity.Property(e => e.PaymentStatusName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("Payment_status_name");
        });

        modelBuilder.Entity<PosOrder>(entity =>
        {
            entity.HasKey(e => e.IdPos).HasName("PK__PosOrder__20AED5AF489C40EB");

            entity.ToTable("PosOrder");

            entity.Property(e => e.IdPos).HasColumnName("ID_Pos");
            entity.Property(e => e.OrderId).HasColumnName("Order_ID");
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ProductId).HasColumnName("Product_ID");

            entity.HasOne(d => d.Order).WithMany(p => p.PosOrders)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PosOrder__Order___5535A963");

            entity.HasOne(d => d.Product).WithMany(p => p.PosOrders)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PosOrder__Produc__5629CD9C");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.IdReview).HasName("PK__Reviews__E39E9647EFEE706B");

            entity.Property(e => e.IdReview).HasColumnName("ID_Review");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ProductId).HasColumnName("Product_ID");
            entity.Property(e => e.ReviewText).HasMaxLength(1000);
            entity.Property(e => e.UserId).HasColumnName("User_ID");

            entity.HasOne(d => d.Product).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reviews__Product__4BAC3F29");

            entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reviews__User_ID__4AB81AF0");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.IdRole).HasName("PK__Roles__43DCD32D81D0D528");

            entity.Property(e => e.IdRole).HasColumnName("ID_Role");
            entity.Property(e => e.RoleName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("Role_name");
        });

        modelBuilder.Entity<StatusOrder>(entity =>
        {
            entity.HasKey(e => e.IdStatus).HasName("PK__Status_o__5AC2A73403B053BF");

            entity.ToTable("Status_order");

            entity.Property(e => e.IdStatus).HasColumnName("ID_Status");
            entity.Property(e => e.StatusName)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.IdUser).HasName("PK__Users__ED4DE442BDF59D26");

            entity.HasIndex(e => e.Phone, "UQ__Users__5C7E359EB52AC663").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534250CA1E8").IsUnique();

            entity.Property(e => e.IdUser).HasColumnName("ID_User");
            entity.Property(e => e.Address)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FirstName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("Unknown");
            entity.Property(e => e.LastName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("Unknown");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Phone)
                .HasMaxLength(11)
                .IsUnicode(false);
            entity.Property(e => e.RegistrationDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("Registration_date");
            entity.Property(e => e.RoleId).HasColumnName("Role_ID");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Users__Role_ID__3C69FB99");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
