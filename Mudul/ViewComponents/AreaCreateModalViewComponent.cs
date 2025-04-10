using Microsoft.AspNetCore.Mvc;
using Mudul.EntityModels;

namespace Mudul.ViewComponents
{
    public class AreaCreateModalViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var model = new Area();
            return View(model);
        }
    }
}
