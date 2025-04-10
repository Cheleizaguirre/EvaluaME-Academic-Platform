using Microsoft.AspNetCore.Mvc;
using Mudul.Models;

namespace Mudul.ViewComponents
{
    public class SubjectCreateModalViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var model = new SubjectModel();
            return View(model);
        }
    }
}
