namespace Mudul.Models
{
    public class EnrollmentViewModel
    {
        public List<EntityModels.Subject> Subjects { get; set; } // Materias que el docente imparte
        public List<List<string>> Students { get; set; } // Estudiantes a inscribir
    }
}
