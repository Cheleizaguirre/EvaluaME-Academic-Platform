using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mudul.Data;
using Mudul.EntityModels;
using Mudul.Models;
using Newtonsoft.Json;
using OfficeOpenXml;
using System.Security.Claims;
using System.IO;
using NPOI.HSSF.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;
using System.Linq;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;

namespace Mudul.Controllers
{
    [Authorize(Roles = "Teacher, Coordinator,Admin")]
    public class EnrollmentController : Controller
    {
        private readonly ApplicationDbContext _contextIdentity;
        private readonly DefaultdbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IServiceProvider _serviceProvider;

        public EnrollmentController(DefaultdbContext context, UserManager<IdentityUser> userManager, ApplicationDbContext contextIdentity, IServiceProvider serviceProvider)
        {
            _context = context;
            _contextIdentity = contextIdentity;
            _userManager = userManager;
            _serviceProvider = serviceProvider;
        }

        // GET: EnrollmentController
        public async Task<IActionResult> Index(EnrollmentViewModel? model = null)
        {
            if (User.IsInRole("Coordinator"))
            {
                var userCoordinator = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var subjectsCoordinator = await _context.Subjects
                    .Where(s => s.Area.CoordinatorId == userCoordinator)
                    .ToListAsync();
                if (model == null)
                {
                    model = new EnrollmentViewModel { Subjects = subjectsCoordinator, Students = new List<List<string>>() };
                }
                else
                {
                    model.Subjects = subjectsCoordinator;
                }
                return View(model);
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var subjects = await _context.Subjects
                                          .Where(s => s.TeacherId == userId)
                                          .ToListAsync();

            if (model == null)
            {
                model = new EnrollmentViewModel { Subjects = subjects, Students = new List<List<string>>() };
            }
            else
            {
                model.Subjects = subjects;
            }

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOneStudent(StudentModel student, int subjectId)
        {
            // Obtener el rol "Student"
            var studentRole = await _contextIdentity.Roles
                .Where(r => r.Name == "Student")
                .FirstOrDefaultAsync() ?? throw new Exception("El rol 'Student' no existe.");

            var user = _context.AspNetUsers.FirstOrDefault(u => u.NationalId == student.NationalId);

            //Si el usuario existe, no se crea. Solo se matricula
            if (user != null)
            {
                // Matricular estudiante en la materia
                var enrollmentExistedStudent = new Enrollment
                {
                    StudentId = user.Id,
                    SubjectId = subjectId,
                    EnrollmentDate = DateTime.UtcNow,
                    Status = "ACTIVE"
                };
                _context.Enrollments.Add(enrollmentExistedStudent);
                await _context.SaveChangesAsync();
                var subjectEnrolled = _context.Subjects.FirstOrDefault(e => e.SubjectId == subjectId);
                TempData["SuccessMessage"] = $"Alumno matriculado en [{subjectEnrolled.Name}] exitosamente";
                return RedirectToAction("Index");
            }

            if(student.FullName.IsNullOrEmpty())
            {
                TempData["ErrorMessage"] = "El nombre del estudiante no puede estar vacío.";
                return RedirectToAction("Index");
            }

            var email = GenerarEmail(student.FullName, student.NationalId);

            var newUser = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true // Deshabilitar confirmación de email
            };

            var result = await _userManager.CreateAsync(newUser, "Alumno123.");
            if (!result.Succeeded)
            {
                return View();
            }

            await _userManager.AddToRoleAsync(newUser, studentRole.Name);

            var existingUser = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.UserName == newUser.UserName);
            if (existingUser != null)
            {
                existingUser.NationalId = student.NationalId;
            }
            else
            {
                _context.AspNetUsers.Add(new AspNetUser
                {
                    Id = newUser.Id,
                    UserName = newUser.UserName,
                    Email = newUser.Email,
                    NationalId = student.NationalId
                });
            }

            // Matricular estudiante en la materia
            var enrollment = new Enrollment
            {
                StudentId = newUser.Id,
                SubjectId = subjectId,
                EnrollmentDate = DateTime.UtcNow,
                Status = "ACTIVE"
            };

            _context.Enrollments.Add(enrollment);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Alumno registrado exitosamente";
            return RedirectToAction("Index");
        }

