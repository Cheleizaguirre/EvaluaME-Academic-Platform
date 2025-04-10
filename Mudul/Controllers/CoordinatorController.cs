using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Mudul.Data;
using Mudul.EntityModels;
using Mudul.Models;
using System.Security.Claims;

namespace Mudul.Controllers
{
    [Authorize(Roles = "Coordinator")]
    public class CoordinatorController : Controller
    {
        private readonly ApplicationDbContext _contextIdentity;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly DefaultdbContext _context;
        private readonly SignInManager<IdentityUser> _signInManager;

        public CoordinatorController(DefaultdbContext context, UserManager<IdentityUser> userManager, ApplicationDbContext contextIdentity, SignInManager<IdentityUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _contextIdentity = contextIdentity;
            _signInManager = signInManager;
        }

        // GET: CoordinatorController
        public async Task<IActionResult> Index()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = _context.AspNetUsers
                .Include(a => a.Areas)
                .FirstOrDefault(u => u.Id == currentUserId);
            var coordinatorModel = new CoordinatorModel
            {
                UserId = currentUser.Id,
                FullName = currentUser.UserName,
                Email = currentUser.Email,
                PhoneNumber = currentUser.PhoneNumber,
                NationalId = currentUser.NationalId,
                Areas = currentUser.Areas
            };
            return View(coordinatorModel);
        }

