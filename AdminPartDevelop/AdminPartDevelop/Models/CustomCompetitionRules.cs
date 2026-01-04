using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AdminPartDevelop.Models;

public partial class CustomCompetitionRules
{
    public int CustomCompetitionRuleId { get; set; }
    public int RefereeId { get; set; }
    public string CompetitionId { get; set; } = null!;

    public bool isAdded { get; set; }
    public virtual Competition Competition { get; set; }
}
