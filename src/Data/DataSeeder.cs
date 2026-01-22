using authModule.Models;
using Microsoft.AspNetCore.Identity;

namespace authModule.DataContext
{
    public static class DataSeeder
    {
        public static async Task SeedAdminUser(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
            
            string adminEmail = "vucuongtuan03@gmail.com";
            string adminPass = "Admin@123";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var user = new User
                {
                    UserName = "CuongAdmin",
                    Email = adminEmail,
                    EmailConfirmed = true,
                    Role = UserRole.Admin,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(user, adminPass);
                if (result.Succeeded)
                {
                    Console.WriteLine("====================================");
                    Console.WriteLine($"Admin user created: {adminEmail} / {adminPass}");
                    Console.WriteLine("====================================");
                }
                else
                {
                    Console.WriteLine("Failed to create Admin user: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}
