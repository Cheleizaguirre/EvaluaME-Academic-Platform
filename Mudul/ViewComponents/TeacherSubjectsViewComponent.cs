using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mudul.Data;
using Mudul.EntityModels;
using Mudul.Models;
using System.Security.Claims;

namespace Mudul.ViewComponents
{
    public class TeacherSubjectsViewComponent : ViewComponent
    {
        private readonly DefaultdbContext _context;
        private readonly ApplicationDbContext _contextIdentity;

        public TeacherSubjectsViewComponent(DefaultdbContext context, ApplicationDbContext contextIdentity)
        {
            _context = context;
            _contextIdentity = contextIdentity;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _contextIdentity.Users.Where(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null)
            {
                return View(null);
            }
            var subjects = await _context.Subjects.Where(s => s.TeacherId == user.Id).ToListAsync();
            var teacher = new TeacherModel
            {
                UserId = user.Id,
                FullName = user.UserName,
                Subjects = subjects
            };
            return View(teacher);
        }
    }
}