        // POST: UploadExcel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadExcel(IFormFile formFile)
        {
            if (formFile == null || formFile.Length == 0)
            {
                return View("Index");
            }

            var dataList = new List<List<string>>();

            using (var stream = new MemoryStream())
            {
                await formFile.CopyToAsync(stream);
                stream.Position = 0; // Reiniciar el stream

                IWorkbook workbook;

                // Verificar si es un .xls o un .xlsx
                if (formFile.ContentType == "application/vnd.ms-excel") // Archivo .xls
                {
                    workbook = new HSSFWorkbook(stream); // Usar NPOI para .xls
                }
                else if (formFile.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet") // Archivo .xlsx
                {
                    workbook = new XSSFWorkbook(stream); // Usar NPOI para .xlsx
                }
                else
                {
                    return BadRequest("Formato de archivo no soportado.");
                }

                var sheet = workbook.GetSheetAt(0);
                int rowCount = sheet.PhysicalNumberOfRows;

                int headerRow = 0;
                for (int row = 0; row < rowCount; row++)
                {
                    var cell = sheet.GetRow(row)?.GetCell(0);
                    if (cell != null && cell.ToString() == "DOCUMENTO")
                    {
                        headerRow = row;
                        break;
                    }
                }

                if (headerRow == 0) return View("Index", dataList);

                int idCol = 1, nameCol = 2;

                for (int row = headerRow + 1; row < rowCount; row++)
                {
                    var rowData = sheet.GetRow(row);
                    if (rowData == null) continue;

                    var identidad = rowData.GetCell(idCol)?.ToString();
                    var nombre = rowData.GetCell(nameCol)?.ToString();

                    if (!string.IsNullOrEmpty(identidad) && !string.IsNullOrEmpty(nombre))
                    {
                        dataList.Add(new List<string> { identidad, nombre });
                    }
                }
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Obtener el ID del docente desde el claim
            var subjects = await _context.Subjects
                                          .Where(s => s.TeacherId == userId) // Filtrar por el docente actual
                                          .ToListAsync();

            // Crear un modelo con las materias y los estudiantes
            var enrollmentViewModel = new EnrollmentViewModel
            {
                Subjects = subjects,
                Students = dataList
            };

            TempData["Students"] = JsonConvert.SerializeObject(dataList);
            return View("Index", enrollmentViewModel);
        }

        // POST: SaveStudents
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveStudents(int subjectId)
        {
            // Recuperar los datos de TempData
            var studentsJson = TempData["Students"] as string;
            if (string.IsNullOrEmpty(studentsJson))
            {
                TempData["ErrorMessage"] = "La lista de alumnos no se reconoce";
                return RedirectToAction("Index");
            }

            var studentsData = JsonConvert.DeserializeObject<List<List<string>>>(studentsJson);

            var studentRole = await _contextIdentity.Roles
                .Where(r => r.Name == "Student")
                .FirstOrDefaultAsync() ?? throw new Exception("El rol 'Student' no existe.");

            await Parallel.ForEachAsync(studentsData, async (item, _) =>
            {
                using var scope = _serviceProvider.CreateScope();
                var scopedContextIdentity = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var scopedContext = scope.ServiceProvider.GetRequiredService<DefaultdbContext>();
                var scopedUserManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

                var identidad = item[0];
                var nombre = item[1];
                var email = GenerarEmail(nombre, identidad);

                var existingIdentityUser = await scopedContextIdentity.Users.FirstOrDefaultAsync(u => u.UserName == email);

                if (existingIdentityUser == null)
                {
                    var newUser = new IdentityUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true
                    };

                    var result = await scopedUserManager.CreateAsync(newUser, "Alumno123.");

                    if (result.Succeeded)
                    {
                        await scopedUserManager.AddToRoleAsync(newUser, studentRole.Name);

                        var existingUser = await scopedContext.AspNetUsers.FirstOrDefaultAsync(u => u.UserName == newUser.UserName);
                        if (existingUser != null)
                        {
                            existingUser.NationalId = identidad;
                        }
                        else
                        {
                            scopedContext.AspNetUsers.Add(new AspNetUser
                            {
                                Id = newUser.Id,
                                UserName = newUser.UserName,
                                Email = newUser.Email,
                                NationalId = identidad
                            });
                        }

                        // Aquí agregar el estudiante a la matrícula
                        var enrollment = new Enrollment
                        {
                            StudentId = newUser.Id,
                            SubjectId = subjectId,
                            EnrollmentDate = DateTime.UtcNow,
                            Status = "ACTIVE"
                        };
                        scopedContext.Enrollments.Add(enrollment);

                        await scopedContext.SaveChangesAsync();
                    }
                }
                else
                {
                    var existingUser = await scopedContext.AspNetUsers.FirstOrDefaultAsync(u => u.UserName == existingIdentityUser.UserName);
                    if (existingUser != null)
                    {
                        existingUser.NationalId = identidad;
                        await scopedContext.SaveChangesAsync();
                    }

                    // Aquí agregar al estudiante a la matrícula
                    var enrollment = new Enrollment
                    {
                        StudentId = existingIdentityUser.Id,
                        SubjectId = subjectId,
                        EnrollmentDate = DateTime.UtcNow,
                        Status = "ACTIVE"
                    };
                    scopedContext.Enrollments.Add(enrollment);

                    await scopedContext.SaveChangesAsync();
                }
            });

            TempData["SuccessMessage"] = "Alumnos registrados exitosamente";
            return RedirectToAction("Index");
        }


        // POST: DeleteStudentEnrollment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStudentEnrollment(int enrollmentId)
        {
            var enrollment = await _context.Enrollments.Where(e => e.EnrollmentId == enrollmentId)
                .Include(e => e.Subject)
                .Include(e => e.Student)
                .FirstOrDefaultAsync();
            if (enrollment != null) {
                var subject = enrollment.Subject.Name;
                var student = enrollment.Student.UserName;
                _context.Enrollments.Remove(enrollment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{subject} se ha removido de {student} exitosamente";
            }
            else
            {
                TempData["ErrorMessage"] = "No se encontró la matrícula.";
            }
            return RedirectToAction("Index");
        }





        // E-mail generator for ExcelUpload
        private string GenerarEmail(string nombreCompleto, string identidad)
        {
            var partesNombre = nombreCompleto.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string primerNombre = partesNombre.Length > 0 ? partesNombre[0] : "";
            string primerApellido = partesNombre.Length > 2 ? partesNombre[2] : partesNombre.LastOrDefault() ?? "";

            string ultimos4Digitos = identidad.Length >= 4
                ? identidad.Substring(identidad.Length - 4)
                : identidad.PadLeft(4, '0');

            return $"{primerNombre.ToLower()}.{primerApellido.ToLower()}{ultimos4Digitos}@gmail.com";
        }


    }

}
