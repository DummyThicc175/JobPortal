using System.ComponentModel.DataAnnotations;

namespace JobPortal.Models // <--- Quan trọng: Namespace phải khớp hoàn toàn
{
    public class SavedJob
    {
        public int Id { get; set; }

    
        public string CandidateId { get; set; }

       
        public int JobPostId { get; set; }
        public virtual JobPost? JobPost { get; set; }

        public DateTime SavedDate { get; set; } = DateTime.Now;
    }
}