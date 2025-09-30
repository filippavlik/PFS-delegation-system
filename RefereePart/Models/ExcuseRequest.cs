using System;
using System.Collections.Generic;

namespace RefereePart.Models;

public class ExcuseRequest
{
    public string UserId { get; set;}
    public List<ExcuseRequirements> Excuses { get; set; }
}
