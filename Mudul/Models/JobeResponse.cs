namespace Mudul.Models
{
    public class JobeResponse
    {
        public int? run_id { get; set; }
        public int outcome { get; set; }
        public string cmpinfo { get; set; }
        public string stdout { get; set; }
        public string stderr { get; set; }
    }
}
