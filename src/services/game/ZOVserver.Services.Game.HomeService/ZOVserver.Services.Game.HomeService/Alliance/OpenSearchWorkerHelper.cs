using Microsoft.Extensions.Caching.Memory;
using ZOVserver.Services.Shared.AllianceSearchService;

namespace ZOVserver.Services.Game.HomeService.Alliance;

public static class OpenSearchWorkerHelper
{
    public static AllianceOpenSearchWorker Worker { get; set; } = null!;
    public static IMemoryCache AlliancesCache { get; set; } = null!;
}