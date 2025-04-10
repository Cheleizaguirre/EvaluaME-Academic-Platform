using Mudul.EntityModels;

namespace Mudul.ViewModels
{
    public class ExamQuestionsViewModel
    {
        public Exam Exam { get; set; }
        public IEnumerable<ExamQuestion> ExamQuestions { get; set; }
    }
}
