using Mudul.EntityModels;
using System.Security.Claims;

namespace Mudul.Models
{
    public class CoordinatorModel
    {

        public string UserId { get; set; }
        public string FullName { get; set; }

        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        public string NationalId { get; set; }

        public virtual ICollection<Area> Areas { get; set; }
    }
}
