using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Mudul.Models;

namespace Mudul.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserRoleService _userRoleService;

        public HomeController(ILogger<HomeController> logger, UserRoleService userRoleService)
        {
            _logger = logger;
            _userRoleService = userRoleService;
        }

        public async Task<IActionResult> Index()
        {
            var role = await _userRoleService.GetUserRoleAsync();
            ViewData["Layout"] = role switch
            {
                "Admin" => "_LayoutAdmin",
                "Student" => "_LayoutStudent",
                "Teacher" => "_LayoutTeacher",
                "Coordinator" => "_LayoutCoordinator",
                _ => "_Layout"
            };
            if (User.IsInRole("Student"))
            {
                return RedirectToAction("Index", "Student");
            }
            else if (User.IsInRole("Coordinator"))
            {
                return RedirectToAction("Index", "Coordinator");
            }
            else if (User.IsInRole("Teacher"))
            {
                return RedirectToAction("Index", "Teacher");
            }
            else if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }

            return Redirect("/Identity/Account/Login");     //TEMPORAL
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
