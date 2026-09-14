using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MixERP.Net.FrontEnd.Modules.BackOffice.Hubs;
using MixERP.Net.FrontEnd.Modules.Finance.Hubs;

namespace MixERP.Net.FrontEnd.Hubs
{
    public static class Startup
    {
        public static IEndpointRouteBuilder MapMixErpHubs(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapHub<BaseHub>("/hubs/base");
            endpoints.MapHub<UpdateHub>("/hubs/update");
            endpoints.MapHub<DbHub>("/hubs/database");
            endpoints.MapHub<DayOperationHub>("/hubs/day-operation");
            return endpoints;
        }
    }
}
