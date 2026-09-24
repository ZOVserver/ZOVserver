using OpenSearch.Client;
using OpenSearch.Net;
using ZOVserver.Shared.Contracts.Models;

namespace ZOVserver.Services.Shared.AllianceSearchService;

public class AllianceOpenSearchWorker(OpenSearchClient client, int maxMembersCount)
{
    public int MaxMembersInAlliance => maxMembersCount;

    public async Task CreateAllianceAsync(OpenSearchAlliance alliance)
    {
        var response = await client.IndexAsync(alliance, i => i
            .OpType(OpType.Create)
            .Refresh(Refresh.False)
        );

        if (!response.IsValid)
        {
            if (response.ServerError.Status == 409)
                throw new InvalidOperationException($"Alliance {alliance.Id} already exists");

            throw new Exception($"Failed to create alliance: {response.DebugInformation}");
        }
    }

    public async Task UpdateAllianceAsync(OpenSearchAlliance alliance)
    {
        var response = await client.UpdateAsync<OpenSearchAlliance>(alliance.Id, u => u
            .Doc(alliance)
            .Refresh(Refresh.False)
        );

        if (!response.IsValid)
        {
            if (response.ServerError.Status == 404)
                throw new InvalidOperationException($"Alliance {alliance.Id} not found");

            throw new Exception($"Failed to update alliance: {response.DebugInformation}");
        }
    }

    public async Task UpsertAllianceAsync(OpenSearchAlliance alliance)
    {
        var response = await client.UpdateAsync<OpenSearchAlliance>(alliance.Id, u => u
            .Doc(alliance)
            .DocAsUpsert()
            .Refresh(Refresh.False)
        );

        if (!response.IsValid)
            throw new Exception($"Failed to upsert alliance: {response.DebugInformation}");
    }

    public async Task<OpenSearchAlliance[]> SearchAllianceByNameAsync(string name, int count = 50)
    {
        var response = await client.SearchAsync<OpenSearchAlliance>(s => s
            .Size(count)
            .Query(q => q
                .Bool(b => b
                    .Should(
                        sh => sh.Match(m => m
                            .Field(f => f.Name.Suffix("raw"))
                            .Query(name)
                            .Boost(20)
                        ),
                        sh2 => sh2.MatchPhrasePrefix(mpp => mpp
                            .Field(f => f.Name.Suffix("prefix"))
                            .Query(name)
                            .Boost(10)
                        ),
                        sh3 => sh3.Match(m => m
                            .Field(f => f.Name)
                            .Query(name)
                            .Fuzziness(Fuzziness.EditDistance(2))
                            .Boost(5)
                        )
                    )
                    .MinimumShouldMatch(1)
                )
            )
        );

        return response.Documents.ToArray();
    }

    public async Task<OpenSearchAlliance[]> GetRandomAlliancesAsync(int count, int playerTrophies,
        int playerRegionGlobalId = 0)
    {
        var response = await client.SearchAsync<OpenSearchAlliance>(s => s
            .Size(count * 3)
            .Query(q => q
                .Bool(b => b
                    .Must(
                        m => m.Range(r => r
                            .Field(f => f.MembersCount)
                            .LessThan(maxMembersCount)
                        ),
                        m => m.Terms(t => t
                            .Field(f => f.AllianceType)
                            .Terms(0, 1)
                        ),
                        m => m.Range(r => r
                            .Field(f => f.RequiredTrophies)
                            .LessThanOrEquals(playerTrophies)
                        )
                    )
                    .Should(sh => sh.Term(t => t
                            .Field(f => f.RegionGlobalId)
                            .Value(playerRegionGlobalId)
                        )
                    )
                    .MinimumShouldMatch(0)
                )
            )
        );

        var alliances = response.Documents
            .OrderBy(_ => Random.Shared.Next())
            .Take(count)
            .ToArray();

        return alliances;
    }

    public async Task<OpenSearchAlliance[]> GetTopAlliancesAsync(int count, int? regionGlobalId = null)
    {
        var search = new SearchDescriptor<OpenSearchAlliance>()
            .Size(count)
            .Sort(s => s.Descending(f => f.NowTrophies));

        if (regionGlobalId.HasValue)
            search = search.Query(q => q
                .Term(t => t
                    .Field(f => f.RegionGlobalId)
                    .Value(regionGlobalId.Value)
                )
            );

        var response = await client.SearchAsync<OpenSearchAlliance>(search);

        return response.Documents.ToArray();
    }
}