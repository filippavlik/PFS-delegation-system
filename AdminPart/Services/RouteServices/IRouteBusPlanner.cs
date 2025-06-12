using AdminPart.Common;
using System;

namespace AdminPart.Services.RouteServices
{
    public interface IRouteBusPlanner 
    {
        Task<ServiceResult<Tuple<int, int>>> CalculateRoute(float startLatitude,float startLongtitude,float endLatitude,float endLongtitude, DateTime? departureTime = null);
    }
}
