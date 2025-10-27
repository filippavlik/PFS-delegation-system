namespace AdminPartDevelop.DTOs
{
    public class MatchDateInfo
    {
        public DateOnly? Date { get; set; }
        public bool IsHome { get; set; }
    }
    public class RefereesTeamsMatchesResponseDto
    {
        public int RefereeId { get; set; }
        public int HomeCount { get; set; }
        public int AwayCount { get; set; }
    	public List<MatchDateInfo> MatchesDates {get;set;} = new List<MatchDateInfo>();
    }
}
