using AdminPartDevelop.Models;

namespace AdminPartDevelop.Views.ViewModels
{
    public class ExcuseRequest
    {
        public int RefereeId { get; set; }
        public List<ExcuseRequirements> Excuses { get; set; }
    }

}

