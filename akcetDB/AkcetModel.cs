namespace akcetDB
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class AkcetModel : DbContext
    {
        public AkcetModel()
            : base("DefaultConnection")
        {
        }

        public virtual DbSet<Address> Addresses { get; set; }
        public virtual DbSet<Company> Companies { get; set; }
        public virtual DbSet<DD> DDS { get; set; }
        public virtual DbSet<Fakturi> Fakturis { get; set; }
        public virtual DbSet<FakturiTemp> FakturiTemps { get; set; }
        public virtual DbSet<ProductInvoice> ProductInvoices { get; set; }
        public virtual DbSet<ProductInvoiceTemp> ProductInvoiceTemps { get; set; }
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<Project> Projects { get; set; }
        public virtual DbSet<Counter> Counters { get; set; }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Address>()
                .HasMany(e => e.Companies)
                .WithRequired(e => e.Address)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Company>()
                .HasMany(e => e.Fakturis)
                .WithRequired(e => e.Company)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<DD>()
                .HasMany(e => e.ProductInvoices)
                .WithRequired(e => e.DD)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Fakturi>()
                .HasMany(e => e.ProductInvoices)
                .WithRequired(e => e.Fakturi)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<FakturiTemp>()
                .HasMany(e => e.ProductInvoiceTemps)
                .WithRequired(e => e.FakturiTemp)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Product>()
                .HasMany(e => e.ProductInvoices)
                .WithRequired(e => e.Product)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Project>()
                .HasMany(e => e.ProductInvoices)
                .WithRequired(e => e.Project)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Project>()
                .HasMany(e => e.ProductInvoiceTemps)
                .WithRequired(e => e.Project)
                .WillCascadeOnDelete(false);
        }
    }
}
