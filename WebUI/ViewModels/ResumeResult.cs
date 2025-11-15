using Domain.Entities;

namespace WebAPI.ViewModels
{
    public class ResumeResult
    {
        public List<Skill> Skills { get; set; } = new List<Skill>();

        public List<Experience> Experiences { get; set; } = new List<Experience>();

        public List<Education> Educations { get; set; } = new List<Education>();

        public List<Certificate> Certificates { get; set; } = new List<Certificate>();

        public string JobTitle { get; set; } = string.Empty;
    }
}
