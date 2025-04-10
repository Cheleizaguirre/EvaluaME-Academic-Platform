namespace Mudul.Models
{
    public class ExamAttemptViewModel
    {
        public Mudul.EntityModels.Exam Exam { get; set; } = null!;
        public List<Mudul.EntityModels.ExamQuestion> ExamQuestions { get; set; } = new List<Mudul.EntityModels.ExamQuestion>();
    }
}
