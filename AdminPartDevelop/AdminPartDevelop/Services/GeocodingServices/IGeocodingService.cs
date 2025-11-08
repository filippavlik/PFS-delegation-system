using AdminPartDevelop.Common;
using AdminPartDevelop.Views.ViewModels;
using System;

namespace AdminPartDevelop.Services.GeocodingServices
{
    public interface IGeocodingService 
    {
         Task<ServiceResult<List<SuggestedAddressViewModel>>> SuggestAddress(string input);
    }
}
