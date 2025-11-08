using AdminPartDevelop.Models;

namespace AdminPartDevelop.Views.ViewModels
{
    public class SuggestedAddressViewModel
    {

        public SuggestedAddressViewModel(string? name, string? location, float lat, float lon)
        {
            Name = name;
            Location = location;
            Latitude = lat;
            Longtitude = lon;
        }

        public string Name { get; set; }
        public string Location { get; set; }
        public float Latitude { get; set; }
        public float Longtitude { get; set; }
    }
}
