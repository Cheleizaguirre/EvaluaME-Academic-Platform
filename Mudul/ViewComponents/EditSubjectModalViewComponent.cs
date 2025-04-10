using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Mudul.Data;
using Mudul.EntityModels;
using Mudul.Models;

namespace Mudul.ViewComponents
{
    public class EditSubjectModalViewComponent : ViewComponent
    {
        private readonly DefaultdbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public EditSubjectModalViewComponent(DefaultdbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync(int id)
        {
            var subject = await _context.Subjects
                .Include(s => s.Teacher)
                .FirstOrDefaultAsync(s => s.SubjectId == id);

            if (subject == null)
            {
                return Content("No se encontró la clase.");
            }

            var teachers = await _userManager.GetUsersInRoleAsync("Teacher");

            var model = new SubjectEditViewModel
            {
                SubjectId = subject.SubjectId,
                Name = subject.Name,
                Description = subject.Description,
                Year = subject.Year,
                TeacherId = subject.Teacher?.Id,
                Teachers = teachers.Select(t => new SelectListItem
                {
                    Value = t.Id,
                    Text = t.UserName
                }).ToList()
            };

            return View(model);
        }
    }

}
