using AdminPartDevelop.Models;

namespace AdminPartDevelop.Views.ViewModels
{
    public class RefereeCardViewModel
    {
        public required List<Veto> Vetoes { get; set; }
        public required RefereeWithTimeOptions RefereeWTimeOptions { get; set; }
        public required List<(Competition,bool)> CustomCompetitionsRules { get; set; }
    }
}
