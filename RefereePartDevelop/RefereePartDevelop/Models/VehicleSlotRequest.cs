using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RefereePart.Models;
public class VehicleSlotRequest
{
    public string UserId { get; set; }
    
    [JsonPropertyName("vehicleSlots")]
    public List<VehicleSlotRequirements> VehicleSlots { get; set; }
}
