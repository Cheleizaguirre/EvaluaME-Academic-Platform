using Microsoft.AspNetCore.Mvc;
using Mudul.Models;

namespace Mudul.ViewComponents
{
    public class EditStudentModalViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(StudentModel student)
        {
            return View(student);
        }
    }
}
