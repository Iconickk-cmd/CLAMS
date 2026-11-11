
using Microsoft.AspNetCore.Identity;

namespace CLAMS.API.Services;

public static class CLAMSRoleSeeder
{
    private static readonly string[] Roles =
    {
        "SystemAdministrator",
        "HRManager",
        "HROfficer",
        "Management",
        "ReadOnly",
        "Employee"
    };

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole<int>>>();

        foreach (var role in Roles)
        {
            if (await roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(
                new IdentityRole<int>(role));

            if (!result.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    result.Errors.Select(e => e.Description));

                throw new InvalidOperationException(
                    $"Could not create role '{role}': {errors}");
            }
        }
    }
}
