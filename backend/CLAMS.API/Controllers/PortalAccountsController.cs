
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CLAMS.API.DTOs.PortalAccounts;
using CLAMS.Domain.Enums;
using CLAMS.Infrastructure.Data;
using CLAMS.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CLAMS.API.Controllers;

[Route("api/portal-accounts")]
[ApiController]
[Authorize(Roles = "SystemAdministrator,HRManager")]
public class PortalAccountsController : ControllerBase
{
    private readonly CLAMSHRDbContext _context;
    private readonly UserManager<CLAMSApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<int>> _roleManager;
    private readonly SignInManager<CLAMSApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;

    public PortalAccountsController(
        CLAMSHRDbContext context,
        UserManager<CLAMSApplicationUser> userManager,
        RoleManager<IdentityRole<int>> roleManager,
        SignInManager<CLAMSApplicationUser> signInManager,
        IConfiguration configuration)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    // =========================================================
    // CREATE STAFF PORTAL ACCOUNT
    // HR Manager or System Administrator only
    // =========================================================

    [HttpPost]
    public async Task<IActionResult> CreateAccount(
        [FromBody] CreateStaffPortalAccountRequest request)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId);

        if (employee == null)
        {
            return NotFound(new
            {
                message = "Employee was not found."
            });
        }

        if (employee.EmploymentStatus != EmploymentStatus.Active)
        {
            return BadRequest(new
            {
                message = "Only active employees can receive portal accounts."
            });
        }

        var existingAccount = await _userManager.Users
            .AnyAsync(u => u.EmployeeId == employee.Id);

        if (existingAccount)
        {
            return Conflict(new
            {
                message = "This employee already has a portal account."
            });
        }

        var username = string.IsNullOrWhiteSpace(request.PortalUsername)
            ? await GenerateUsernameAsync()
            : request.PortalUsername.Trim();

        if (username.Length > 20 || !username.All(char.IsDigit))
        {
            return BadRequest(new
            {
                message = "Portal username must contain numbers only and cannot exceed 20 digits."
            });
        }

        var existingUsername =
            await _userManager.FindByNameAsync(username);

        if (existingUsername != null)
        {
            return Conflict(new
            {
                message = "This portal username is already in use."
            });
        }

        if (!await _roleManager.RoleExistsAsync("Employee"))
        {
            return StatusCode(500, new
            {
                message = "The Employee security role has not been configured."
            });
        }

        var temporaryPassword =
            RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

        var activationToken = WebEncoders.Base64UrlEncode(
            RandomNumberGenerator.GetBytes(32));

        var activationTokenHash = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(activationToken)));

        var expiresAt = DateTime.UtcNow.AddHours(48);

        var user = new CLAMSApplicationUser
        {
            UserName = username,
            Email = employee.Email,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            EmployeeId = employee.Id,

            IsActive = true,
            IsPortalActivated = false,
            MustChangePassword = true,

            ActivationTokenHash = activationTokenHash,
            ActivationTokenExpiresAt = expiresAt,
            TemporaryPasswordExpiresAt = expiresAt,

            CreatedAt = DateTime.UtcNow
        };

        // Hash the temporary password.
        // Permanent password rules are enforced during activation.
        user.PasswordHash = _userManager.PasswordHasher.HashPassword(
            user,
            temporaryPassword);

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        var createResult = await _userManager.CreateAsync(user);

        if (!createResult.Succeeded)
        {
            await transaction.RollbackAsync();

            return BadRequest(new
            {
                message = "Could not create the portal account.",
                errors = createResult.Errors.Select(e => e.Description)
            });
        }

        var roleResult = await _userManager.AddToRoleAsync(
            user,
            "Employee");

        if (!roleResult.Succeeded)
        {
            await transaction.RollbackAsync();

            return StatusCode(500, new
            {
                message = "Could not assign the Employee role.",
                errors = roleResult.Errors.Select(e => e.Description)
            });
        }

        await transaction.CommitAsync();

        var activationBaseUrl =
            _configuration["Portal:ActivationBaseUrl"]
            ?? "http://localhost:4300/activate-account";

        var activationUrl = QueryHelpers.AddQueryString(
            activationBaseUrl,
            new Dictionary<string, string?>
            {
                ["username"] = username,
                ["token"] = activationToken
            });

        var employeeName = string.Join(
            " ",
            new[]
            {
                employee.FirstName,
                employee.MiddleName,
                employee.LastName
            }.Where(name => !string.IsNullOrWhiteSpace(name)));

        var response = new StaffPortalAccountResponse
        {
            UserId = user.Id,
            EmployeeId = employee.Id,
            EmployeeNumber = employee.EmployeeNumber,
            EmployeeName = employeeName,
            PortalUsername = username,
            TemporaryPassword = temporaryPassword,
            ActivationUrl = activationUrl,
            ActivationExpiresAt = expiresAt,

            Message =
                "Staff portal account created successfully. " +
                "The employee must activate the account within 48 hours."
        };

        return Ok(response);
    }

    // =========================================================
    // ACTIVATE STAFF PORTAL ACCOUNT
    // Public endpoint: employee does not need to be logged in
    // =========================================================

    [AllowAnonymous]
    [HttpPost("activate")]
    public async Task<IActionResult> ActivateAccount(
        [FromBody] ActivateStaffPortalAccountRequest request)
    {
        var user = await _userManager.Users
            .Include(u => u.Employee)
            .SingleOrDefaultAsync(u =>
                u.UserName == request.PortalUsername);

        if (user == null)
        {
            return BadRequest(new
            {
                message = "Invalid activation details."
            });
        }

        if (user.IsPortalActivated)
        {
            return Conflict(new
            {
                message = "This portal account has already been activated."
            });
        }

        if (!user.IsActive ||
            user.Employee == null ||
            user.Employee.EmploymentStatus != EmploymentStatus.Active)
        {
            return BadRequest(new
            {
                message = "This employee portal account is not eligible for activation."
            });
        }

        if (string.IsNullOrWhiteSpace(user.ActivationTokenHash) ||
            user.ActivationTokenExpiresAt == null ||
            user.ActivationTokenExpiresAt <= DateTime.UtcNow)
        {
            return BadRequest(new
            {
                message = "The activation link has expired. Contact HR for a new link."
            });
        }

        if (user.TemporaryPasswordExpiresAt == null ||
            user.TemporaryPasswordExpiresAt <= DateTime.UtcNow)
        {
            return BadRequest(new
            {
                message = "The temporary password has expired. Contact HR for a new one."
            });
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return BadRequest(new
            {
                message = "Invalid activation details."
            });
        }

        var suppliedTokenHash = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(request.ActivationToken)));

        var storedHashBytes =
            Encoding.UTF8.GetBytes(user.ActivationTokenHash);

        var suppliedHashBytes =
            Encoding.UTF8.GetBytes(suppliedTokenHash);

        if (storedHashBytes.Length != suppliedHashBytes.Length ||
            !CryptographicOperations.FixedTimeEquals(
                storedHashBytes,
                suppliedHashBytes))
        {
            return BadRequest(new
            {
                message = "Invalid activation details."
            });
        }

        var passwordVerification =
            _userManager.PasswordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.TemporaryPassword);

        if (passwordVerification == PasswordVerificationResult.Failed)
        {
            return BadRequest(new
            {
                message = "Invalid activation details."
            });
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        var passwordResult = await _userManager.ChangePasswordAsync(
            user,
            request.TemporaryPassword,
            request.NewPassword);

        if (!passwordResult.Succeeded)
        {
            await transaction.RollbackAsync();

            return BadRequest(new
            {
                message = "Could not set the new password.",
                errors = passwordResult.Errors.Select(e => e.Description)
            });
        }

        user.IsPortalActivated = true;
        user.MustChangePassword = false;

        user.ActivationTokenHash = null;
        user.ActivationTokenExpiresAt = null;
        user.TemporaryPasswordExpiresAt = null;

        user.UpdatedAt = DateTime.UtcNow;

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            await transaction.RollbackAsync();

            return StatusCode(500, new
            {
                message = "Account activation could not be completed.",
                errors = updateResult.Errors.Select(e => e.Description)
            });
        }

        await transaction.CommitAsync();

        var employeeName = string.Join(
            " ",
            new[]
            {
                user.Employee.FirstName,
                user.Employee.MiddleName,
                user.Employee.LastName
            }.Where(name => !string.IsNullOrWhiteSpace(name)));

        var response = new ActivateStaffPortalAccountResponse
        {
            IsSuccessful = true,
            PortalUsername = user.UserName!,
            EmployeeName = employeeName,
            Message =
                "Your staff portal account has been activated successfully. " +
                "You can now log in using your portal username and new password."
        };

        return Ok(response);
    }

    // =========================================================
    // STAFF PORTAL LOGIN
    // Public endpoint: employee uses numeric username/password
    // =========================================================

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] StaffPortalLoginRequest request)
    {
        var user = await _userManager.Users
            .Include(u => u.Employee)
            .SingleOrDefaultAsync(u =>
                u.UserName == request.PortalUsername);

        if (user == null)
        {
            return Unauthorized(new
            {
                message = "Invalid username or password."
            });
        }

        if (!user.IsPortalActivated)
        {
            return Unauthorized(new
            {
                message = "Your portal account has not been activated. Please complete activation first."
            });
        }

        if (!user.IsActive ||
            user.Employee == null ||
            user.Employee.EmploymentStatus != EmploymentStatus.Active)
        {
            return Unauthorized(new
            {
                message = "This portal account is not currently eligible for access."
            });
        }

        // Verify password and apply Identity lockout rules.
        var signInResult = await _signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);

        if (signInResult.IsLockedOut)
        {
            return StatusCode(423, new
            {
                message = "Your account is temporarily locked because of repeated failed login attempts. Please try again later."
            });
        }

        if (!signInResult.Succeeded)
        {
            return Unauthorized(new
            {
                message = "Invalid username or password."
            });
        }

        var roles = await _userManager.GetRolesAsync(user);

        var jwtKey = _configuration["Jwt:Key"];
        var jwtIssuer = _configuration["Jwt:Issuer"];
        var jwtAudience = _configuration["Jwt:Audience"];

        if (string.IsNullOrWhiteSpace(jwtKey) ||
            string.IsNullOrWhiteSpace(jwtIssuer) ||
            string.IsNullOrWhiteSpace(jwtAudience))
        {
            return StatusCode(500, new
            {
                message = "Authentication configuration is incomplete."
            });
        }

        byte[] keyBytes;

        try
        {
            keyBytes = Convert.FromBase64String(jwtKey);
        }
        catch (FormatException)
        {
            return StatusCode(500, new
            {
                message = "JWT signing key configuration is invalid."
            });
        }

        var signingKey = new SymmetricSecurityKey(keyBytes);

        var credentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        var expiresAt = DateTime.UtcNow.AddMinutes(60);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("name", user.UserName!),
            new("userId", user.Id.ToString()),
            new("employeeId", user.Employee.Id.ToString()),
            new("employeeNumber", user.Employee.EmployeeNumber)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim("role", role));
        }

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler()
            .WriteToken(token);

        var employeeName = string.Join(
            " ",
            new[]
            {
                user.Employee.FirstName,
                user.Employee.MiddleName,
                user.Employee.LastName
            }.Where(name => !string.IsNullOrWhiteSpace(name)));

        var response = new StaffPortalLoginResponse
        {
            IsSuccessful = true,
            AccessToken = accessToken,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            EmployeeId = user.Employee.Id,
            PortalUsername = user.UserName!,
            EmployeeNumber = user.Employee.EmployeeNumber,
            EmployeeName = employeeName,
            Roles = roles.ToArray(),
            Message = "Login successful."
        };

        return Ok(response);
    }

    // =========================================================
    // ASSIGN HR MANAGER ROLE
    // System Administrator only
    // =========================================================

    [Authorize(Roles = "SystemAdministrator")]
    [HttpPost("{userId:int}/assign-hr-manager")]
    public async Task<IActionResult> AssignHRManagerRole(int userId)
    {
        var user = await _userManager.Users
            .Include(u => u.Employee)
            .SingleOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return NotFound(new
            {
                message = "Portal account was not found."
            });
        }

        if (!user.IsActive ||
            user.Employee == null ||
            user.Employee.EmploymentStatus != EmploymentStatus.Active)
        {
            return BadRequest(new
            {
                message = "Only active employee accounts can receive the HRManager role."
            });
        }

        const string roleName = "HRManager";

        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            return StatusCode(500, new
            {
                message = "The HRManager security role has not been configured."
            });
        }

        if (await _userManager.IsInRoleAsync(user, roleName))
        {
            return Ok(new
            {
                message = "The account already has the HRManager role.",
                userId = user.Id,
                portalUsername = user.UserName,
                role = roleName
            });
        }

        var result = await _userManager.AddToRoleAsync(user, roleName);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = "Could not assign the HRManager role.",
                errors = result.Errors.Select(e => e.Description)
            });
        }

        return Ok(new
        {
            message = "HRManager role assigned successfully.",
            userId = user.Id,
            portalUsername = user.UserName,
            role = roleName
        });
    }
    // =========================================================
    // AUTOMATIC NUMERIC USERNAME GENERATION
    // =========================================================

    private async Task<string> GenerateUsernameAsync()
    {
        var usernames = await _userManager.Users
            .Select(u => u.UserName)
            .ToListAsync();

        var highestNumber = usernames
            .Where(name =>
                !string.IsNullOrWhiteSpace(name) &&
                name.All(char.IsDigit))
            .Select(name =>
                int.TryParse(name, out var number) ? number : 0)
            .DefaultIfEmpty(0)
            .Max();

        return checked(highestNumber + 1).ToString();
    }
}

