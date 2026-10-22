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

            // Clean up any legacy "Thú cưng" CategoryGroup/Categories from Products table if present
            await CleanUpLegacyPetsFromProductsAsync(context);

            if (!await context.CategoryGroups.AnyAsync(g => g.Name != "Thú cưng"))
                await SeedCatalogAsync(context);

            if (!await context.Breeds.AnyAsync())
                await SeedPetsAndBreedsAsync(context);

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

        private static async Task CleanUpLegacyPetsFromProductsAsync(ApplicationDbContext context)
        {
            try
            {
                var legacyGroup = await context.CategoryGroups
                    .Include(g => g.Categories)
                    .FirstOrDefaultAsync(g => g.Name == "Thú cưng" || g.Id == 1);

                if (legacyGroup != null && legacyGroup.Name == "Thú cưng")
                {
                    var catIds = legacyGroup.Categories?.Select(c => c.Id).ToList() ?? new List<int>();
                    if (catIds.Any())
                    {
                        var legacyProducts = await context.Products
                            .Where(p => catIds.Contains(p.CategoryId))
                            .Include(p => p.Features)
                            .Include(p => p.Images)
                            .ToListAsync();

                        if (legacyProducts.Any())
                        {
                            foreach (var p in legacyProducts)
                            {
                                if (p.Features != null && p.Features.Any()) context.ProductFeatures.RemoveRange(p.Features);
                                if (p.Images != null && p.Images.Any()) context.ProductImages.RemoveRange(p.Images);
                            }
                            context.Products.RemoveRange(legacyProducts);
                        }

                        var legacyCats = await context.Categories.Where(c => catIds.Contains(c.Id)).ToListAsync();
                        context.Categories.RemoveRange(legacyCats);
                    }
                    context.CategoryGroups.Remove(legacyGroup);
                    await context.SaveChangesAsync();
                }
            }
            catch
            {
                // Soft ignore in case of migration constraint variations
            }
        }

        private static async Task SeedCatalogAsync(ApplicationDbContext context)
        {
            var groups = new[]
            {
                new CategoryGroup { Name = "Thức ăn", Icon = "bi-basket", SortOrder = 1 },
                new CategoryGroup { Name = "Đồ chơi", Icon = "bi-balloon", SortOrder = 2 },
                new CategoryGroup { Name = "Phụ kiện", Icon = "bi-tag", SortOrder = 3 },
                new CategoryGroup { Name = "Quần áo", Icon = "bi-handbag", SortOrder = 4 },
                new CategoryGroup { Name = "Chăm sóc", Icon = "bi-droplet", SortOrder = 5 }
            };
            context.CategoryGroups.AddRange(groups);
            await context.SaveChangesAsync();

            var petTypes = new[]
            {
                new PetType { Name = "Chó", Icon = "bi-paw-fill", SortOrder = 1 },
                new PetType { Name = "Mèo", Icon = "bi-paw-fill", SortOrder = 2 },
                new PetType { Name = "Chim", Icon = "bi-feather", SortOrder = 3 },
                new PetType { Name = "Hamster", Icon = "bi-circle-fill", SortOrder = 4 },
                new PetType { Name = "Cá cảnh", Icon = "bi-water", SortOrder = 5 },
                new PetType { Name = "Thỏ", Icon = "bi-heart-fill", SortOrder = 6 }
            };
            foreach (var pt in petTypes)
            {
                if (!await context.PetTypes.AnyAsync(p => p.Name == pt.Name))
                {
                    context.PetTypes.Add(pt);
                }
            }
            await context.SaveChangesAsync();

            var priceRanges = new[]
            {
                new PriceRange { Label = "Dưới 100.000đ", MinPrice = 0, MaxPrice = 99999, SortOrder = 1 },
                new PriceRange { Label = "100.000đ - 300.000đ", MinPrice = 100000, MaxPrice = 300000, SortOrder = 2 },
                new PriceRange { Label = "300.000đ - 500.000đ", MinPrice = 300001, MaxPrice = 500000, SortOrder = 3 },
                new PriceRange { Label = "500.000đ - 1.000.000đ", MinPrice = 500001, MaxPrice = 1000000, SortOrder = 4 },
                new PriceRange { Label = "Trên 1.000.000đ", MinPrice = 1000001, MaxPrice = null, SortOrder = 5 }
            };
            if (!await context.PriceRanges.AnyAsync())
            {
                context.PriceRanges.AddRange(priceRanges);
                await context.SaveChangesAsync();
            }

            var services = new[]
            {
                new Service { Name = "Spa & Chăm sóc", Description = "Tắm, sấy, cắt tỉa lông, vệ sinh tai móng chuyên nghiệp.", Icon = "bi-scissors", PriceFrom = 150000, SortOrder = 1 },
                new Service { Name = "Tắm rửa thú cưng", Description = "Tắm sạch với sữa tắm chuyên dụng, massage thư giãn.", Icon = "bi-droplet", PriceFrom = 80000, SortOrder = 2 },
                new Service { Name = "Khám sức khỏe", Description = "Kiểm tra tổng quát, tư vấn dinh dưỡng và tiêm phòng.", Icon = "bi-heart-pulse", PriceFrom = 200000, SortOrder = 3 },
                new Service { Name = "Nhận nuôi thú cưng", Description = "Kết nối thú cưng cần mái ấm với gia đình yêu thương.", Icon = "bi-house-heart", PriceFrom = 0, SortOrder = 4 },
                new Service { Name = "Huấn luyện cơ bản", Description = "Dạy quy tắc vệ sinh, xã hội hóa và lệnh cơ bản.", Icon = "bi-award", PriceFrom = 350000, SortOrder = 5 },
                new Service { Name = "Khách sạn thú cưng", Description = "Gửi thú cưng an toàn khi bạn đi vắng.", Icon = "bi-building", PriceFrom = 120000, SortOrder = 6 }
            };
            if (!await context.Services.AnyAsync())
            {
                context.Services.AddRange(services);
                await context.SaveChangesAsync();
            }

            var brands = new[]
            {
                new Brand { Name = "Royal Canin" },
                new Brand { Name = "Pedigree" },
                new Brand { Name = "Whiskas" },
                new Brand { Name = "SmartHeart" },
                new Brand { Name = "KikiAzoo" }
            };
            foreach (var b in brands)
            {
                if (!await context.Brands.AnyAsync(x => x.Name == b.Name))
                {
                    context.Brands.Add(b);
                }
            }
            await context.SaveChangesAsync();

            var g = await context.CategoryGroups.ToDictionaryAsync(x => x.Name, x => x.Id);
            var p = await context.PetTypes.ToDictionaryAsync(x => x.Name, x => x.Id);
            var bDict = await context.Brands.ToDictionaryAsync(x => x.Name, x => x.Id);

            var categories = new List<Category>
            {
                new() { Name = "Thức ăn chó", Icon = "bi-basket", CategoryGroupId = g["Thức ăn"], SortOrder = 1 },
                new() { Name = "Thức ăn mèo", Icon = "bi-basket2", CategoryGroupId = g["Thức ăn"], SortOrder = 2 },
                new() { Name = "Thức ăn chim & cá", Icon = "bi-flower1", CategoryGroupId = g["Thức ăn"], SortOrder = 3 },
                new() { Name = "Đồ chơi cao su", Icon = "bi-circle", CategoryGroupId = g["Đồ chơi"], SortOrder = 1 },
                new() { Name = "Đồ chơi bông & cào móng", Icon = "bi-balloon", CategoryGroupId = g["Đồ chơi"], SortOrder = 2 },
                new() { Name = "Vòng cổ & dây dắt", Icon = "bi-link", CategoryGroupId = g["Phụ kiện"], SortOrder = 1 },
                new() { Name = "Lồng, chuồng & nệm", Icon = "bi-house", CategoryGroupId = g["Phụ kiện"], SortOrder = 2 },
                new() { Name = "Bát ăn & bình nước", Icon = "bi-cup-straw", CategoryGroupId = g["Phụ kiện"], SortOrder = 3 },
                new() { Name = "Áo & áo khoác", Icon = "bi-handbag", CategoryGroupId = g["Quần áo"], SortOrder = 1 },
                new() { Name = "Sữa tắm & dưỡng lông", Icon = "bi-droplet", CategoryGroupId = g["Chăm sóc"], SortOrder = 1 },
                new() { Name = "Vitamin & thực phẩm bổ sung", Icon = "bi-capsule", CategoryGroupId = g["Chăm sóc"], SortOrder = 2 },
                new() { Name = "Cát vệ sinh & khay", Icon = "bi-box-seam", CategoryGroupId = g["Chăm sóc"], SortOrder = 3 }
            };
            context.Categories.AddRange(categories);
            await context.SaveChangesAsync();

            var c = await context.Categories.ToDictionaryAsync(x => x.Name, x => x.Id);

            var products = new List<Product>
            {
                new() { Name = "Royal Canin Mini Puppy 2kg", Price = 320000, OriginalPrice = 380000, Description = "Thức ăn khô cao cấp cho chó con giống nhỏ (dưới 10kg), hỗ trợ hệ tiêu hóa và miễn dịch.", Ingredients = "Gạo, bột thịt gia cầm, protein thực vật cô lập...", UsageGuide = "Cho ăn 2-3 lần/ngày theo cân nặng.", CategoryId = c["Thức ăn chó"], PetTypeId = p["Chó"], BrandId = bDict["Royal Canin"], Rating = 4.8, SalesCount = 65, ImageUrl = "/images/product-food.svg", Stock = 25 },
                new() { Name = "Pedigree Adult vị Bò & Rau Củ 3kg", Price = 245000, OriginalPrice = 290000, Description = "Thức ăn hạt dinh dưỡng cân bằng cho chó trưởng thành, bổ sung Omega 6 và Kẽm cho lông bóng mượt.", CategoryId = c["Thức ăn chó"], PetTypeId = p["Chó"], BrandId = bDict["Pedigree"], Rating = 4.5, SalesCount = 52, ImageUrl = "/images/product-food.svg", Stock = 30 },
                new() { Name = "Whiskas Tuna Vị Cá Ngừ 1.2kg", Price = 125000, OriginalPrice = 145000, Description = "Thức ăn hạt thơm ngon vị cá ngừ đại dương cho mèo lớn, giàu vitamin A và Taurine.", CategoryId = c["Thức ăn mèo"], PetTypeId = p["Mèo"], BrandId = bDict["Whiskas"], Rating = 4.7, SalesCount = 88, ImageUrl = "/images/product-food.svg", Stock = 40 },
                new() { Name = "Pate Mèo Royal Canin Kitten Instinctive 85g", Price = 38000, Description = "Pate mềm mịn dinh dưỡng cao cấp dành cho mèo con từ 4 đến 12 tháng tuổi.", Ingredients = "Thịt và dẫn xuất từ thịt, ngũ cốc, chiết xuất protein...", CategoryId = c["Thức ăn mèo"], PetTypeId = p["Mèo"], BrandId = bDict["Royal Canin"], Rating = 4.9, SalesCount = 120, ImageUrl = "/images/product-food.svg", Stock = 50 },
                new() { Name = "Bóng cao su huấn luyện chó chống cắn phá", Price = 45000, OriginalPrice = 60000, Description = "Đồ chơi cao su tự nhiên siêu bền, có gờ massage làm sạch răng miệng khi cắn.", CategoryId = c["Đồ chơi cao su"], PetTypeId = p["Chó"], BrandId = bDict["KikiAzoo"], Rating = 4.3, SalesCount = 34, ImageUrl = "/images/product-toy.svg", Stock = 20 },
                new() { Name = "Trụ cào móng cây xương rồng cho mèo", Price = 210000, OriginalPrice = 250000, Description = "Cây cào móng quấn dây thừng sisal tự nhiên giúp mèo xả stress và tránh cào sofa.", CategoryId = c["Đồ chơi bông & cào móng"], PetTypeId = p["Mèo"], BrandId = bDict["KikiAzoo"], Rating = 4.8, SalesCount = 45, ImageUrl = "/images/product-toy.svg", Stock = 15 },
                new() { Name = "Vòng cổ da kèm chuông lục lạc", Price = 89000, Description = "Vòng cổ da PU cao cấp mềm mại, điều chỉnh kích cỡ linh hoạt cho chó mèo nhỏ.", CategoryId = c["Vòng cổ & dây dắt"], PetTypeId = p["Chó"], BrandId = bDict["KikiAzoo"], Rating = 4.6, SalesCount = 28, ImageUrl = "/images/product-accessory.svg", Stock = 25 },
                new() { Name = "Dây dắt trợ lực tự cuốn 5m KikiAzoo", Price = 135000, OriginalPrice = 165000, Description = "Dây dắt tự co giãn với nút khóa an toàn một chạm, chịu tải lên đến 20kg.", CategoryId = c["Vòng cổ & dây dắt"], PetTypeId = p["Chó"], BrandId = bDict["KikiAzoo"], Rating = 4.7, SalesCount = 39, ImageUrl = "/images/product-accessory.svg", Stock = 18 },
                new() { Name = "Nệm lông cừu êm ái tròn size L", Price = 250000, OriginalPrice = 290000, Description = "Đệm nằm êm ấm bọc vải nhung mềm mại, ruột bông gòn tinh khiết đàn hồi tốt.", CategoryId = c["Lồng, chuồng & nệm"], PetTypeId = p["Mèo"], BrandId = bDict["KikiAzoo"], Rating = 4.8, SalesCount = 42, ImageUrl = "/images/product-accessory.svg", Stock = 12 },
                new() { Name = "Bát ăn đôi inox nghiêng 15 độ chống gù", Price = 115000, Description = "Bát đôi inox chống gỉ kèm chân đế nhựa dốc 15 độ bảo vệ cột sống cổ cho thú cưng.", CategoryId = c["Bát ăn & bình nước"], PetTypeId = p["Mèo"], BrandId = bDict["KikiAzoo"], Rating = 4.5, SalesCount = 31, ImageUrl = "/images/product-accessory.svg", Stock = 30 },
                new() { Name = "Áo len ấm mùa đông họa tiết tuần lộc", Price = 99000, Description = "Áo len co giãn dệt kim giữ nhiệt tối đa cho cún cưng và mèo vào mùa đông se lạnh.", CategoryId = c["Áo & áo khoác"], PetTypeId = p["Chó"], BrandId = bDict["KikiAzoo"], Rating = 4.2, SalesCount = 20, ImageUrl = "/images/product-cloth.svg", Stock = 22 },
                new() { Name = "Sữa tắm thảo mộc mượt lông KikiCare 500ml", Price = 156000, OriginalPrice = 180000, Description = "Sữa tắm chiết xuất tràm trà và dầu dừa tự nhiên, kháng khuẩn, khử mùi hôi hiệu quả.", CategoryId = c["Sữa tắm & dưỡng lông"], PetTypeId = p["Chó"], BrandId = bDict["KikiAzoo"], Rating = 4.6, SalesCount = 55, ImageUrl = "/images/product-care.svg", Stock = 28 },
                new() { Name = "Vitamin tổng hợp Bio-NutriPet 60 viên", Price = 185000, Description = "Bổ sung đầy đủ vitamin A, D3, E, B-complex giúp tăng cường đề kháng và dưỡng lông óng mượt.", CategoryId = c["Vitamin & thực phẩm bổ sung"], PetTypeId = p["Chó"], BrandId = bDict["KikiAzoo"], Rating = 4.8, SalesCount = 60, ImageUrl = "/images/product-care.svg", Stock = 35 },
                new() { Name = "Cát vệ sinh đậu nành Cature Tofu 6L", Price = 145000, Description = "Cát vệ sinh tự nhiên từ bã đậu nành, vón cục cực nhanh, xả được trực tiếp vào bồn cầu.", CategoryId = c["Cát vệ sinh & khay"], PetTypeId = p["Mèo"], BrandId = bDict["KikiAzoo"], Rating = 4.9, SalesCount = 140, ImageUrl = "/images/product-care.svg", Stock = 50 }
            };
            context.Products.AddRange(products);
            await context.SaveChangesAsync();

            var features = new List<ProductFeature>();
            foreach (var prod in products)
            {
                features.AddRange(new[]
                {
                    new ProductFeature { ProductId = prod.Id, Icon = "bi-shield-check", Text = "Sản phẩm chính hãng 100%", SortOrder = 1 },
                    new ProductFeature { ProductId = prod.Id, Icon = "bi-truck", Text = "Giao hàng toàn quốc siêu tốc", SortOrder = 2 },
                    new ProductFeature { ProductId = prod.Id, Icon = "bi-arrow-repeat", Text = "Đổi trả miễn phí trong 7 ngày", SortOrder = 3 }
                });
            }
            context.ProductFeatures.AddRange(features);

            if (!await context.BlogPosts.AnyAsync())
            {
                context.BlogPosts.AddRange(
                    new BlogPost { Title = "10 cách giữ thú cưng khỏe mạnh", Summary = "Mẹo chăm sóc hàng ngày cho chó mèo tại nhà.", Content = "Chăm sóc thú cưng đòi hỏi sự kiên nhẫn và chế độ dinh dưỡng hợp lý...", ImageUrl = "/images/news1.jpg", PublishedDate = DateTime.Now.AddDays(-2), IsFeatured = true },
                    new BlogPost { Title = "Chọn thức ăn chuẩn cho chó con theo độ tuổi", Summary = "Hướng dẫn chi tiết chọn hạt và sữa dinh dưỡng.", Content = "Giai đoạn đầu đời của cún con cần lượng calo và protein cao...", ImageUrl = "/images/news2.jpg", PublishedDate = DateTime.Now.AddDays(-5) },
                    new BlogPost { Title = "Lịch tiêm phòng vaccine cần thiết cho mèo cưng", Summary = "Bảo vệ các bé mèo khỏi bệnh truyền nhiễm nguy hiểm.", Content = "Tiêm vaccine là lá chắn bảo vệ tốt nhất cho các bé mèo...", ImageUrl = "/images/news3.jpg", PublishedDate = DateTime.Now.AddDays(-8) }
                );
            }

            await context.SaveChangesAsync();
        }

        private static async Task SeedPetsAndBreedsAsync(ApplicationDbContext context)
        {
            var p = await context.PetTypes.ToDictionaryAsync(x => x.Name, x => x.Id);

            var breeds = new List<Breed>
            {
                new() { Name = "Poodle Tiny", PetTypeId = p["Chó"], Description = "Giống chó thông minh, không rụng lông, ngoại hình đáng yêu như gấu bông.", Origin = "Pháp / Đức" },
                new() { Name = "Golden Retriever", PetTypeId = p["Chó"], Description = "Thân thiện, trung thành, cực kỳ tình cảm và nghe lời chủ nhân.", Origin = "Anh Quốc" },
                new() { Name = "Corgi Pembroke", PetTypeId = p["Chó"], Description = "Chó chân ngắn, mông trái tim nổi tiếng của Hoàng gia Anh, rất lanh lợi.", Origin = "Xứ Wales" },
                new() { Name = "Phốc Sóc (Pomeranian)", PetTypeId = p["Chó"], Description = "Bộ lông xù quý tộc, tính cách năng động, tinh nghịch.", Origin = "Ba Lan / Đức" },
                new() { Name = "Shiba Inu", PetTypeId = p["Chó"], Description = "Quốc khuyển Nhật Bản với gương mặt cười trứ danh, sạch sẽ và độc lập.", Origin = "Nhật Bản" },

                new() { Name = "Mèo Anh lông ngắn (BSH)", PetTypeId = p["Mèo"], Description = "Khuôn mặt bánh bao tròn trĩnh, tính tình điềm đạm, quấn chủ.", Origin = "Anh Quốc" },
                new() { Name = "Mèo Tai cụp (Scottish Fold)", PetTypeId = p["Mèo"], Description = "Đôi tai cụp độc đáo, đôi mắt to tròn ngây thơ, rất ngọt ngào.", Origin = "Scotland" },
                new() { Name = "Mèo Ragdoll", PetTypeId = p["Mèo"], Description = "Bộ lông dài mượt mà, mắt xanh biếc hút hồn, tính tình hiền như cục bột.", Origin = "Hoa Kỳ" },

                new() { Name = "Chim Họa Mi", PetTypeId = p["Chim"], Description = "Giọng hót trong trẻo, thánh thót, dáng vóc thanh tao.", Origin = "Việt Nam / Trung Quốc" },
                new() { Name = "Vẹt Sun Conure", PetTypeId = p["Chim"], Description = "Màu lông vàng cam rực rỡ, thông minh, biết nói và bắt chước tiếng người.", Origin = "Nam Mỹ" },
                new() { Name = "Chim Yến Phụng", PetTypeId = p["Chim"], Description = "Màu sắc đa dạng, hiền lành, dễ nuôi và rất quấn người.", Origin = "Úc" },

                new() { Name = "Hamster Bear (Syrian)", PetTypeId = p["Hamster"], Description = "Thân hình tròn trịa mập mạp, hiền khô, dễ bế ẵm.", Origin = "Syria" },
                new() { Name = "Hamster Winter White", PetTypeId = p["Hamster"], Description = "Kích thước mini, nhanh nhẹn, có thể đổi màu lông theo mùa.", Origin = "Siberia" }
            };

            context.Breeds.AddRange(breeds);
            await context.SaveChangesAsync();

            var b = await context.Breeds.ToDictionaryAsync(x => x.Name, x => x.Id);

            var pets = new List<Pet>
            {
                new()
                {
                    Name = "Bé Poodle Tiny Nâu Đỏ Thuần Chủng",
                    SkuCode = "PET-POD-001",
                    BreedId = b["Poodle Tiny"],
                    AgeMonths = 2,
                    BirthDate = DateTime.Now.AddDays(-75),
                    Gender = "Đực",
                    Color = "Nâu Đỏ (Red Brown)",
                    Price = 5800000,
                    OriginalPrice = 6500000,
                    IsVaccinated = true,
                    VaccinationDetails = "Đã tiêm 2 mũi Vanguard 7 bệnh, sổ theo dõi sức khỏe đầy đủ",
                    IsDewormed = true,
                    MicrochipCode = "KIKI-MC-9812",
                    HealthGuaranteeDays = 60,
                    Status = "Available",
                    Quantity = 1,
                    ImageUrl = "/images/pets/67affc0d-4334-458e-9a39-244e7403f720.jpg",
                    Description = "Bé Poodle Tiny lông xoăn tít màu nâu đỏ cực kỳ bắt mắt. Bé đã biết ăn hạt ngâm mềm, đi vệ sinh đúng chỗ, tính tình hoạt bát và quấn người."
                },
                new()
                {
                    Name = "Bé Golden Retriever Đại Gia Đình Ngoan Ngoãn",
                    SkuCode = "PET-GLD-002",
                    BreedId = b["Golden Retriever"],
                    AgeMonths = 3,
                    BirthDate = DateTime.Now.AddDays(-90),
                    Gender = "Cái",
                    Color = "Vàng Kem Ánh Kim",
                    Price = 7500000,
                    OriginalPrice = 8500000,
                    IsVaccinated = true,
                    VaccinationDetails = "Đã tiêm 2 mũi vaccine phòng 7 bệnh và tẩy giun định kỳ 2 lần",
                    IsDewormed = true,
                    MicrochipCode = "KIKI-MC-8743",
                    HealthGuaranteeDays = 45,
                    Status = "Available",
                    Quantity = 1,
                    ImageUrl = "/images/pets/7365a0a6-0d8f-4f52-bd54-81bf2bc5ff4e.jpg",
                    Description = "Bé Golden khuôn mặt bầu bĩnh, cốt to khung xương chuẩn, rất tình cảm và thông minh. Sẵn sàng về nhà mới cùng ba mẹ yêu thương."
                },
                new()
                {
                    Name = "Corgi Pembroke Mông Trái Tim Siêu Cưng",
                    SkuCode = "PET-CRG-003",
                    BreedId = b["Corgi Pembroke"],
                    AgeMonths = 2,
                    BirthDate = DateTime.Now.AddDays(-68),
                    Gender = "Đực",
                    Color = "Vàng Trắng (Red & White)",
                    Price = 8900000,
                    OriginalPrice = 9800000,
                    IsVaccinated = true,
                    VaccinationDetails = "Đã tiêm 2 mũi vaccine 7 bệnh, có chứng nhận kiểm dịch",
                    IsDewormed = true,
                    MicrochipCode = "KIKI-MC-4451",
                    HealthGuaranteeDays = 60,
                    Status = "Available",
                    Quantity = 1,
                    ImageUrl = "/images/pets/8c23a455-3910-4b93-b057-9991d08f7bd2.jpg",
                    Description = "Bé Corgi chân ngắn tủn mủn, mông hình trái tim lắc lư cực yêu. Bé ăn khỏe, không kén ăn, đã biết lệnh cơ bản ngồi và bắt tay."
                },
                new()
                {
                    Name = "Mèo Anh Lông Ngắn Xám Xanh Mặt Bánh Bao",
                    SkuCode = "PET-BSH-004",
                    BreedId = b["Mèo Anh lông ngắn (BSH)"],
                    AgeMonths = 3,
                    BirthDate = DateTime.Now.AddDays(-88),
                    Gender = "Cái",
                    Color = "Xám Xanh (Blue Solid)",
                    Price = 5200000,
                    OriginalPrice = 6000000,
                    IsVaccinated = true,
                    VaccinationDetails = "Tiêm 2 mũi vaccine 4 bệnh PureVax, sổ giun 3 lần",
                    IsDewormed = true,
                    MicrochipCode = "KIKI-MC-7719",
                    HealthGuaranteeDays = 60,
                    Status = "Available",
                    Quantity = 1,
                    ImageUrl = "/images/pets/910ef3d2-48eb-4654-8112-fd597eb07408.jpg",
                    Description = "Bé ALN màu xám xanh thuần chủng, nọng cổ to dày, má phính bánh bao. Bé biết sử dụng thau cát vệ sinh 100%, thích nằm ôm ngủ."
                },
                new()
                {
                    Name = "Mèo Tai Cụp Scottish Fold Xám Khói Quý Tộc",
                    SkuCode = "PET-SCF-005",
                    BreedId = b["Mèo Tai cụp (Scottish Fold)"],
                    AgeMonths = 2,
                    BirthDate = DateTime.Now.AddDays(-70),
                    Gender = "Đực",
                    Color = "Xám Khói Ánh Bạc",
                    Price = 6200000,
                    OriginalPrice = 7000000,
                    IsVaccinated = true,
                    VaccinationDetails = "Tiêm 2 mũi vaccine ngừa bệnh truyền nhiễm mèo",
                    IsDewormed = true,
                    MicrochipCode = "KIKI-MC-3329",
                    HealthGuaranteeDays = 60,
                    Status = "Available",
                    Quantity = 1,
                    ImageUrl = "/images/pets/bc5795fd-c093-4895-bb76-748443a81afe.jpg",
                    Description = "Bé Scottish Fold tai cụp sát đầu cực hiếm, mắt tròn như bi ve ve. Bản tính nhẹ nhàng, trầm tính, không cắn phá đồ đạc."
                },
                new()
                {
                    Name = "Mèo Ragdoll Mắt Xanh Dương Thuần Chủng",
                    SkuCode = "PET-RGD-006",
                    BreedId = b["Mèo Ragdoll"],
                    AgeMonths = 3,
                    BirthDate = DateTime.Now.AddDays(-95),
                    Gender = "Cái",
                    Color = "Seal Point Bicolor",
                    Price = 12500000,
                    OriginalPrice = 14000000,
                    IsVaccinated = true,
                    VaccinationDetails = "Đã tiêm đủ 2 mũi vaccine, có giấy tiêm phòng thú y",
                    IsDewormed = true,
                    MicrochipCode = "KIKI-MC-1102",
                    HealthGuaranteeDays = 90,
                    Status = "Available",
                    Quantity = 1,
                    ImageUrl = "/images/pets/f01125d4-a2df-46e1-9bb4-7c5fb9d30b2c.jpg",
                    Description = "Tiểu thư Ragdoll mắt xanh màu đại dương sâu thẳm, lông mềm xù như bông gòn. Cực kỳ hiền lành, thích được bế ngửa nựng nịu."
                },
                new()
                {
                    Name = "Vẹt Sun Conure Vàng Cam Rực Rỡ",
                    SkuCode = "PET-BRD-007",
                    BreedId = b["Vẹt Sun Conure"],
                    AgeMonths = 4,
                    BirthDate = DateTime.Now.AddDays(-120),
                    Gender = "Không rõ",
                    Color = "Vàng Cam Đỏ",
                    Price = 3800000,
                    OriginalPrice = 4200000,
                    IsVaccinated = true,
                    VaccinationDetails = "Đã kiểm dịch thú y, cánh tỉa nhẹ an toàn",
                    IsDewormed = true,
                    MicrochipCode = null,
                    HealthGuaranteeDays = 30,
                    Status = "Available",
                    Quantity = 1,
                    ImageUrl = "/images/pets/fb4e4ffb-319b-497a-a682-01f1acad1f56.jpg",
                    Description = "Bé Vẹt Sun Conure sắc màu nổi bật, đã qua thuần hóa, bay đậu trên tay chủ khi gọi. Đang giai đoạn học nói từ đơn rất nhanh."
                },
                new()
                {
                    Name = "Cặp Chim Yến Phụng Đa Sắc Hót Líu Lo",
                    SkuCode = "PET-BRD-008",
                    BreedId = b["Chim Yến Phụng"],
                    AgeMonths = 5,
                    BirthDate = DateTime.Now.AddDays(-150),
                    Gender = "Đực",
                    Color = "Xanh Dương & Vàng Chanh",
                    Price = 450000,
                    OriginalPrice = 550000,
                    IsVaccinated = true,
                    VaccinationDetails = "Đã xổ giun và sát khuẩn định kỳ",
                    IsDewormed = true,
                    MicrochipCode = null,
                    HealthGuaranteeDays = 30,
                    Status = "Available",
                    Quantity = 2,
                    ImageUrl = "/images/product-accessory.svg",
                    Description = "Cặp yến phụng phối màu bắt mắt, tiếng hót líu lo vui tai cả ngày. Tặng kèm cóng nước và thức ăn thử."
                },
                new()
                {
                    Name = "Hamster Bear Vàng Mập Dễ Thương",
                    SkuCode = "PET-HAM-009",
                    BreedId = b["Hamster Bear (Syrian)"],
                    AgeMonths = 2,
                    BirthDate = DateTime.Now.AddDays(-60),
                    Gender = "Đực",
                    Color = "Vàng Mật Ong",
                    Price = 120000,
                    OriginalPrice = 150000,
                    IsVaccinated = true,
                    VaccinationDetails = "Đã kiểm tra sức khỏe và ký sinh trùng",
                    IsDewormed = true,
                    MicrochipCode = null,
                    HealthGuaranteeDays = 15,
                    Status = "Available",
                    Quantity = 3,
                    ImageUrl = "/images/product-toy.svg",
                    Description = "Bé Bear tròn ung ủng, 2 má phúng phính trữ thức ăn siêu ngộ nghĩnh. Bé rất lành tính, không cắn tay người chăm."
                }
            };

            context.Pets.AddRange(pets);
            await context.SaveChangesAsync();
        }
    }
}
