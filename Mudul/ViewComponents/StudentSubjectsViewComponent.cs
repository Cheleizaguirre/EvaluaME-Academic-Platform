using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mudul.Data;
using Mudul.EntityModels;
using Mudul.Models;
using System.Security.Claims;
using System.Threading.Tasks;



namespace Mudul.ViewComponents
{
    public class StudentSubjectsViewComponent : ViewComponent
    {
        private readonly DefaultdbContext _context;
        private readonly ApplicationDbContext _contextIdentity;
        public StudentSubjectsViewComponent(DefaultdbContext context, ApplicationDbContext contextIdentity)
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

            var enrollments = await _context.Enrollments.Where(e => e.StudentId == user.Id).Include(e => e.Subject).ToListAsync();

            var student = new StudentModel
            {
                UserId = user.Id,
                FullName = user.UserName,
                Enrollments = enrollments
            };
            return View(student);
        }
    }
}
