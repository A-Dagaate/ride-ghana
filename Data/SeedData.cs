using Microsoft.AspNetCore.Identity;
using RideGhana.Models;

namespace RideGhana.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.EnsureCreatedAsync();

        // Seed roles
        foreach (var role in new[] { "Admin", "Customer" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Seed admin user
        if (await userManager.FindByEmailAsync("admin@rideghana.com") == null)
        {
            var admin = new ApplicationUser
            {
                UserName = "admin@rideghana.com",
                Email = "admin@rideghana.com",
                FullName = "Ride Ghana Admin",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(admin, "Admin@123!");
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        // Seed cars
        if (!context.Cars.Any())
        {
            context.Cars.AddRange(new List<Car>
            {
                new Car
                {
                    Make = "Toyota",
                    Model = "Camry",
                    Year = 2014,
                    Category = "Economy",
                    DailyRate = 200,
                    Seats = 5,
                    Transmission = "Automatic",
                    Description = "Reliable and fuel-efficient sedan, perfect for city driving around Kumasi.",
                    ImagePath = "/images/cars/camry_front.jpeg",
                    Location = "Kumasi, Ghana",
                    DisplayOrder = 1
                },
                new Car
                {
                    Make = "Toyota",
                    Model = "Corolla",
                    Year = 2017,
                    Category = "Economy",
                    DailyRate = 200,
                    Seats = 5,
                    Transmission = "Automatic",
                    Description = "Reliable and fuel-efficient sedan, perfect for city driving around Kumasi.",
                    ImagePath = "/images/cars/Corolla_side.jpeg",
                    Location = "Kumasi, Ghana",
                    DisplayOrder = 2
                },
                new Car
                {
                    Make = "Hyundai",
                    Model = "Elantra",
                    Year = 2015,
                    Category = "Economy",
                    DailyRate = 250,
                    Seats = 5,
                    Transmission = "Automatic",
                    Description = "Reliable and fuel-efficient sedan, perfect for city driving around Kumasi.",
                    ImagePath = "/images/cars/Elantra_f.jpeg",
                    Location = "Kumasi, Ghana",
                    DisplayOrder = 3
                },
                new Car
                {
                    Make = "Mercedes-Benz",
                    Model = "E-Class",
                    Year = 2015,
                    Category = "Luxury",
                    DailyRate = 650,
                    Seats = 5,
                    Transmission = "Automatic",
                    Description = "Premium executive sedan for business travel or special occasions.",
                    ImagePath = "/images/cars/placeholder.svg",
                    Location = "Kumasi, Ghana",
                    DisplayOrder = 4
                },
                new Car
                {
                    Make = "Hyundai",
                    Model = "Tucson",
                    Year = 2022,
                    Category = "SUV",
                    DailyRate = 280,
                    Seats = 5,
                    Transmission = "Automatic",
                    Description = "Comfortable mid-size SUV, great for families and weekend road trips.",
                    ImagePath = "/images/cars/placeholder.svg",
                    Location = "Kumasi, Ghana",
                    DisplayOrder = 5
                },
                new Car
                {
                    Make = "Kia",
                    Model = "Rio",
                    Year = 2023,
                    Category = "Economy",
                    DailyRate = 150,
                    Seats = 5,
                    Transmission = "Manual",
                    Description = "Compact and budget-friendly, great for navigating busy city streets.",
                    ImagePath = "/images/cars/placeholder.svg",
                    Location = "Kumasi, Ghana",
                    DisplayOrder = 6
                }
            });

            await context.SaveChangesAsync();
        }
    }
}
