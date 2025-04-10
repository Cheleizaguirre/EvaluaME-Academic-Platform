using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mudul.Data;
using Mudul.EntityModels;
using Mudul.Models; // Aquí se encuentra ExamAttemptViewModel y JobeLanguage
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mudul.Controllers
{
    [Authorize(Roles = "Student")]
    public class ExamSubmissionController : Controller
    {
        private readonly DefaultdbContext _context;

        public ExamSubmissionController(DefaultdbContext context)
        {
            _context = context;
        }

        // GET: ExamSubmission/Attempt?examId=...
        public async Task<IActionResult> Attempt(int examId)
        {
            // Cargar el examen y sus preguntas
            var exam = await _context.Exams
                .Include(e => e.ExamQuestions)
                .FirstOrDefaultAsync(e => e.ExamId == examId);
            if (exam == null)
                return NotFound();

            // Construir el ViewModel
            var viewModel = new ExamAttemptViewModel
            {
                Exam = exam,
                ExamQuestions = exam.ExamQuestions.ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Attempt(int examId, IFormCollection form)
        {
            // 1. Lectura de datos, exam, studentId, etc. (lo mismo que ya tienes)
            var startTimeString = form["StartTime"].ToString();
            DateTime startTime;
            if (!DateTime.TryParse(startTimeString, out startTime))
                startTime = DateTime.Now;

            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var exam = await _context.Exams
                .Include(e => e.ExamQuestions)
                .FirstOrDefaultAsync(e => e.ExamId == examId);
            if (exam == null)
                return NotFound();

            decimal totalGrade = 0;
            decimal totalPossible = exam.ExamQuestions.Sum(q => q.Weight ?? 0);
            var responses = new Dictionary<int, object>();

            foreach (var question in exam.ExamQuestions)
            {
                string answerKey = $"Answer_{question.QuestionId}";
                string weightKey = $"FinalWeight_{question.QuestionId}";

                // Respuesta enviada por el usuario
                string answer = form[answerKey].ToString().Trim();

                // Puntaje "tentativo" enviado desde el hidden
                decimal finalWeight;
                if (!decimal.TryParse(form[weightKey], out finalWeight))
                    finalWeight = question.Weight ?? 0;

                // VALIDACIÓN SERVER-SIDE:
                if (question.QuestionType == "codigo")
                {
                    // Si está vacía, 0 puntos
                    if (string.IsNullOrEmpty(answer))
                    {
                        finalWeight = 0;
                    }
                    else
                    {
                        // Llamar a JOBe para verificar
                        bool isCorrect = await ValidateCodeServerSideAsync(answer, question.JobeConfiguration);
                        if (!isCorrect)
                        {
                            finalWeight = 0;
                        }
                    }
                }
                else
                {
                    // Podrías validar otros tipos de preguntas, si así lo deseas
                    // ...
                }

                totalGrade += finalWeight;
                responses[question.QuestionId] = new
                {
                    Answer = answer,
                    FinalWeight = finalWeight
                };
            }

            string responseData = JsonConvert.SerializeObject(responses);

            var submissionTime = DateTime.Now;
            var timeTaken = submissionTime - startTime;

            // Calcular AttemptNumber
            int attemptNumber = 1;
            var lastAttempt = await _context.ExamSubmissions
                .Where(s => s.ExamId == examId && s.StudentId == studentId)
                .OrderByDescending(s => s.AttemptNumber)
                .FirstOrDefaultAsync();
            if (lastAttempt != null)
            {
                attemptNumber = (lastAttempt.AttemptNumber ?? 0) + 1;
            }

            var submission = new ExamSubmission
            {
                ExamId = examId,
                StudentId = studentId,
                SubmissionDate = submissionTime,
                ResponseData = responseData,
                GlobalResult = null,
                AttemptNumber = attemptNumber,
                Status = "ACTIVE"
            };

            _context.ExamSubmissions.Add(submission);
            await _context.SaveChangesAsync();

            decimal percentGrade = (totalPossible > 0) ? (totalGrade / totalPossible) * 100 : 0;
            TempData["ExamGrade"] = totalGrade.ToString("0.##");
            TempData["TotalPossible"] = totalPossible.ToString("0.##");
            TempData["PercentGrade"] = percentGrade.ToString("0.##");
            TempData["TimeTaken"] = string.Format("{0:hh\\:mm\\:ss}", timeTaken);
            TempData["AttemptNumber"] = attemptNumber.ToString();
            TempData["SubjectId"] = exam.SubjectId.ToString();
            TempData["SuccessMessage"] =
                $"Examen enviado correctamente. Nota obtenida: {totalGrade:0.##} de {totalPossible:0.##} ({percentGrade:0.##}%). " +
                $"Tiempo empleado: {string.Format("{0:hh\\:mm\\:ss}", timeTaken)}. Intento: {attemptNumber}.";

            return RedirectToAction("Result");
        }



        // GET: ExamSubmission/Result
        public IActionResult Result()
        {
            return View();
        }

        // (Opcional) GET: ExamSubmission/Index
        public async Task<IActionResult> Index()
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var submissions = await _context.ExamSubmissions
                .Include(s => s.Exam)
                .Where(s => s.StudentId == studentId && s.Status == "ACTIVE")
                .ToListAsync();
            return View(submissions);
        }

        private async Task<bool> ValidateCodeServerSideAsync(string sourceCode, string jsonConfig)
        {
            // 1. Parsear la configuración para extraer el expected_output
            if (string.IsNullOrWhiteSpace(jsonConfig))
                return false; // No hay config

            JObject configObj;
            try
            {
                configObj = JObject.Parse(jsonConfig);
            }
            catch
            {
                return false; // Config inválida => no pasa
            }

            // run_spec / expected_output
            var runSpec = configObj["run_spec"];
            if (runSpec == null || runSpec["expected_output"] == null)
                return false;

            string expectedOutput = runSpec["expected_output"]?.ToString();
            if (string.IsNullOrEmpty(expectedOutput))
                return false;

            // 2. Construir payload para JOBe
            JObject payload = new JObject
            {
                ["run_spec"] = new JObject
                {
                    ["language_id"] = "python3",
                    ["sourcecode"] = sourceCode,
                    ["stdin"] = "",
                    ["expected_output"] = expectedOutput,
                    ["max_cpu_time"] = 5,
                    ["max_memory"] = 64000000
                }
            };

            // 3. Llamar la API JOBe
            using (var client = new HttpClient())
            {
                // Cambia la URL base a la tuya
                string jobeUrl = "https://glowworm-chief-probably.ngrok-free.app/jobe/index.php/restapi/runs";
                var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");

                try
                {
                    var response = await client.PostAsync(jobeUrl, content);
                    string resultString = await response.Content.ReadAsStringAsync();
                    var resultJson = JObject.Parse(resultString);

                    // outcome === 15 => se considera correcto
                    int outcome = (int?)resultJson["outcome"] ?? 0;
                    return (outcome == 15);
                }
                catch
                {
                    // Si la llamada falla por tiempo de espera, error de red, etc.
                    return false;
                }
            }
        }




    }




}
