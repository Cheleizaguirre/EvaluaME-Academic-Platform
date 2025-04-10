using Microsoft.AspNetCore.Mvc.Rendering;

namespace Mudul.Models
{
    public class SubjectEditViewModel : SubjectModel
    {
        public string TeacherId { get; set; }
        public List<SelectListItem> Teachers { get; set; }
    }
}
