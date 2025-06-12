using AdminPart.Models;

namespace AdminPart.Views.ViewModels
{
    public class TransportBetweenMatchesViewModel
    {
        public TransportBetweenMatchesViewModel(bool isManageable, string message, Transfer? preMatch, Transfer? postMatch)
        {
            IsManageable = isManageable;
            Message = message;
            PreMatch = preMatch;
            PostMatch = postMatch;
        }

        public bool IsManageable { get; set; }
        public string Message { get; set; }
        public Transfer? PreMatch { get; set; }
        public Transfer? PostMatch { get; set; }
    }

}
