using Microsoft.AspNetCore.Mvc;

namespace Mudul.ViewComponents
{
    public class CreateStudentModalViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
