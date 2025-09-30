using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RefereePart.Models
{
	public partial class VehicleSlotRequirements
	{
    		public DateOnly DateFrom { get; set; }

    		public TimeOnly TimeFrom { get; set; }

    		public DateOnly DateTo { get; set; }

    		public TimeOnly TimeTo { get; set; }

    		public bool? HasCarInTheSlot { get; set; }
}}
