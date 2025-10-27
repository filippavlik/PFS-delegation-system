using AdminPartDevelop.Common;
using System;

namespace AdminPartDevelop.Services.RouteServices
{
    public interface IRouteCarPlanner 
    {
        Task<ServiceResult<Tuple<int, int>>> CalculateRoute(float startLatitude,float startLongtitude,float endLatitude,float endLongtitude, DateTime? departureTime = null);
    }
}
