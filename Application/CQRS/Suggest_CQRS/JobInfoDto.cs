namespace Application.CQRS.Suggest_CQRS
{
    public class JobInfoDto
    {
        public string? Title { get; set; }
        public string? Company { get; set; }
        public string? Location { get; set; }
        public string? Link { get; set; }
        public string? CoverLetter { get; set; }
        public JobTypeDto? Type { get; set; }
    }
    public enum JobTypeDto
    {
        LinkedIn,
        Xing,
        Stepstone,
        Indeed
    }
}
