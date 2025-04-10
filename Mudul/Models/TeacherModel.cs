using Microsoft.AspNetCore.Identity;
using Mudul.EntityModels;

namespace Mudul.Models
{
    public class TeacherModel
    {
        public string UserId { get; set; }
        public string FullName { get; set; }

        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        public string NationalId { get; set; }

        public bool IsChangingPassword { get; set; } = false;


        public virtual Subject SubjectToAssign { get; set; } = new Subject();

        public virtual ICollection<Subject> Subjects { get; set; } = new List<Subject>();
    }
}
