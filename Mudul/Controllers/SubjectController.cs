using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mudul.Data;
using Mudul.EntityModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Mudul.Controllers
{
    [Authorize(Roles = "Student,Teacher,Coordinator")]
    public class SubjectController : Controller
    {
        private readonly DefaultdbContext _context;

        public SubjectController(DefaultdbContext context)
        {
            _context = context;
        }

        // GET: SubjectController
        public ActionResult Index(int id)
        {
            // Obtener el ID del usuario actual
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Obtener la asignatura con sus exámenes y el profesor
            var subject = _context.Subjects
                .Include(s => s.Teacher)
                .Include(s => s.Exams)
                .FirstOrDefault(s => s.SubjectId == id);

            if (subject == null)
            {
                return NotFound();
            }

            // Verificar que el usuario tenga acceso
            bool userHasAccess = false;
            if (User.IsInRole("Teacher") && subject.TeacherId == userId)
            {
                userHasAccess = true;
            }
            else if (User.IsInRole("Student"))
            {
                var enrollment = _context.Enrollments
                    .FirstOrDefault(e => e.StudentId == userId && e.SubjectId == id);
                userHasAccess = (enrollment != null);
            }
            else if (User.IsInRole("Coordinator"))
            {
                userHasAccess = true;
            }

            if (!userHasAccess)
            {
                return Forbid();
            }

            // Modo edición desactivado por defecto
            ViewBag.ShowCreateExam = false;

            return View(subject); // Renderizamos la vista "Index" con la asignatura
        }

        // GET: SubjectController/Edit/5
        public ActionResult Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var subject = _context.Subjects
                .Include(s => s.Teacher)
                .Include(s => s.Exams)
                .FirstOrDefault(s => s.SubjectId == id);

            if (subject == null)
            {
                return NotFound();
            }

            bool userHasAccess = false;
            if (User.IsInRole("Teacher") && subject.TeacherId == userId)
            {
                userHasAccess = true;
            }
            else if (User.IsInRole("Coordinator"))
            {
                userHasAccess = true;
            }
            if (!userHasAccess)
            {
                return Forbid();
            }

            // Aquí activamos el modo edición para mostrar el botón de crear examen
            ViewBag.ShowCreateExam = true;

            // En vez de retornar una vista Edit distinta, retornamos la misma vista "Index"
            return View("Index", subject);
        }

        // POST: SubjectController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                var subject = _context.Subjects.FirstOrDefault(s => s.SubjectId == id);
                if (subject == null)
                {
                    return NotFound();
                }

                // Guardar la nueva descripción
                subject.Description = collection["description"];
                _context.Update(subject);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Descripción guardada exitosamente";
                return RedirectToAction("Index", new { id });
            }
            catch
            {
                TempData["ErrorMessage"] = "Algo salió mal, intenta más tarde";
                return View();
            }
        }

        // GET: SubjectController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: SubjectController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
