using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Mudul.Data;
using System.Security.Claims;

public class UserRoleService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserRoleService(
        IHttpContextAccessor httpContextAccessor, 
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<string> GetUserRoleAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
        {
            return "Guest";
        }

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return "Guest";
        }

        var identityUser = await _userManager.FindByIdAsync(userId);
        if (identityUser == null)
        {
            return "Guest";
        }

        var roles = await _userManager.GetRolesAsync(identityUser);
        var role = roles.FirstOrDefault();

        if (role != null)
        {
            return role switch
            {
                "Admin" => "Admin",
                "Student" => "Student",
                "Teacher" => "Teacher",
                "Coordinator" => "Coordinator",
                _ => "Guest"
            };
        }

        return "Guest";
    }
}
