using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mudul.EntityModels;
using Mudul.Models; // Asegúrate que JobeLanguage y ValidateCodeRequest estén definidos/visibles
using Mudul.ViewModels;
using Newtonsoft.Json;

namespace Mudul.Controllers
{
    [Authorize(Roles = "Coordinator,Admin,Teacher")]
    public class ExamQuestionsController : Controller
    {
        private readonly DefaultdbContext _context;

        public ExamQuestionsController(DefaultdbContext context)
        {
            _context = context;
        }
        


        //----------------------------------------------------------------------
        // 1. BANCO DE PREGUNTAS (ExamId = 0)
        //----------------------------------------------------------------------

        // GET: ExamQuestions/BankIndex
        // Muestra todas las preguntas que tienen ExamId = 0 (no asignadas a ningún examen).
        public async Task<IActionResult> BankIndex()
        {
            // Obtenemos las preguntas donde ExamId=0 y Status=ACTIVE
            var bankQuestions = await _context.ExamQuestions
                .Where(q => q.ExamId == 0 && q.Status == "ACTIVE")
                .ToListAsync();

            return View(bankQuestions); // Vista: /Views/ExamQuestions/BankIndex.cshtml
        }

        // GET: ExamQuestions/BankCreate
        // Muestra el formulario para crear una pregunta en el banco (ExamId=0).
        public async Task<IActionResult> BankCreate()
        {
            // Si necesitas JOBe, cargar la lista de lenguajes:
            // var languages = await GetLanguagesAsync();
            // ViewBag.Languages = languages;

            // Creamos una pregunta sin examen asignado (ExamId=0).
            var model = new ExamQuestion
            {
                ExamId = 0,
                Status = "ACTIVE"
            };
            return View(model); // Vista: /Views/ExamQuestions/BankCreate.cshtml
        }

        // GET: ExamQuestions/Create?examId=...
        public async Task<IActionResult> Create(int examId)
        {
            // Crea el modelo con el examId y estado activo.
            var examQuestion = new ExamQuestion
            {
                ExamId = examId,
                Status = "ACTIVE"
            };

            // Carga la lista de lenguajes para el select (usado en preguntas de tipo "codigo").
            var languages = await GetLanguagesAsync();
            ViewBag.Languages = languages;

            return View(examQuestion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
     [Bind("QuestionId,ExamId,QuestionText,QuestionType,Weight,Status")] ExamQuestion examQuestion)
        {
            // Quitar la validación de la propiedad "Exam" que no se envía en el formulario.
            ModelState.Remove("Exam");

            // Verifica que ExamId sea mayor que 0 y que el examen exista
            if (examQuestion.ExamId <= 0)
            {
                ModelState.AddModelError("ExamId", "ExamId inválido o examen no existe.");
            }
            else if (!await _context.Exams.AnyAsync(e => e.ExamId == examQuestion.ExamId))
            {
                ModelState.AddModelError("ExamId", "El examen especificado no existe.");
            }

            // Si la pregunta es de tipo "codigo", obtener la configuración validada desde el formulario
            if (examQuestion.QuestionType == "codigo")
            {
                var validatedConfig = Request.Form["validatedConfiguration"].ToString();
                System.Diagnostics.Debug.WriteLine("Validated Config: " + validatedConfig);
                if (string.IsNullOrWhiteSpace(validatedConfig))
                {
                    ModelState.AddModelError("JobeConfiguration", "Debe validar el código antes de guardar la pregunta.");
                }
                else
                {
                    examQuestion.JobeConfiguration = validatedConfig;
                }
            }
            else
            {
                examQuestion.JobeConfiguration = null;
            }

            if (ModelState.IsValid)
            {
                examQuestion.Status = "ACTIVE";
                _context.Add(examQuestion);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Pregunta guardada correctamente";
                return RedirectToAction(nameof(Index), new { examId = examQuestion.ExamId });
            }

            // Si hay errores, recargar la lista de lenguajes para que el select se llene
            var languages = await GetLanguagesAsync();
            ViewBag.Languages = languages;

            return View(examQuestion);
        }



        //----------------------------------------------------------------------
        // 2. PREGUNTAS ASIGNADAS A UN EXAMEN (ExamId > 0)
        //----------------------------------------------------------------------

        // GET: ExamQuestions?examId=...
        // Lista las preguntas de un examen en particular (ExamId>0).
        public async Task<IActionResult> Index(int? examId)
        {
            if (examId == null || examId == 0)
            {
                // BANCO DE PREGUNTAS
                var bankQuestions = await _context.ExamQuestions
                    .Where(q => q.ExamId == 0 && q.Status == "ACTIVE")
                    .ToListAsync();

                // Armamos el ViewModel con Exam = null para indicar "banco"
                var vm = new ExamQuestionsViewModel
                {
                    Exam = null, // null => no hay examen
                    ExamQuestions = bankQuestions
                };

                // Usaremos la misma vista "Index.cshtml"
                return View("Index", vm);
            }
            else
            {
                // Preguntas de un examen específico
                var exam = await _context.Exams.FirstOrDefaultAsync(e => e.ExamId == examId);
                if (exam == null) return NotFound();

                var examQuestions = await _context.ExamQuestions
                    .Where(e => e.ExamId == examId && e.Status == "ACTIVE")
                    .ToListAsync();

                var vm = new ExamQuestionsViewModel
                {
                    Exam = exam,
                    ExamQuestions = examQuestions
                };

                return View("Index", vm);
            }
        }


    


        //----------------------------------------------------------------------
        // 3. VALIDACIÓN DE CÓDIGO (AJAX)
        //----------------------------------------------------------------------

        // Recibe el JSON con { selectedLanguage, sourceCode } y arma la config para JOBe
        [HttpPost]
        public async Task<IActionResult> ValidateCode([FromBody] ValidateCodeRequest request)
        {
            // Validar que tengamos al menos algo en sourceCode
            if (string.IsNullOrWhiteSpace(request.sourceCode))
            {
                return Json(new { success = false, message = "Debe ingresar el código fuente." });
            }

            // Construimos el run_spec
            var runSpec = new
            {
                language_id = string.IsNullOrWhiteSpace(request.selectedLanguage) ? "python3" : request.selectedLanguage,
                sourcecode = request.sourceCode,
                stdin = "",
                expected_output = "",
                max_cpu_time = 5,
                max_memory = 64000000
            };
            var payload = new { run_spec = runSpec };

            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var content = new StringContent(
                        JsonConvert.SerializeObject(payload),
                        System.Text.Encoding.UTF8,
                        "application/json"
                    );

                    // Llamada a la API de JOBe
                    var response = await client.PostAsync("https://glowworm-chief-probably.ngrok-free.app/jobe/index.php/restapi/runs", content);
                    if (response.IsSuccessStatusCode)
                    {
                        string respJson = await response.Content.ReadAsStringAsync();
                        var jobeResp = JsonConvert.DeserializeObject<JobeResponse>(respJson);

                        var realOutput = jobeResp?.stdout ?? "";
                        var finalConfig = new
                        {
                            run_spec = new
                            {
                                language_id = runSpec.language_id,
                                sourcecode = request.sourceCode,
                                stdin = "",
                                expected_output = realOutput,
                                max_cpu_time = 5,
                                max_memory = 64000000
                            }
                        };
                        var configJson = JsonConvert.SerializeObject(finalConfig, Formatting.Indented);

                        return Json(new { success = true, configuration = configJson });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Error al ejecutar el código en JOBe." });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Excepción: " + ex.Message });
            }
        }

