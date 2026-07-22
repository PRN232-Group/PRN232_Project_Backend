using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Com.FPTU.Prn232SE1819.Api.Entity.Models;

public partial class InteriorStudioDbContext : DbContext
{
    public InteriorStudioDbContext(DbContextOptions<InteriorStudioDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductReview> ProductReviews { get; set; }

    public virtual DbSet<ProductSpec> ProductSpecs { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<EmailOtp> EmailOtps { get; set; }

    public virtual DbSet<AppPage> AppPages { get; set; }

    public virtual DbSet<RolePermission> RolePermissions { get; set; }

    public virtual DbSet<InteriorDesign> InteriorDesigns { get; set; }

    public virtual DbSet<InteriorDesignImage> InteriorDesignImages { get; set; }

    public virtual DbSet<InteriorDesignHighlight> InteriorDesignHighlights { get; set; }

    public virtual DbSet<InteriorDesignSpec> InteriorDesignSpecs { get; set; }

    public virtual DbSet<InteriorDesignMaterial> InteriorDesignMaterials { get; set; }

    public virtual DbSet<InteriorDesignPackage> InteriorDesignPackages { get; set; }

    public virtual DbSet<InteriorDesignProduct> InteriorDesignProducts { get; set; }

    public virtual DbSet<Content> Contents { get; set; }

    public virtual DbSet<QuotationRequest> QuotationRequests { get; set; }

    public virtual DbSet<QuotationRequestProduct> QuotationRequestProducts { get; set; }
public virtual DbSet<Quotation> Quotations { get; set; }

public virtual DbSet<QuotationProduct> QuotationProducts { get; set; }

public virtual DbSet<SystemLog> SystemLogs { get; set; }

public virtual DbSet<DesignRequest> DesignRequests { get; set; }
public virtual DbSet<DesignRequestProduct> DesignRequestProducts { get; set; }
public virtual DbSet<DesignRequestAttachment> DesignRequestAttachments { get; set; }
public virtual DbSet<ChatThread> ChatThreads { get; set; }
public virtual DbSet<ChatMessage> ChatMessages { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Carts__3214EC071057300C");

            entity.HasIndex(e => e.UserId, "UQ__Carts__1788CC4DFB3E964B").IsUnique();

            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.User).WithOne(p => p.Cart)
                .HasForeignKey<Cart>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Carts__UserId__4BAC3F29");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CartItem__3214EC071BE3A418");

            entity.HasIndex(e => e.ProductId, "IX_CartItems_ProductId");

            entity.HasIndex(e => new { e.CartId, e.ProductId }, "UQ_Cart_Product").IsUnique();

            entity.HasOne(d => d.Cart).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.CartId)
                .HasConstraintName("FK__CartItems__CartI__5070F446");

