using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mudul.EntityModels;
using System.Security.Claims;

namespace Mudul.Controllers
{
    public class AreaController : Controller
    {
        private readonly DefaultdbContext _context;
        public AreaController(DefaultdbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var areas = _context.Areas
                .Where(e => e.Status == "ACTIVE")
                .Include(e => e.Coordinator)
                .ToList();
            return View(areas);
        }


        // GET: Mostrar modal de creación
        public async Task<IActionResult> Create()
        {
            var model = new Area();
            return ViewComponent("AreaCreateModal", model);
        }

        // GET: Mostrar modal de edición
        public async Task<IActionResult> Edit(int id)
        {
            var area = await _context.Areas
                .Include(e => e.Coordinator)
                .FirstOrDefaultAsync(e => e.AreaId == id);
            if (area == null)
            {
                return NotFound();
            }
            return ViewComponent("EditAreaModal", area);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Area model)
        {
            var areaToEdit = await _context.Areas
                .Include(e => e.Coordinator)
                .FirstOrDefaultAsync(e => e.AreaId == model.AreaId);
            if (areaToEdit == null)
            {
                TempData["ErrorMessage"] = "No se encontró el área a editar.";
                return RedirectToAction("Index");
            }

            areaToEdit.Name = model.Name;
            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Área editada correctamente.";
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] = "Ocurrió un error al editar el área.";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Ocurrió un error al editar el área.";
                return RedirectToAction("Index");
            }
            return RedirectToAction("Index");
        }

        // POST: Crear área
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Area model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            ModelState.Remove("AreaId");
            ModelState.Remove("Coordinator");
            ModelState.Remove("CoordinatorId");
            ModelState.Remove("Status");

            if (!ModelState.IsValid)
            {
                return RedirectToAction("Index");
            }

            var newArea = new Area
            {
                Name = model.Name,
                CoordinatorId = userId,
                Status = "ACTIVE"
            };

            try
            {
                _context.Areas.Add(newArea);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Área creada correctamente.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Ocurrió un error al crear el área.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var area = await _context.Areas.FindAsync(id);
            if (area == null)
            {
                return NotFound();
            }
            area.Status = "INACTIVE";
            _context.Update(area);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Área eliminada correctamente.";
            return RedirectToAction("Index");
        }
    }
}
