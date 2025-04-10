using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mudul.Data;
using Mudul.EntityModels;
using Mudul.Models;
using NuGet.Protocol;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Mudul.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        private readonly DefaultdbContext _context;
        private readonly SignInManager<IdentityUser> _signInManager;

        public StudentController(DefaultdbContext context, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _context = context;
            _signInManager = signInManager;
        }

        public IActionResult Classes(string course)
        {
            ViewBag.CourseName = course; // Para mostrar el nombre en la vista
            return View();
        }

        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _context.AspNetUsers.Where(e => e.Id == userId).Include(e => e.Roles).FirstOrDefault();
            var userViewModel = new UserEditViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Roles.FirstOrDefault().Name
            };
            return View(userViewModel);
        }


        // GET: StudentController
        public async Task<IActionResult> Index()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var enrollments = _context.Enrollments
                .Include(e => e.Subject)
                .Include(e => e.Subject.Teacher)
                .Where(e => e.StudentId == currentUserId)
                .ToList();
            return View(enrollments);
        }

        // POST: StudentController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string Id, string UserName, string Email, string PhoneNumber, string Password, string ConfirmPassword)
        {
            var user = await _context.AspNetUsers.FindAsync(Id);
            if (user == null)
            {
                return NotFound();
            }

            // Actualizar datos del usuario con _context
            user.UserName = UserName;
            user.Email = Email;
            user.PhoneNumber = PhoneNumber;

            if (!string.IsNullOrEmpty(Password) && Password == ConfirmPassword)
            {
                var identityUser = await _userManager.FindByIdAsync(Id);
                if (identityUser != null)
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(identityUser);
                    await _userManager.ResetPasswordAsync(identityUser, token, Password);
                }
            }

            try
            {
                _context.Update(user);
                await _context.SaveChangesAsync();

                // Refrescar sesión si el usuario editado es el usuario logueado actualmente
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null && currentUser.Id == Id)
                {
                    await _signInManager.RefreshSignInAsync(currentUser);
                }

                TempData["SuccessMessage"] = "Perfil actualizado correctamente.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Hubo un error al actualizar el perfil.";
            }

            return RedirectToAction("Profile");
        }
    }


}
