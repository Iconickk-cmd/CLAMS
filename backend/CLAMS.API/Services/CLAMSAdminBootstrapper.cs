
using CLAMS.Infrastructure.Data;
using CLAMS.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CLAMS.API.Services;

public static class CLAMSAdminBootstrapper
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var username = Environment.GetEnvironmentVariable(
            "CLAMS_ADMIN_USERNAME");

        var password = Environment.GetEnvironmentVariable(
            "CLAMS_ADMIN_PASSWORD");

        // Do nothing unless bootstrap credentials are supplied.
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (!username.All(char.IsDigit) || username.Length > 20)
        {
            throw new InvalidOperationException(
                "Administrator username must contain only digits " +
                "and be no longer than 20 characters.");
        }

        using var scope = services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<CLAMSHRDbContext>();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<CLAMSApplicationUser>>();

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole<int>>>();

        // Only bootstrap the first portal account.
        if (await db.Users.AnyAsync())
        {
            Console.WriteLine(
                "Administrator bootstrap skipped: portal accounts already exist.");
            return;
        }

        var employee = await db.Employees
            .SingleOrDefaultAsync(e =>
                e.EmployeeNumber == "CLAMS-EMP-0002");

        if (employee == null)
        {
            throw new InvalidOperationException(
                "Administrator employee CLAMS-EMP-0002 was not found.");
        }

        if (employee.EmploymentStatus.ToString() != "Active")
        {
            throw new InvalidOperationException(
                "The administrator employee record is not active.");
        }

        const string roleName = "SystemAdministrator";

        if (!await roleManager.RoleExistsAsync(roleName))
        {
            throw new InvalidOperationException(
                "SystemAdministrator role has not been seeded.");
        }

        var user = new CLAMSApplicationUser
        {
            UserName = username,
            Email = employee.Email,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            EmployeeId = employee.Id,

            EmailConfirmed = true,
            IsActive = true,
            IsPortalActivated = true,
            MustChangePassword = false,

            CreatedAt = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(
            user, password);

        if (!createResult.Succeeded)
        {
            var errors = string.Join(
                "; ",
                createResult.Errors.Select(e => e.Description));

            throw new InvalidOperationException(
                $"Administrator account creation failed: {errors}");
        }

        var roleResult = await userManager.AddToRoleAsync(
            user, roleName);

        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);

            var errors = string.Join(
                "; ",
                roleResult.Errors.Select(e => e.Description));

            throw new InvalidOperationException(
                $"Administrator role assignment failed: {errors}");
        }

        Console.WriteLine(
            $"Initial administrator account created successfully. " +
            $"Username: {username}; Employee: {employee.EmployeeNumber}; " +
            $"Role: {roleName}.");
    }
}
