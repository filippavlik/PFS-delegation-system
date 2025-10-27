namespace AdminPartDevelop.DTOs
{
    public class CompetitionToUpdateDto
    {
        public string CompetitionId { get; set; }
        public string CompetitionName { get; set; }
        public int CompetitionLength { get; set; }
        public int CompetitionAmountOfReferees { get; set; }
	    public int CompetitionLeague { get; set; }
    }
}
