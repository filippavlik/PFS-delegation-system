using AdminPartDevelop.Models;

namespace AdminPartDevelop.Views.ViewModels
{
        public partial class ExcuseRequirements
        {

            public DateOnly DateFrom { get; set; }

            public TimeOnly TimeFrom { get; set; }

            public DateOnly DateTo { get; set; }

            public TimeOnly TimeTo { get; set; }


            public string? Note { get; set; }

            public string? Reason { get; set; }
        }

}

