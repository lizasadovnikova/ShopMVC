using System;
using System.Collections.Generic;
using ShopDomain.Model;
using Microsoft.EntityFrameworkCore;

namespace ShopInfrastructure;

public partial class DbshopContext : DbContext
{
    public DbshopContext()
    {
    }

    public DbshopContext(DbContextOptions<DbshopContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Item> Items { get; set; }

    public virtual DbSet<OriginCountry> OriginCountries { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-8IE5BFQ\\SQLEXPRESS; Database=DBShop; Trusted_Connection=True; TrustServerCertificate=True; ");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("CategoryID");
            entity.Property(e => e.Description)
                .IsRequired(false)
                .HasMaxLength(50)
                .IsFixedLength();
            entity.Property(e => e.Name)
                .HasMaxLength(20)
                .IsFixedLength();
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.ToTable("Item");

            entity.Property(e => e.Id).HasColumnName("ItemID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CountryId).HasColumnName("CountryID");
            entity.Property(e => e.Description)
                .IsRequired(false)
                .HasMaxLength(50)
                .IsFixedLength();
            entity.Property(e => e.ImagePath)
            .IsRequired(false)
            .HasMaxLength(2048);
            entity.Property(e => e.Name)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Category).WithMany(p => p.Items)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Item_Categories1");

            entity.HasOne(d => d.Country).WithMany(p => p.Items)
                .HasForeignKey(d => d.CountryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Item_OriginCountry1");
        });

        modelBuilder.Entity<OriginCountry>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.ToTable("OriginCountry");

            entity.Property(e => e.Id).HasColumnName("CountryID");
            entity.Property(e => e.Name)
                .HasMaxLength(30)
                .IsFixedLength();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
