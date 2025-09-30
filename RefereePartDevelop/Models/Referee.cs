using System;
using System.Collections.Generic;

namespace RefereePart.Models;

public partial class Referee
{
    public int RefereeId { get; set; }

    public string? UserId { get; set; }

    public string? FacrId { get; set; }

    public string Name { get; set; } = null!;

    public string Surname { get; set; } = null!;

    public string Email { get; set; } = null!;

    public int League { get; set; }

    public int Age { get; set; }

    public bool Ofs { get; set; }

    public string? Note { get; set; }

    public string PragueZone { get; set; } = null!;

    public string? ActuallPragueZone { get; set; }

    public bool CarAvailability { get; set; }

    public DateTime TimestampChange { get; set; }

    public virtual ICollection<Excuse> Excuses { get; set; } = new List<Excuse>();

    public virtual ICollection<VehicleSlot> VehicleSlots { get; set; } = new List<VehicleSlot>();
}