        // GET: CoordinatorController/Profile
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _context.AspNetUsers.Where(e => e.Id == userId).Include(e => e.Roles).FirstOrDefault();
            var userViewModel = new UserEditViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Roles.FirstOrDefault().Name
            };
            return View(userViewModel);
        }

        // POST: CoordinatorController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string Id, string UserName, string Email, string PhoneNumber, string Password, string ConfirmPassword)
        {
            var user = await _context.AspNetUsers.FindAsync(Id);
            if (user == null)
            {
                return NotFound();
            }

            // Actualizar datos del usuario con _context
            user.UserName = UserName;
            user.Email = Email;
            user.PhoneNumber = PhoneNumber;

            if (!string.IsNullOrEmpty(Password) && Password == ConfirmPassword)
            {
                var identityUser = await _userManager.FindByIdAsync(Id);
                if (identityUser != null)
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(identityUser);
                    await _userManager.ResetPasswordAsync(identityUser, token, Password);
                }
            }

            try
            {
                _context.Update(user);
                await _context.SaveChangesAsync();

                // Refrescar sesión si el usuario editado es el usuario logueado actualmente
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null && currentUser.Id == Id)
                {
                    await _signInManager.RefreshSignInAsync(currentUser);
                }

                TempData["SuccessMessage"] = "Perfil actualizado correctamente.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Hubo un error al actualizar el perfil.";
            }

            return RedirectToAction("Profile");
        }

        // GET: CoordinatorController/TeacherManagement
        public async Task<IActionResult> TeacherManagement()
        {
            var TeacherList = _context.AspNetUsers
                                .Where(e => e.Roles.Any(r => r.Name == "Teacher"))
                .Select(e => new TeacherModel
                {
                    UserId = e.Id,
                    FullName = e.UserName,
                    Email = e.Email,
                    PhoneNumber = e.PhoneNumber,
                    NationalId = e.NationalId,
                    Subjects = _context.Subjects.Where(s => s.TeacherId == e.Id).ToList()
                }).ToList();

            return View(TeacherList);

        }

        // GET: Modal for Edit Teacher Profile
        public async Task<IActionResult> TeacherEditModal(string id)
        {
            var teacher = await _context.AspNetUsers
                .Where(e => e.Id == id && e.Roles.Any(r => r.Name == "Teacher"))
                .Select(e => new TeacherModel
                {
                    UserId = e.Id,
                    FullName = e.UserName,
                    Email = e.Email,
                    PhoneNumber = e.PhoneNumber,
                    NationalId = e.NationalId,
                    Subjects = _context.Subjects.Where(s => s.TeacherId == e.Id).ToList()
                })
                .FirstOrDefaultAsync();

            if (teacher == null)
            {
                return NotFound();
            }

            return ViewComponent("EditTeacherModal", new { teacher });
        }

        // POST: CoordinatorController/TeacherEdit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TeacherEdit(TeacherModel model)
        {
            ModelState.Remove("PhoneNumber");
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Datos inválidos. Verifica el formulario.";
                return RedirectToAction("TeacherManagement");
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Docente no encontrado.";
                return RedirectToAction("TeacherManagement");
            }

            // Establecer UserName como email
            user.UserName = model.Email;
            user.NormalizedUserName = model.Email.ToUpper();

            user.Email = model.Email;
            user.NormalizedEmail = model.Email.ToUpper();
            user.PhoneNumber = model.PhoneNumber;

            // Actualizar con UserManager (valida internamente)
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                TempData["ErrorMessage"] = "Hubo un error al actualizar el docente.";
                return RedirectToAction("TeacherManagement");
            }

            // Actualizar en tabla extendida
            var extendedUser = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.Id == user.Id);
            if (extendedUser != null)
            {
                extendedUser.NationalId = model.NationalId;
                extendedUser.UserName = model.FullName;
            }

            // Reiniciar contraseña si está marcado
            if (model.IsChangingPassword)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, "Docente123.");
                if (!passwordResult.Succeeded)
                {
                    TempData["ErrorMessage"] = "No se pudo reiniciar la contraseña.";
                    return RedirectToAction("TeacherManagement");
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Docente actualizado exitosamente.";
            return RedirectToAction("TeacherManagement");
        }

        // GET: Modal for creating a new teacher
        public IActionResult TeacherCreateModal()
        {
            return ViewComponent("TeacherCreateModal");
        }

        // POST: CoordinatorController/TeacherCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTeacher(TeacherModel teacher)
        {
            // Obtener el rol "Teacher"
            var teacherRole = await _contextIdentity.Roles
                .FirstOrDefaultAsync(r => r.Name == "Teacher")
                ?? throw new Exception("El rol 'Teacher' no existe.");

            var user = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.NationalId == teacher.NationalId);

            // Si el usuario ya existe, se asigna a la materia
            if (user != null)
            {
                var subjectToAssign = await _context.Subjects.FindAsync(teacher.SubjectToAssign.SubjectId);
                if (subjectToAssign == null)
                {
                    TempData["ErrorMessage"] = "La materia especificada no existe.";
                    return RedirectToAction("TeacherManagement");
                }

                subjectToAssign.TeacherId = user.Id;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Docente asignado exitosamente a la clase [{subjectToAssign.Name}]";
                return RedirectToAction("TeacherManagement");
            }

            // Validar nombre
            if (string.IsNullOrWhiteSpace(teacher.FullName))
            {
                TempData["ErrorMessage"] = "El nombre del docente no puede estar vacío.";
                return RedirectToAction("TeacherManagement");
            }

            // Generar email desde nombre + DNI
            var email = GenerarEmail(teacher.FullName, teacher.NationalId);

            // Crear usuario en Identity
            var newUser = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(newUser, "Alumno123.");
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "Error al crear el usuario del docente.";
                return RedirectToAction("TeacherManagement");
            }

            await _userManager.AddToRoleAsync(newUser, teacherRole.Name);

            // Guardar en AspNetUsers (datos extendidos), evitando duplicados
            var extendedUser = await _context.AspNetUsers.FindAsync(newUser.Id);
            if (extendedUser == null)
            {
                _context.AspNetUsers.Add(new AspNetUser
                {
                    Id = newUser.Id,
                    UserName = newUser.UserName,
                    Email = newUser.Email,
                    NationalId = teacher.NationalId,
                    PhoneNumber = teacher.PhoneNumber
                });
            }
            else
            {
                extendedUser.NationalId = teacher.NationalId;
                extendedUser.PhoneNumber = teacher.PhoneNumber;
            }

            // Asignar el docente a la materia
            var subject = await _context.Subjects.FindAsync(teacher.SubjectToAssign.SubjectId);
            if (subject == null)
            {
                TempData["ErrorMessage"] = "La materia especificada no existe.";
                return RedirectToAction("TeacherManagement");
            }

            subject.TeacherId = newUser.Id;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Docente registrado y asignado a la clase [{subject.Name}] exitosamente.";
            return RedirectToAction("TeacherManagement");
        }

        // POST: CoordinadorController/TeacherDelete
        [HttpPost]
        public async Task<IActionResult> TeacherDelete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            var userCoordinator = User.FindFirstValue(ClaimTypes.NameIdentifier);


            var subjectToDelete = await _context.Subjects
                .Where(s => s.TeacherId == id)
                .ToListAsync();

            foreach (var subject in subjectToDelete)
            {
                subject.TeacherId = userCoordinator;
            }



            await _context.SaveChangesAsync();

            var userRoles = await _userManager.GetRolesAsync(user);
            if (userRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, userRoles);
            }


            await _userManager.DeleteAsync(user);


            return Ok();
        }

        // GET: CoordinatorController/StudentManagement
        public async Task<IActionResult> StudentManagement()
        {
            var studentList = _context.AspNetUsers
                .Where(e => e.Roles.Any(r => r.Name == "Student"))
                .Select(e => new StudentModel
                {
                    UserId = e.Id,
                    FullName = e.UserName,
                    Email = e.Email,
                    PhoneNumber = e.PhoneNumber,
                    NationalId = e.NationalId,
                    Enrollments = _context.Enrollments.Where(s => s.StudentId == e.Id).ToList()
                }).ToList();

            return View(studentList);
        }

        // GET: Modal for editing a student
        public async Task<IActionResult> StudentEditModal(string id)
        {
            var student = await _context.AspNetUsers
                .Where(e => e.Id == id && e.Roles.Any(r => r.Name == "Student"))
                .Select(e => new StudentModel
                {
                    UserId = e.Id,
                    FullName = e.UserName,
                    Email = e.Email,
                    PhoneNumber = e.PhoneNumber,
                    NationalId = e.NationalId,
                    Enrollments = _context.Enrollments.Where(s => s.StudentId == e.Id)
                    .Include(e => e.Subject)
                    .Include(e => e.Subject.Teacher)
                    .ToList()
                })
                .FirstOrDefaultAsync();
            if (student == null)
            {
                return NotFound();
            }
            return ViewComponent("EditStudentModal", new { student });
        }

        // POST: CoordinatorController/StudentEdit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StudentEdit(StudentModel student)
        {
            ModelState.Remove("PhoneNumber");
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Datos inválidos. Verifica el formulario.";
                return RedirectToAction("StudentManagement");
            }
            var user = await _userManager.FindByIdAsync(student.UserId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Estudiante no encontrado.";
                return RedirectToAction("StudentManagement");
            }
            // Establecer UserName como email
            user.UserName = student.Email;
            user.NormalizedUserName = student.Email.ToUpper();
            user.Email = student.Email;
            user.NormalizedEmail = student.Email.ToUpper();
            user.PhoneNumber = student.PhoneNumber;
            // Actualizar con UserManager (valida internamente)
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                TempData["ErrorMessage"] = "Hubo un error al actualizar el estudiante.";
                return RedirectToAction("StudentManagement");
            }
            // Actualizar en tabla extendida
            var extendedUser = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.Id == user.Id);
            if (extendedUser != null)
            {
                extendedUser.NationalId = student.NationalId;
                extendedUser.UserName = student.FullName;
            }
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Estudiante actualizado exitosamente.";
            return RedirectToAction("StudentManagement");
        }

        // POST: CoordinatorController/StudentDelete
        [HttpPost]
        public async Task<IActionResult> StudentDelete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Eliminar entregas de exámenes
            var submissions = await _context.ExamSubmissions
                .Where(s => s.StudentId == id)
                .ToListAsync();

            _context.ExamSubmissions.RemoveRange(submissions);

            // Eliminar matrículas
            var enrollments = await _context.Enrollments
                .Where(e => e.StudentId == id)
                .ToListAsync();

            _context.Enrollments.RemoveRange(enrollments);

            await _context.SaveChangesAsync();

            // Quitar roles antes de eliminar el usuario
            var userRoles = await _userManager.GetRolesAsync(user);
            if (userRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, userRoles);
            }

            // Finalmente, eliminar el usuario
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok();
        }

        // GET: CoordinatorController/SubjectManagement
        public async Task<IActionResult> SubjectManagement()
        {
            var userCoordinator = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var subjectList = await _context.Subjects
                .Where(s => s.Area.CoordinatorId == userCoordinator)
                .Include(e => e.Teacher)
                .Include(e => e.Area)
                .Select(
                s => new SubjectModel
                {
                    SubjectId = s.SubjectId,
                    Name = s.Name,
                    Description = s.Description,
                    Year = s.Year,
                    Area = s.Area,
                    Teacher = s.Teacher
                }).ToListAsync();
            return View(subjectList);
        }

        // GET: Modal for editing a subject
        public async Task<IActionResult> SubjectEditModal(int id)
        {
            var subject = await _context.Subjects
                .Include(s => s.Teacher)
                .FirstOrDefaultAsync(s => s.SubjectId == id);

            if (subject == null)
            {
                return NotFound();
            }

            return ViewComponent("EditSubjectModal", new { id });
        }

        // POST: CoordinatorController/SubjectEdit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubjectEdit(SubjectEditViewModel model)
        {
            ModelState.Remove("Area");
            ModelState.Remove("Year");
            ModelState.Remove("Teacher");
            ModelState.Remove("Teachers");
            if (!ModelState.IsValid)
            {
                // Si el modelo no es válido, puede ser útil volver a cargar los Teachers
                // en caso quieras reutilizar la vista con errores
                model.Teachers = _context.AspNetUsers
                    .Where(u => u.Roles.Any(r => r.Name == "Teacher"))
                    .Select(u => new SelectListItem
                    {
                        Value = u.Id,
                        Text = u.UserName
                    }).ToList();

                return PartialView("Components/SubjectEditModal/Default", model);
            }

            var subject = await _context.Subjects.FindAsync(model.SubjectId);
            if (subject == null)
            {
                TempData["ErrorMessage"] = "La clase no fue encontrada.";
                return RedirectToAction("Subjects");
            }

            subject.Name = model.Name;
            subject.Description = model.Description;
            subject.Year = model.Year ?? DateTime.Now.Year;

            // Asignar docente si se proporcionó uno
            if (!string.IsNullOrEmpty(model.TeacherId))
            {
                subject.TeacherId = model.TeacherId;
            }

            try
            {
                _context.Subjects.Update(subject);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Clase actualizada correctamente.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ocurrió un error al actualizar la clase.";
                // Opcional: registrar el error con logger
            }

            return RedirectToAction("SubjectManagement");
        }

        // GET: Modal for creating a new subject
        public IActionResult SubjectCreateModal()
        {
            var model = new SubjectModel();
            return ViewComponent("SubjectCreateModal", model);
        }

        // POST: Create a new subject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSubject(SubjectModel model)
        {
            ModelState.Remove("SubjectId");
            ModelState.Remove("Year");
            ModelState.Remove("Teacher");
            ModelState.Remove("Area");

            var userCoordinator = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var area = await _context.Areas
                .FirstOrDefaultAsync(a => a.CoordinatorId == userCoordinator);

            if (!ModelState.IsValid)
            {
                
                return ViewComponent("SubjectCreateModal", model);
            }

            var newSubject = new Subject
            {
                Name = model.Name,
                Description = model.Description,
                Year = model.Year ?? DateTime.Now.Year,
                Area = area,
                TeacherId = userCoordinator,
                Status = "ACTIVE"
            };

            try
            {
                _context.Subjects.Add(newSubject);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Clase creada correctamente.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ocurrió un error al crear la clase.";
            }

            return RedirectToAction("SubjectManagement");
        }

        // POST: CoordinatorController/SubjectDelete
        [HttpPost]
        public async Task<IActionResult> SubjectDelete(int id)
        {
            var subject = await _context.Subjects
                .Include(s => s.Enrollments)
                .Include(s => s.Exams)
                .FirstOrDefaultAsync(s => s.SubjectId == id);

            if (subject == null)
            {
                return NotFound();
            }

            // Validación: No se puede eliminar si tiene exámenes
            if (subject.Exams.Any())
            {
                return BadRequest("No se puede eliminar esta clase porque ya tiene exámenes registrados.");
            }

            // Eliminar las inscripciones asociadas
            if (subject.Enrollments.Any())
            {
                _context.Enrollments.RemoveRange(subject.Enrollments);
            }

            // Eliminar la clase
            _context.Subjects.Remove(subject);
            await _context.SaveChangesAsync();

            return Ok();
        }


        // E-mail generator
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
