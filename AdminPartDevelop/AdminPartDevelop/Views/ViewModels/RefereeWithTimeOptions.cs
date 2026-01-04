using AdminPartDevelop.Models;

namespace AdminPartDevelop.Views.ViewModels
{
    public class RefereeWithTimeOptions
    {
        public class TimeRange
        {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public string RangeType { get; set; }
            public string? MatchId { get; set; }
            public int? ExcuseId { get; set; }
            public int? SlotId { get; set; }
            public int? TransferId { get; set; }
            public string? ActuallLocation { get; set; }


            public TimeRange(DateTime matchStart, DateTime matchEnd, string typeOfRange, string? matchId = null,    int? excuseId = null,    int? slotId = null, int? transferId = null, string? actuallLocation = null)
            {
                this.Start = matchStart;
                this.End = matchEnd;
                this.RangeType = typeOfRange;
                this.MatchId = matchId;
                this.ExcuseId = excuseId;
                this.SlotId = slotId;
                this.TransferId = transferId;
                this.ActuallLocation = actuallLocation;
            }            
        }

        public Referee Referee{ get; set; }
        public SortedSet<TimeRange> SortedRanges { get; set; }
        public bool isFreeSaturdayMorning { get; set; }
        public bool isFreeSaturdayAfternoon { get; set; }
        public bool isFreeSundayMorning { get; set; }
        public bool isFreeSundayAfternoon { get; set; }
        public bool hasSpecialNote { get; set; }

        public RefereeWithTimeOptions(Referee referee)
        {
            Referee = referee;
            SortedRanges = new SortedSet<TimeRange>(Comparer<TimeRange>.Create((x, y) => x.Start.CompareTo(y.Start)));
            isFreeSaturdayMorning = true;
            isFreeSaturdayAfternoon = true;
            isFreeSundayMorning = true;
            isFreeSundayAfternoon = true;
            hasSpecialNote = false;
        }
    }
}
