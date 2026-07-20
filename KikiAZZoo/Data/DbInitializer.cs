using KikiAZZoo.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KikiAZZoo.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            await context.Database.MigrateAsync();
            await SeedRolesAsync(roleManager);
            await SeedAdminAsync(userManager);

            if (!await context.CategoryGroups.AnyAsync())
                await SeedCatalogAsync(context);

            if (!await context.Coupons.AnyAsync())
            {
                context.Coupons.AddRange(
                    new Coupon { Code = "KIKI10", DiscountValue = 10, IsPercentage = true, MinOrderValue = 100000, StartDate = DateTime.Now.AddDays(-1), EndDate = DateTime.Now.AddDays(30), IsActive = true, MaxUsage = 100 },
                    new Coupon { Code = "SPALOVE", DiscountValue = 50000, IsPercentage = false, MinOrderValue = 200000, StartDate = DateTime.Now.AddDays(-1), EndDate = DateTime.Now.AddDays(30), IsActive = true, MaxUsage = 100 }
                );
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            foreach (var role in new[] { SD.Role_Admin, SD.Role_Customer, SD.Role_Employee, SD.Role_Company })
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        private static async Task SeedAdminAsync(UserManager<ApplicationUser> userManager)
        {
            var guest = await userManager.FindByNameAsync("guest@kikiazoo.com");
            if (guest == null)
            {
                guest = new ApplicationUser
                {
                    UserName = "guest@kikiazoo.com",
                    Email = "guest@kikiazoo.com",
                    FullName = "Khách vãng lai",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(guest, "Guest@123");
                await userManager.AddToRoleAsync(guest, SD.Role_Customer);
            }

            if (await userManager.Users.AnyAsync(u => u.UserName == "admin@kikiazoo.com")) return;

            var admin = new ApplicationUser
            {
                UserName = "admin@kikiazoo.com",
                Email = "admin@kikiazoo.com",
                FullName = "Quản trị viên",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(admin, "Admin@123");
            await userManager.AddToRoleAsync(admin, SD.Role_Admin);
        }

        private static async Task SeedCatalogAsync(ApplicationDbContext context)
        {
            var groups = new[]
            {
                new CategoryGroup { Name = "Thú cưng", Icon = "bi-heart", SortOrder = 1 },
                new CategoryGroup { Name = "Thức ăn", Icon = "bi-basket", SortOrder = 2 },
                new CategoryGroup { Name = "Đồ chơi", Icon = "bi-balloon", SortOrder = 3 },
                new CategoryGroup { Name = "Phụ kiện", Icon = "bi-tag", SortOrder = 4 },
                new CategoryGroup { Name = "Quần áo", Icon = "bi-handbag", SortOrder = 5 },
                new CategoryGroup { Name = "Chăm sóc", Icon = "bi-droplet", SortOrder = 6 }
            };
            context.CategoryGroups.AddRange(groups);
            await context.SaveChangesAsync();

            var petTypes = new[]
            {
                new PetType { Name = "Chó", Icon = "bi-paw-fill", SortOrder = 1 },
                new PetType { Name = "Mèo", Icon = "bi-paw-fill", SortOrder = 2 },
                new PetType { Name = "Chim", Icon = "bi-feather", SortOrder = 3 },
                new PetType { Name = "Hamster", Icon = "bi-circle-fill", SortOrder = 4 },
                new PetType { Name = "Thỏ", Icon = "bi-heart-fill", SortOrder = 5 }
            };
            context.PetTypes.AddRange(petTypes);
            await context.SaveChangesAsync();

            var priceRanges = new[]
            {
                new PriceRange { Label = "Dưới 100.000đ", MinPrice = 0, MaxPrice = 99999, SortOrder = 1 },
                new PriceRange { Label = "100.000đ - 300.000đ", MinPrice = 100000, MaxPrice = 300000, SortOrder = 2 },
                new PriceRange { Label = "300.000đ - 500.000đ", MinPrice = 300001, MaxPrice = 500000, SortOrder = 3 },
                new PriceRange { Label = "500.000đ - 1.000.000đ", MinPrice = 500001, MaxPrice = 1000000, SortOrder = 4 },
                new PriceRange { Label = "Trên 1.000.000đ", MinPrice = 1000001, MaxPrice = null, SortOrder = 5 }
            };
            context.PriceRanges.AddRange(priceRanges);

            var services = new[]
            {
                new Service { Name = "Spa & Chăm sóc", Description = "Tắm, sấy, cắt tỉa lông, vệ sinh tai móng chuyên nghiệp.", Icon = "bi-scissors", PriceFrom = 150000, SortOrder = 1 },
                new Service { Name = "Tắm rửa thú cưng", Description = "Tắm sạch với sữa tắm chuyên dụng, massage thư giãn.", Icon = "bi-droplet", PriceFrom = 80000, SortOrder = 2 },
                new Service { Name = "Khám sức khỏe", Description = "Kiểm tra tổng quát, tư vấn dinh dưỡng và tiêm phòng.", Icon = "bi-heart-pulse", PriceFrom = 200000, SortOrder = 3 },
                new Service { Name = "Nhận nuôi thú cưng", Description = "Kết nối thú cưng cần mái ấm với gia đình yêu thương.", Icon = "bi-house-heart", PriceFrom = 0, SortOrder = 4 },
                new Service { Name = "Huấn luyện cơ bản", Description = "Dạy quy tắc vệ sinh, xã hội hóa và lệnh cơ bản.", Icon = "bi-award", PriceFrom = 350000, SortOrder = 5 },
                new Service { Name = "Khách sạn thú cưng", Description = "Gửi thú cưng an toàn khi bạn đi vắng.", Icon = "bi-building", PriceFrom = 120000, SortOrder = 6 }
            };
            context.Services.AddRange(services);
            await context.SaveChangesAsync();

            var brands = new[]
            {
                new Brand { Name = "Royal Canin" },
                new Brand { Name = "Pedigree" },
                new Brand { Name = "Whiskas" },
                new Brand { Name = "SmartHeart" },
                new Brand { Name = "KikiAzoo" }
            };
            context.Brands.AddRange(brands);
            await context.SaveChangesAsync();

            var g = groups.ToDictionary(x => x.Name, x => x.Id);
            var p = petTypes.ToDictionary(x => x.Name, x => x.Id);

            var categories = new List<Category>
            {
                new() { Name = "Chó cảnh", Icon = "bi-emoji-smile", CategoryGroupId = g["Thú cưng"] },
                new() { Name = "Mèo cảnh", Icon = "bi-emoji-heart-eyes", CategoryGroupId = g["Thú cưng"] },
                new() { Name = "Chim cảnh", Icon = "bi-feather", CategoryGroupId = g["Thú cưng"] },
                new() { Name = "Thức ăn chó", Icon = "bi-basket", CategoryGroupId = g["Thức ăn"] },
                new() { Name = "Thức ăn mèo", Icon = "bi-basket2", CategoryGroupId = g["Thức ăn"] },
                new() { Name = "Đồ chơi cao su", Icon = "bi-circle", CategoryGroupId = g["Đồ chơi"] },
                new() { Name = "Đồ chơi bông", Icon = "bi-balloon", CategoryGroupId = g["Đồ chơi"] },
                new() { Name = "Vòng cổ & dây dắt", Icon = "bi-link", CategoryGroupId = g["Phụ kiện"] },
                new() { Name = "Lồng & nệm", Icon = "bi-house", CategoryGroupId = g["Phụ kiện"] },
                new() { Name = "Áo & áo khoác", Icon = "bi-handbag", CategoryGroupId = g["Quần áo"] },
                new() { Name = "Sữa tắm & dưỡng lông", Icon = "bi-droplet", CategoryGroupId = g["Chăm sóc"] },
                new() { Name = "Vitamin & thực phẩm bổ sung", Icon = "bi-capsule", CategoryGroupId = g["Chăm sóc"] }
            };
            context.Categories.AddRange(categories);
            await context.SaveChangesAsync();

            var c = categories.ToDictionary(x => x.Name, x => x.Id);

            var products = new List<Product>
            {
                new() { Name = "Chó Poodle mini (2 tháng)", Price = 5500000, OriginalPrice = 6000000, Description = "Chó Poodle thuần chủng, đã tiêm phòng 2 mũi, sổ theo dõi đầy đủ.", CategoryId = c["Chó cảnh"], PetTypeId = p["Chó"], BrandId = brands[4].Id, Rating = 4.9, ImageUrl = "/images/pet-dog.svg" },
                new() { Name = "Mèo Anh lông ngắn (3 tháng)", Price = 4800000, Description = "Mèo ALN xinh xắn, lanh lợi, đã tẩy giun và tiêm phòng.", CategoryId = c["Mèo cảnh"], PetTypeId = p["Mèo"], BrandId = brands[4].Id, Rating = 4.8, ImageUrl = "/images/pet-cat.svg" },
                new() { Name = "Royal Canin Mini Puppy 2kg", Price = 320000, OriginalPrice = 380000, Description = "Thức ăn khô cho chó con giống nhỏ.", Ingredients = "Gạo, bột thịt gia cầm...", UsageGuide = "Cho ăn 2-3 lần/ngày.", CategoryId = c["Thức ăn chó"], PetTypeId = p["Chó"], BrandId = brands[0].Id, Rating = 4.8, ImageUrl = "/images/product-food.svg" },
                new() { Name = "Whiskas Tuna 1.2kg", Price = 125000, Description = "Thức ăn mèo vị cá ngừ.", CategoryId = c["Thức ăn mèo"], PetTypeId = p["Mèo"], BrandId = brands[2].Id, Rating = 4.6, ImageUrl = "/images/product-food.svg" },
                new() { Name = "Bóng cao su chó size M", Price = 45000, Description = "Đồ chơi cao su bền, an toàn răng.", CategoryId = c["Đồ chơi cao su"], PetTypeId = p["Chó"], BrandId = brands[4].Id, Rating = 4.3, ImageUrl = "/images/product-toy.svg" },
                new() { Name = "Gấu bông squeaker", Price = 65000, Description = "Đồ chơi bông có tiếng kêu vui nhộn.", CategoryId = c["Đồ chơi bông"], PetTypeId = p["Chó"], BrandId = brands[4].Id, Rating = 4.5, ImageUrl = "/images/product-toy.svg" },
                new() { Name = "Vòng cổ da có chuông", Price = 89000, Description = "Vòng cổ da mềm cao cấp.", CategoryId = c["Vòng cổ & dây dắt"], PetTypeId = p["Chó"], BrandId = brands[4].Id, Rating = 4.7, ImageUrl = "/images/product-accessory.svg" },
                new() { Name = "Nệm lông cừu size L", Price = 250000, Description = "Nệm êm ấm cho chó mèo trung bình.", CategoryId = c["Lồng & nệm"], PetTypeId = p["Mèo"], BrandId = brands[4].Id, Rating = 4.6, ImageUrl = "/images/product-accessory.svg" },
                new() { Name = "Áo len ấm cho chó", Price = 99000, Description = "Áo len mềm giữ ấm mùa đông.", CategoryId = c["Áo & áo khoác"], PetTypeId = p["Chó"], BrandId = brands[4].Id, Rating = 4.2, ImageUrl = "/images/product-cloth.svg" },
                new() { Name = "Sữa tắm thảo mộc", Price = 156000, Description = "Sữa tắm dịu nhẹ chiết xuất thảo mộc.", CategoryId = c["Sữa tắm & dưỡng lông"], PetTypeId = p["Chó"], BrandId = brands[4].Id, Rating = 4.4, ImageUrl = "/images/product-care.svg" },
                new() { Name = "Vitamin tổng hợp 60 viên", Price = 185000, Description = "Bổ sung vitamin cho lông óng mượt.", CategoryId = c["Vitamin & thực phẩm bổ sung"], PetTypeId = p["Mèo"], BrandId = brands[4].Id, Rating = 4.5, ImageUrl = "/images/product-care.svg" },
                new() { Name = "Pedigree Adult 3kg", Price = 245000, OriginalPrice = 290000, Description = "Thức ăn cho chó trưởng thành.", CategoryId = c["Thức ăn chó"], PetTypeId = p["Chó"], BrandId = brands[1].Id, Rating = 4.5, ImageUrl = "/images/product-food.svg" },
                new() { Name = "Mèo Anh lông ngắn (5 tháng)", Price = 5200000, Description = "Mèo giàu năng lượng, thuần chủng, đã tiêm phòng cơ bản.", CategoryId = c["Mèo cảnh"], PetTypeId = p["Mèo"], BrandId = brands[4].Id, Rating = 4.7, ImageUrl = "/images/pet-cat.svg" },
                new() { Name = "Chim họa mi mini", Price = 900000, Description = "Chim họa mi tiếng hót trong và khỏe.", CategoryId = c["Chim cảnh"], PetTypeId = p["Chim"], BrandId = brands[4].Id, Rating = 4.6, ImageUrl = "/images/product-accessory.svg" },
                new() { Name = "Thức ăn hạt mèo vị cá 1.5kg", Price = 145000, Description = "Thức ăn khô dành cho mèo trưởng thành.", CategoryId = c["Thức ăn mèo"], PetTypeId = p["Mèo"], BrandId = brands[2].Id, Rating = 4.4, ImageUrl = "/images/product-food.svg" },
                new() { Name = "Vòng cao su đồ chơi chó", Price = 55000, Description = "Vòng cao su bền và an toàn cho chó.", CategoryId = c["Đồ chơi cao su"], PetTypeId = p["Chó"], BrandId = brands[4].Id, Rating = 4.3, ImageUrl = "/images/product-toy.svg" },
                new() { Name = "Búp bê bông mèo", Price = 72000, Description = "Đồ chơi bông mềm cho mèo.", CategoryId = c["Đồ chơi bông"], PetTypeId = p["Mèo"], BrandId = brands[4].Id, Rating = 4.6, ImageUrl = "/images/product-toy.svg" },
                new() { Name = "Dây dắt vải mềm", Price = 99000, Description = "Dây dắt êm ái cho chó và mèo.", CategoryId = c["Vòng cổ & dây dắt"], PetTypeId = p["Chó"], BrandId = brands[4].Id, Rating = 4.5, ImageUrl = "/images/product-accessory.svg" },
                new() { Name = "Lồng chim mini", Price = 210000, Description = "Lồng nhẹ, thông thoáng cho chim cảnh.", CategoryId = c["Lồng & nệm"], PetTypeId = p["Chim"], BrandId = brands[4].Id, Rating = 4.2, ImageUrl = "/images/product-accessory.svg" },
                new() { Name = "Áo hoodie mèo", Price = 119000, Description = "Áo len dễ thương cho mèo vào mùa đông.", CategoryId = c["Áo & áo khoác"], PetTypeId = p["Mèo"], BrandId = brands[4].Id, Rating = 4.3, ImageUrl = "/images/product-cloth.svg" },
                new() { Name = "Sữa tắm bạc hà", Price = 175000, Description = "Sữa tắm mát lạnh dành cho chó và mèo.", CategoryId = c["Sữa tắm & dưỡng lông"], PetTypeId = p["Chó"], BrandId = brands[4].Id, Rating = 4.4, ImageUrl = "/images/product-care.svg" },
                new() { Name = "Vitamin tăng đề kháng 30 viên", Price = 215000, Description = "Bổ sung vitamin cho sức khỏe thú cưng.", CategoryId = c["Vitamin & thực phẩm bổ sung"], PetTypeId = p["Chó"], BrandId = brands[4].Id, Rating = 4.5, ImageUrl = "/images/product-care.svg" }
            };
            context.Products.AddRange(products);
            await context.SaveChangesAsync();

            var features = new List<ProductFeature>();
            foreach (var prod in products)
            {
                features.AddRange(new[]
                {
                    new ProductFeature { ProductId = prod.Id, Icon = "bi-shield-check", Text = "Sản phẩm chính hãng", SortOrder = 1 },
                    new ProductFeature { ProductId = prod.Id, Icon = "bi-truck", Text = "Giao hàng toàn quốc", SortOrder = 2 },
                    new ProductFeature { ProductId = prod.Id, Icon = "bi-arrow-repeat", Text = "Đổi trả trong 7 ngày", SortOrder = 3 }
                });
                if (prod.CategoryId == c["Chó cảnh"] || prod.CategoryId == c["Mèo cảnh"])
                {
                    features.Add(new ProductFeature { ProductId = prod.Id, Icon = "bi-heart-pulse", Text = "Đã tiêm phòng & tẩy giun", SortOrder = 4 });
                    features.Add(new ProductFeature { ProductId = prod.Id, Icon = "bi-file-earmark-text", Text = "Giấy tờ đầy đủ", SortOrder = 5 });
                }
            }
            context.ProductFeatures.AddRange(features);

            context.BlogPosts.AddRange(
                new BlogPost { Title = "10 cách giữ thú cưng khỏe mạnh", Summary = "Mẹo chăm sóc hàng ngày.", Content = "Chăm sóc thú cưng đòi hỏi sự kiên nhẫn...", ImageUrl = "/images/news1.jpg", PublishedDate = DateTime.Now.AddDays(-2), IsFeatured = true },
                new BlogPost { Title = "Chọn thức ăn cho chó con", Summary = "Hướng dẫn theo độ tuổi.", Content = "Thức ăn là yếu tố quan trọng...", ImageUrl = "/images/news2.jpg", PublishedDate = DateTime.Now.AddDays(-5) },
                new BlogPost { Title = "Vaccine cần thiết cho mèo", Summary = "Lịch tiêm phòng đầy đủ.", Content = "Tiêm vaccine bảo vệ mèo...", ImageUrl = "/images/news3.jpg", PublishedDate = DateTime.Now.AddDays(-8) }
            );

            await context.SaveChangesAsync();
        }
    }
}
