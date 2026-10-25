using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KikiAZZoo.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<CategoryGroup> CategoryGroups => Set<CategoryGroup>();
        public DbSet<PetType> PetTypes => Set<PetType>();
        public DbSet<Brand> Brands => Set<Brand>();
        public DbSet<ProductImage> ProductImages => Set<ProductImage>();
        public DbSet<ProductFeature> ProductFeatures => Set<ProductFeature>();
        public DbSet<Service> Services => Set<Service>();
        public DbSet<PriceRange> PriceRanges => Set<PriceRange>();
        public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
        public DbSet<PetServiceBooking> PetServiceBookings => Set<PetServiceBooking>();
        public DbSet<PetProfile> PetProfiles => Set<PetProfile>();
        public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
        public DbSet<Coupon> Coupons => Set<Coupon>();
        public DbSet<Breed> Breeds => Set<Breed>();
        public DbSet<Pet> Pets => Set<Pet>();
        public DbSet<UserCartItem> UserCartItems => Set<UserCartItem>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
            builder.Entity<Product>().Property(p => p.OriginalPrice).HasPrecision(18, 2);
            builder.Entity<Pet>().Property(p => p.Price).HasPrecision(18, 2);
            builder.Entity<Pet>().Property(p => p.OriginalPrice).HasPrecision(18, 2);
            builder.Entity<Order>().Property(o => o.TotalPrice).HasPrecision(18, 2);
            builder.Entity<Order>().Property(o => o.ShippingFee).HasPrecision(18, 2);
            builder.Entity<Order>().Property(o => o.DiscountAmount).HasPrecision(18, 2);
            builder.Entity<OrderDetail>().Property(d => d.Price).HasPrecision(18, 2);
            builder.Entity<Service>().Property(s => s.PriceFrom).HasPrecision(18, 2);
            builder.Entity<PriceRange>().Property(p => p.MinPrice).HasPrecision(18, 2);
            builder.Entity<PriceRange>().Property(p => p.MaxPrice).HasPrecision(18, 2);
            builder.Entity<Coupon>().Property(c => c.DiscountValue).HasPrecision(18, 2);
            builder.Entity<Coupon>().Property(c => c.MinOrderValue).HasPrecision(18, 2);
            builder.Entity<PetServiceBooking>().Property(b => b.Price).HasPrecision(18, 2);
        }
    }
}
