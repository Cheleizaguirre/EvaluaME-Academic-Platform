using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mudul.EntityModels;

namespace Mudul.Controllers
{
    public class ExamsController : Controller
    {
        private readonly DefaultdbContext _context;

        public ExamsController(DefaultdbContext context)
        {
            _context = context;
        }

        // GET: Exams/Create?subjectId=5&subjectName=Matemáticas
        public IActionResult Create(int? subjectId, string subjectName)
        {
            var exam = new Exam();

            if (subjectId.HasValue)
            {
                exam.SubjectId = subjectId.Value;
            }

            // Asigna TeacherId y TeacherName según el rol
            if (User.IsInRole("Teacher"))
            {
                exam.TeacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                ViewBag.TeacherName = User.Identity.Name;
            }
            else if (User.IsInRole("Coordinator") || User.IsInRole("Admin"))
            {
                // Otra lógica, si es necesaria.
            }

            // Asigna el nombre de la asignatura en ViewBag
            if (!string.IsNullOrWhiteSpace(subjectName))
            {
                ViewBag.SubjectName = subjectName;
                exam.Title = $"Examen de {subjectName}";
            }
            else
            {
                ViewBag.SubjectName = string.Empty;
                exam.Title = "Examen";
            }

            // Valor por defecto para ExamType y Status
            exam.ExamType = "Jobe";
            exam.Status = "ACTIVE";

            return View(exam);
        }


        // POST: Exams/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ExamId,SubjectId,TeacherId,Title,Description,PublishDate,ExamType,TimeLimit,H5pcontentId,Status")] Exam exam)
        {
            // Removemos propiedades de navegación que no vienen en el formulario
            ModelState.Remove("Subject");
            ModelState.Remove("Teacher");

            if (ModelState.IsValid)
            {
                _context.Add(exam);
                await _context.SaveChangesAsync();

                // Redirige a ExamQuestions/Create pasando examId para crear las preguntas asociadas
                return RedirectToAction("Create", "ExamQuestions", new { examId = exam.ExamId });
            }
            return View(exam);
        }


        // GET: Exams/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var exam = await _context.Exams.FindAsync(id);
            if (exam == null)
            {
                return NotFound();
            }
            return View(exam);
        }

        // POST: Exams/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ExamId,SubjectId,TeacherId,Title,Description,PublishDate,ExamType,TimeLimit,H5PContentId,Status")] Exam exam)
        {
            if (id != exam.ExamId)
            {
                return NotFound();
            }

            ModelState.Remove("Subject");
            ModelState.Remove("Teacher");
            if (ModelState.IsValid)
            {
                try
                {
                    // Establecer el valor de Status en "ACTIVE"
                    exam.Status = "ACTIVE";

                    _context.Update(exam);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ExamsExists(exam.ExamId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(exam);
        }


        // GET: Exams/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var exam = await _context.Exams
                .FirstOrDefaultAsync(m => m.ExamId == id);
            if (exam == null)
            {
                return NotFound();
            }

            return View(exam);
        }

        // POST: Exams/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam != null)
            {
                exam.Status = "INACTIVE";
                _context.Exams.Update(exam);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }


        private bool ExamsExists(int id)
        {
            return _context.Exams.Any(e => e.ExamId == id);
        }
    }
}
