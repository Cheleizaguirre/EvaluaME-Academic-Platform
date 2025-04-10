using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mudul.EntityModels;
using Mudul.Models;
using System.Security.Claims;

namespace Mudul.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class TeacherController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly DefaultdbContext _context;
        private readonly SignInManager<IdentityUser> _signInManager;

        public TeacherController(DefaultdbContext context, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: TeacherController
        public IActionResult Index()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var subjects = _context.Subjects
                .Where(s => s.TeacherId == currentUserId)
                .Select(s => new SubjectViewModel
                {
                    Id = s.SubjectId,
                    Name = s.Name,
                    StudentCount = s.Enrollments.Count(e => e.Status == "ACTIVE")
                })
                .ToList();

            return View(subjects);
        }


        // GET: TeacherController/Evaluations
        public IActionResult Evaluations()
        {
            return View();
        }

        // GET: TeacherController/Profile
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

        // GET: TeacherController/Details/5
        public IActionResult Details(int id)
        {
            return View();
        }

        // GET: TeacherController/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: TeacherController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: TeacherController/Edit/5
        public IActionResult Edit(int id)
        {
            return View();
        }

        // POST: TeacherController/Edit/5
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

        // GET: TeacherController/Delete/5
        public IActionResult Delete(int id)
        {
            return View();
        }

        // POST: TeacherController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

    }
}
