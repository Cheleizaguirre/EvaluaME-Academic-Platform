using Mudul.EntityModels;
namespace Mudul.Models
{
    public class SubjectModel
    {
        public int SubjectId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int? Year { get; set; }
        public Area Area { get; set; }
        public AspNetUser Teacher { get; set; }
        public string Status { get; set; } = "ACTIVE";
    }
}
