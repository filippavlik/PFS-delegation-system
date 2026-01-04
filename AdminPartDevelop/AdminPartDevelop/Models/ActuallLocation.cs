using AdminPartDevelop.Models;
using System;

namespace AdminPartDevelop.Models
{
    public partial class ActuallLocation
    {
        public int ActuallLocationId { get; set; }

        public int RefereeId { get; set; }

        public DateOnly DateFrom { get; set; }

        public TimeOnly TimeFrom { get; set; }

        public DateOnly DateTo { get; set; }

        public TimeOnly TimeTo { get; set; }

        public DateTime DatetimeAdded { get; set; }

        public string? Note { get; set; }

        public string? Address { get; set; }

        public float? Latitude { get; set; }

        public float? Longitude { get; set; }

        public virtual Referee Referee { get; set; } = null!;
    }
}