        //----------------------------------------------------------------------
        // 4. DETALLES / EDITAR / ELIMINAR (compartido para banco o examen)
        //----------------------------------------------------------------------

        // GET: ExamQuestions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var examQuestion = await _context.ExamQuestions.FirstOrDefaultAsync(m => m.QuestionId == id);
            if (examQuestion == null) return NotFound();

            return View(examQuestion); // /Views/ExamQuestions/Details.cshtml
        }

        // GET: ExamQuestions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var examQuestion = await _context.ExamQuestions.FindAsync(id);
            if (examQuestion == null) return NotFound();

            return View(examQuestion); // /Views/ExamQuestions/Edit.cshtml
        }

        // POST: ExamQuestions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("QuestionId,ExamId,QuestionText,QuestionType,JobeConfiguration,Weight,Status")] ExamQuestion examQuestion)
        {
            if (id != examQuestion.QuestionId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    examQuestion.Status = "ACTIVE";
                    _context.Update(examQuestion);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ExamQuestionExists(examQuestion.QuestionId))
                        return NotFound();
                    else
                        throw;
                }

                // Si es del banco (ExamId=0), regresamos al banco; sino, a la vista del examen.
                if (examQuestion.ExamId == 0)
                    return RedirectToAction(nameof(BankIndex));
                else
                    return RedirectToAction(nameof(Index), new { examId = examQuestion.ExamId });
            }

            // Si algo falla, recargamos la vista
            return View(examQuestion);
        }

        // GET: ExamQuestions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var examQuestion = await _context.ExamQuestions
                .FirstOrDefaultAsync(m => m.QuestionId == id);
            if (examQuestion == null) return NotFound();

            return View(examQuestion); // /Views/ExamQuestions/Delete.cshtml
        }

        // POST: ExamQuestions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var examQuestion = await _context.ExamQuestions.FindAsync(id);
            if (examQuestion != null)
            {
                examQuestion.Status = "INACTIVE";
                _context.ExamQuestions.Update(examQuestion);
                await _context.SaveChangesAsync();
            }

            if (examQuestion?.ExamId == 0)
                return RedirectToAction(nameof(BankIndex));
            else
                return RedirectToAction(nameof(Index), new { examId = examQuestion?.ExamId });
        }

        private bool ExamQuestionExists(int id)
        {
            return _context.ExamQuestions.Any(e => e.QuestionId == id);
        }

        //----------------------------------------------------------------------
        // (Opcional) Lista de lenguajes si usas JOBe
        //----------------------------------------------------------------------
        private async Task<List<JobeLanguage>> GetLanguagesAsync()
        {
            var languages = new List<JobeLanguage>();
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var response = await client.GetAsync("https://glowworm-chief-probably.ngrok-free.app/jobe/index.php/restapi/languages");
                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        var langArrays = JsonConvert.DeserializeObject<List<List<string>>>(json);
                        languages = langArrays
                            .Select(a => new JobeLanguage { language_id = a[0], version = a[1] })
                            .ToList();
                    }
                }
            }
            catch
            {
                languages = new List<JobeLanguage>();
            }
            return languages;
        }
    }

    // -------------------------------------------------------------------------
    // Clases auxiliares (puedes ponerlas en archivos separados si gustas).
    // -------------------------------------------------------------------------
    public class JobeResponse
    {
        public int? run_id { get; set; }
        public int outcome { get; set; }
        public string cmpinfo { get; set; }
        public string stdout { get; set; }
        public string stderr { get; set; }
    }

    public class ValidateCodeRequest
    {
        public string selectedLanguage { get; set; }
        public string sourceCode { get; set; }
    }
}
