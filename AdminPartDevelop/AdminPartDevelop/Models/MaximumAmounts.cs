using AdminPartDevelop.Models;
using System;

namespace AdminPartDevelop.Models;

public partial class MaximumAmount
{
    public int MaximumAmountId { get; set; }

    public int RefereeId { get; set; }

    public DateOnly DateFrom { get; set; }

    public TimeOnly TimeFrom { get; set; }

    public DateOnly DateTo { get; set; }

    public TimeOnly TimeTo { get; set; }

    public DateTime DatetimeAdded { get; set; }

    public string? Note { get; set; }

    public int MaximumValue { get; set; }   // ⬅ INTEGER

    public virtual Referee Referee { get; set; } = null!;
}