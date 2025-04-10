using Microsoft.AspNetCore.Mvc;
using Mudul.EntityModels;
namespace Mudul.ViewComponents
{
    public class EditAreaModalViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(Area area)
        {
            return View(area);
        }
    }
}
