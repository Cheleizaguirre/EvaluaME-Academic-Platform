using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Mudul.Data;
using Mudul.EntityModels;
using System.Data;

namespace Mudul.Models
{
    public class StudentModel
    {

        public string UserId { get; set; }

        public string FullName { get; set; }

        public string NationalId { get; set; }

        public string PhoneNumber { get; set; }

        public string Email { get; set; }

        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    }
}