            entity.HasOne(d => d.Product).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CartItems__Produ__5165187F");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Categori__3214EC07ADA0CC0D");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(120);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Orders__3214EC074E08F314");

            entity.HasIndex(e => e.CreatedAt, "IX_Orders_CreatedAt").IsDescending();

            entity.HasIndex(e => e.CustomerId, "IX_Orders_Customer");

            entity.HasIndex(e => e.Status, "IX_Orders_Status");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CustomerEmail).HasMaxLength(256);
            entity.Property(e => e.CustomerName).HasMaxLength(150);
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.Phone).HasMaxLength(30);
            entity.Property(e => e.ShippingAddress).HasMaxLength(500);
            entity.Property(e => e.Status).HasMaxLength(30);
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Customer).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Orders__Customer__5535A963");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__OrderIte__3214EC07C97336F1");

            entity.HasIndex(e => e.OrderId, "IX_OrderItems_Order");

            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__OrderItem__Order__59063A47");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__OrderItem__Produ__59FA5E80");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Products__3214EC073DFE9077");

            entity.HasIndex(e => e.CategoryId, "IX_Products_Category");

            entity.HasIndex(e => e.IsActive, "IX_Products_IsActive").HasFilter("([IsDeleted]=(0))");

            entity.HasIndex(e => e.Name, "IX_Products_Name");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MarketPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__Products__Catego__3A81B327");
        });

        modelBuilder.Entity<ProductReview>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ProductR__3214EC07B32F3603");

            entity.HasIndex(e => new { e.ProductId, e.UserId }, "UQ_Review").IsUnique();

            entity.Property(e => e.Comment).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductReviews)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ProductRe__Produ__5EBF139D");

            entity.HasOne(d => d.User).WithMany(p => p.ProductReviews)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ProductRe__UserI__5FB337D6");
        });

        modelBuilder.Entity<ProductSpec>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__ProductS__B40CC6CD30B56513");

            entity.Property(e => e.ProductId).ValueGeneratedNever();
            entity.Property(e => e.Dimensions).HasMaxLength(120);
            entity.Property(e => e.Finish).HasMaxLength(120);
            entity.Property(e => e.Material).HasMaxLength(200);
            entity.Property(e => e.Origin).HasMaxLength(100);
            entity.Property(e => e.WeightKg).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Product).WithOne(p => p.ProductSpec)
                .HasForeignKey<ProductSpec>(d => d.ProductId)
                .HasConstraintName("FK__ProductSp__Produ__440B1D61");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Roles__3214EC072C0253D7");

            entity.HasIndex(e => e.Name, "UQ__Roles__737584F6FD05A41C").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC0768BC2D98");

            entity.HasIndex(e => e.IsLocked, "IX_Users_IsLocked");

            entity.HasIndex(e => e.RoleId, "IX_Users_RoleId");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534FB6ACF4B").IsUnique();

            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash).HasMaxLength(512);
            entity.Property(e => e.Phone).HasMaxLength(30);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Users__RoleId__29572725");
        });

        modelBuilder.Entity<AppPage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("AppPages");
            entity.HasIndex(e => e.PageKey).IsUnique();
            entity.Property(e => e.PageKey).HasMaxLength(120);
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.Section).HasMaxLength(30);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<SystemLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("SystemLogs");
            entity.HasIndex(e => e.CreatedAt, "IX_SystemLogs_CreatedAt");
            entity.HasIndex(e => e.Action, "IX_SystemLogs_Action");
            entity.HasIndex(e => e.Entity, "IX_SystemLogs_Entity");
            entity.Property(e => e.Action).HasMaxLength(80);
            entity.Property(e => e.Entity).HasMaxLength(80);
            entity.Property(e => e.EntityId).HasMaxLength(80);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.PageId });
            entity.ToTable("RolePermissions");
            entity.HasOne(d => d.Role).WithMany(p => p.RolePermissions)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.Page).WithMany(p => p.RolePermissions)
                .HasForeignKey(d => d.PageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmailOtp>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("EmailOtps");
            entity.HasIndex(e => new { e.Email, e.Purpose }, "IX_EmailOtps_Email_Purpose");
            entity.HasIndex(e => e.ResetToken, "IX_EmailOtps_ResetToken");
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.Purpose).HasMaxLength(30);
            entity.Property(e => e.Otp).HasMaxLength(10);
            entity.Property(e => e.ResetToken).HasMaxLength(128);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
        });

        modelBuilder.Entity<InteriorDesign>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("InteriorDesigns");
            entity.HasIndex(e => e.Category, "IX_InteriorDesigns_Category");
            entity.HasIndex(e => e.IsPublished, "IX_InteriorDesigns_IsPublished");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.Style).HasMaxLength(100);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.AreaSqm).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.BudgetFrom).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.BudgetTo).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.StudioPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MarketAvgPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.IsPublished).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<InteriorDesignImage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("InteriorDesignImages");
            entity.Property(e => e.Url).HasMaxLength(500);
            entity.HasOne(d => d.InteriorDesign).WithMany(p => p.InteriorDesignImages)
                .HasForeignKey(d => d.InteriorDesignId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InteriorDesignHighlight>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("InteriorDesignHighlights");
            entity.Property(e => e.Text).HasMaxLength(500);
            entity.HasOne(d => d.InteriorDesign).WithMany(p => p.InteriorDesignHighlights)
                .HasForeignKey(d => d.InteriorDesignId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InteriorDesignSpec>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("InteriorDesignSpecs");
            entity.Property(e => e.Label).HasMaxLength(120);
            entity.Property(e => e.Value).HasMaxLength(300);
            entity.HasOne(d => d.InteriorDesign).WithMany(p => p.InteriorDesignSpecs)
                .HasForeignKey(d => d.InteriorDesignId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InteriorDesignMaterial>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("InteriorDesignMaterials");
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.Origin).HasMaxLength(100);
            entity.Property(e => e.Finish).HasMaxLength(150);
            entity.Property(e => e.Care).HasMaxLength(300);
            entity.HasOne(d => d.InteriorDesign).WithMany(p => p.InteriorDesignMaterials)
                .HasForeignKey(d => d.InteriorDesignId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InteriorDesignPackage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("InteriorDesignPackages");
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Includes).HasMaxLength(500);
            entity.HasOne(d => d.InteriorDesign).WithMany(p => p.InteriorDesignPackages)
                .HasForeignKey(d => d.InteriorDesignId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InteriorDesignProduct>(entity =>
        {
            entity.HasKey(e => new { e.InteriorDesignId, e.ProductId });
            entity.ToTable("InteriorDesignProducts");
            entity.HasOne(d => d.InteriorDesign).WithMany(p => p.InteriorDesignProducts)
                .HasForeignKey(d => d.InteriorDesignId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.Product).WithMany(p => p.InteriorDesignProducts)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Content>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Contents");
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Title).HasMaxLength(250);
            entity.Property(e => e.Slug).HasMaxLength(250);
            entity.Property(e => e.Type).HasMaxLength(50);
            entity.Property(e => e.CoverUrl).HasMaxLength(500);
            entity.Property(e => e.IsPublished).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<QuotationRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("QuotationRequests");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Status).HasMaxLength(30);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.HasOne(d => d.Customer).WithMany()
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne(d => d.HandledBy).WithMany()
                .HasForeignKey(d => d.HandledById)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<QuotationRequestProduct>(entity =>
        {
            entity.HasKey(e => new { e.QuotationRequestId, e.ProductId });
            entity.ToTable("QuotationRequestProducts");
            entity.Property(e => e.Quantity).HasDefaultValue(1);
            entity.HasOne(d => d.QuotationRequest).WithMany(p => p.QuotationRequestProducts)
                .HasForeignKey(d => d.QuotationRequestId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.Product).WithMany()
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Quotation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Quotations");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Status).HasMaxLength(30);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.HasOne(d => d.QuotationRequest).WithMany(p => p.Quotations)
                .HasForeignKey(d => d.QuotationRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne(d => d.Customer).WithMany()
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne(d => d.CreatedBy).WithMany()
                .HasForeignKey(d => d.CreatedById)
                .OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne(d => d.ApprovedBy).WithMany()
                .HasForeignKey(d => d.ApprovedById)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<QuotationProduct>(entity =>
        {
            entity.HasKey(e => new { e.QuotationId, e.ProductId });
            entity.ToTable("QuotationProducts");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");
            entity.HasOne(d => d.Quotation).WithMany(p => p.QuotationProducts)
                .HasForeignKey(d => d.QuotationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.Product).WithMany()
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });
        modelBuilder.Entity<DesignRequestProduct>()
            .HasKey(dp => new { dp.DesignRequestId, dp.ProductId });
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

}
