using AdminPartDevelop.Common;
using AdminPartDevelop.DTOs;
using AdminPartDevelop.Views.ViewModels;
using Aspose.Cells;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AdminPartDevelop.Services.FileParsers
{
    public class ExportData : IExcelExporter
    {
        private readonly ILogger<ExportData> _logger;

        public ExportData(ILogger<ExportData> logger)
        {
            _logger = logger;
        }

        public ServiceResult<byte[]> GenerateMatchExcel(List<MatchViewModel> matches, Dictionary<int, Tuple<string, string>> infoReferees)
        {
            try
            {
                var headers = new[]
                {
                "Číslo zápasu", "Domací", "Hosté", "Datum zápasu", "HR",
                "AR1", "AR2", "4R", "DS", "DT",
                "VAR","AVAR", "Hřiště", "competition", "already_played", "locked", "last_changed_by", "last_changed"
            };

                var workbook = new Workbook();
                var sheet = workbook.Worksheets[0];
                var cells = sheet.Cells;

                for (int i = 0; i < headers.Length; i++)
                    cells[0, i].PutValue(headers[i]);

                // Write rows
                for (int i = 0; i < matches.Count; i++)
                {
                    var m = matches[i];
                    var refereeId = m.Match.RefereeId;
                    var ar1Id = m.Match.Ar1Id;
                    var ar2Id = m.Match.Ar2Id ;
                    Tuple<string,string> ? tupleReferee = refereeId.HasValue ? infoReferees[refereeId.Value] : null;
                    Tuple<string, string> ? tupleAr1 = ar1Id.HasValue ? infoReferees[ar1Id.Value] : null;
                    Tuple<string, string> ? tupleAr2 = ar2Id.HasValue ? infoReferees[ar2Id.Value] : null;

                    var row = new object[]

                    {
                    m.Match.MatchId,
                    m.Match.HomeTeamId+" - "+m.HomeTeamName,
                    m.Match.AwayTeamId+" - "+m.AwayTeamName,
                    m.Match.MatchDate.ToString("dd.MM.yyyy") + " " + m.Match.MatchTime.ToString("HH:mm"),
                    tupleReferee != null
                    ? tupleReferee.Item1 + (string.IsNullOrEmpty(tupleReferee.Item2) ? " ()" : $" ({tupleReferee.Item2})")
                    : "",

                tupleAr1 != null
                    ? tupleAr1.Item1 + (string.IsNullOrEmpty(tupleAr1.Item2) ? " ()" : $" ({tupleAr1.Item2})")
                    : "",

                tupleAr2 != null
                    ? tupleAr2.Item1 + (string.IsNullOrEmpty(tupleAr2.Item2) ? " ()" : $" ({tupleAr2.Item2})")
                    : "",
                    "",
                    "",
                    "",
                    "",
                    "",
                    m.FieldName,
                    m.CompetitionName,
                    m.Match.AlreadyPlayed,
                    m.Match.Locked,
                    m.Match.LastChangedBy,
                    m.Match.LastChanged.ToString("dd.MM.yyyy HH:mm")
                    };

                    for (int j = 0; j < row.Length; j++)
                        cells[i + 1, j].PutValue(row[j]);
                }

                using var stream = new MemoryStream();
                workbook.Save(stream, SaveFormat.Xlsx);
                return ServiceResult<byte[]>.Success(stream.ToArray());

            }catch(Exception ex)
            {
                _logger.LogError(ex, "[GenerateMatchExcel] Error creating file with matches");
                return ServiceResult<byte[]>.Failure("Nepodařilo se vytvořit soubor");
            }
        }
    }
}

