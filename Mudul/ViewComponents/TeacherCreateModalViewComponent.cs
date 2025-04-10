using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mudul.EntityModels;
using Mudul.Models;

namespace Mudul.ViewComponents
{
    public class TeacherCreateModalViewComponent : ViewComponent
    {
        private readonly DefaultdbContext _context;

        public TeacherCreateModalViewComponent(DefaultdbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Obtener las materias disponibles para asignar al docente
            var subjects = await _context.Subjects.ToListAsync();
            var model = new TeacherModel
            {
                Subjects = subjects
            };

            return View(model);
        }
    }

}
