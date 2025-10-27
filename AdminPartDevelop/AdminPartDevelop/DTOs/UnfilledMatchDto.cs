using AdminPartDevelop.Models;

namespace AdminPartDevelop.DTOs
{
    public class UnfilledMatchDto
    {
       

        public UnfilledMatchDto(string numberMatch, string idHomeRaw, string nameHome, string idAwayRaw, string nameAway, DateTime dateOfGame, string gameField)
        {
            this.NumberMatch = numberMatch;
            this.IdHomeRaw = idHomeRaw;
            this.NameHome = nameHome;
            this.IdAwayRaw = idAwayRaw;
            this.NameAway = nameAway;
            this.DateOfGame = dateOfGame;
            this.GameField = gameField;
        }

        public string NumberMatch { get; set; }
        public string IdHomeRaw { get; set; }
        public string NameHome { get; set; }
        public string IdAwayRaw { get; set; }
        public string NameAway { get; set; }
        public DateTime DateOfGame { get; set; }
        public string GameField { get; set; }
    }
}
