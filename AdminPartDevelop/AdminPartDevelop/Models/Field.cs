using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AdminPartDevelop.Models;

public partial class Field
{
    public Field(string fieldName)
    {
        FieldName = fieldName;
    }
    public Field(string fieldName,string fieldAddress,float latitude,float longtitude)
    {
        FieldName = fieldName;
        FieldAddress = fieldAddress;
        Longitude = longtitude;
        Latitude = latitude;
    }

    public int FieldId { get; set; }

    public string FieldName { get; set; } = null!;

    public string? FieldAddress { get; set; }

    public float Latitude { get; set; }

    public float Longitude { get; set; }

    [JsonIgnore]
    public virtual ICollection<Match> Matches { get; set; } = new List<Match>();
}
