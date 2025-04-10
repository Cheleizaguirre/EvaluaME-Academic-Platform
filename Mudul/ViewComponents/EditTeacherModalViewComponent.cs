using Microsoft.AspNetCore.Mvc;
using Mudul.Models;

namespace Mudul.ViewComponents
{
    public class EditTeacherModalViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(TeacherModel teacher)
        {
            return View(teacher);
        }
    }
}