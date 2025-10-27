using AdminPartDevelop.Common;
using AdminPartDevelop.DTOs;
using AdminPartDevelop.Models;
using AdminPartDevelop.Views.ViewModels;
using Aspose.Cells;
using Microsoft.Maui.Storage;

namespace AdminPartDevelop.Services.FileParsers
{
    public interface IExcelExporter
    {
        
            /// <summary>
            /// Generates an Excel file containing match information and referee assignments.
            /// </summary>
            /// <param name="matches">A list of match view models containing details such as teams, date, time and other metadata.</param>
            /// <param name="infoReferees">A dictionary mapping referee IDs to tuples containing name and additional information.</param>
            /// <returns>
            /// A ServiceResult containing the Excel file as a byte array if successful, 
            /// or an error message if the operation fails.
            /// </returns>
            ServiceResult<byte[]> GenerateMatchExcel(List<MatchViewModel> matches,Dictionary<int,Tuple<string,string>> infoReferees);


    }
}
