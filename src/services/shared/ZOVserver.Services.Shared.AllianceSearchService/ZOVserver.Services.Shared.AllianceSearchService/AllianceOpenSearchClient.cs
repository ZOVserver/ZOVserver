using OpenSearch.Client;
using OpenSearch.Net;
using ZOVserver.Shared.Contracts.Models;

namespace ZOVserver.Services.Shared.AllianceSearchService;

public static class AllianceOpenSearchClient
{
    private static OpenSearchClient InitOpenSearchClient(PoolSettings poolSettings, ConnSettings connSettings)
    {
        if (poolSettings.Nodes == null || poolSettings.Nodes.Length == 0)
            throw new ArgumentException("At least one node is required", nameof(poolSettings.Nodes));

        var pool = poolSettings.PoolType switch
        {
            ConnectionPoolType.Static => new StaticConnectionPool(poolSettings.Nodes),
            ConnectionPoolType.Sticky => new StickyConnectionPool(poolSettings.Nodes),
            ConnectionPoolType.Sniffing => new SniffingConnectionPool(poolSettings.Nodes),
            _ => throw new ArgumentOutOfRangeException(nameof(poolSettings.PoolType), poolSettings.PoolType, null)
        };

        var settings = new ConnectionSettings(pool);

        settings.PingTimeout(connSettings.PingTimeout);
        settings.RequestTimeout(connSettings.RequestTimeout);
        settings.MaximumRetries(connSettings.MaxRetries);
        settings.MaxRetryTimeout(connSettings.RetryTimeout);

        settings.DefaultIndex(connSettings.Index);

        if (connSettings.UsePrettyJson)
            settings.PrettyJson();

        if (connSettings.EnableDebugMode)
            settings.EnableDebugMode();

        if (connSettings.EnableHttpCompression)
            settings.EnableHttpCompression();

        if (connSettings.EnableHttpPipelining)
            settings.EnableHttpPipelining();

        if (connSettings is { BasicAuthentication: not null, ApiKeyAuthentication: not null })
            throw new InvalidOperationException("Use either BasicAuthentication OR ApiKeyAuthentication, not both");

        if (connSettings.BasicAuthentication != null)
            settings.BasicAuthentication(
                connSettings.BasicAuthentication.Value.Item1,
                connSettings.BasicAuthentication.Value.Item2);

        if (connSettings.ApiKeyAuthentication != null)
            settings.ApiKeyAuthentication(
                connSettings.ApiKeyAuthentication.Value.Item1,
                connSettings.ApiKeyAuthentication.Value.Item2);

        return new OpenSearchClient(settings);
    }

    public static async Task<AllianceOpenSearchWorker?> InitAllianceOpenSearchWorkerAsync(PoolSettings poolSettings,
        ConnSettings connSettings,
        int shards = 3, int replicas = 1, int maxAllianceMembersCount = 100)
    {
        var client = InitOpenSearchClient(poolSettings, connSettings);

        if ((await client.Indices.ExistsAsync(connSettings.Index)).Exists)
            return new AllianceOpenSearchWorker(client, maxAllianceMembersCount);

        var res = await client.Indices.CreateAsync(connSettings.Index, c => c
            .Settings(s => s
                .NumberOfShards(shards)
                .NumberOfReplicas(replicas)
                .Analysis(a => a
                    .Tokenizers(t => t
                        .EdgeNGram("edge_tokenizer", e => e
                            .MinGram(2)
                            .MaxGram(10)
                            .TokenChars(TokenChar.Letter, TokenChar.Digit, TokenChar.Symbol)
                        )
                    )
                    .Analyzers(an => an
                        .Custom("edge_analyzer", ca => ca
                            .Tokenizer("edge_tokenizer")
                            .Filters("lowercase")
                        )
                        .Custom("simple_lower", ca => ca
                            .Tokenizer("whitespace")
                            .Filters("lowercase")
                        )
                    )
                )
            )
            .Map<OpenSearchAlliance>(m => m
                .Properties(ps => ps
                    .Text(t => t
                        .Name(n => n.Name)
                        .Analyzer("simple_lower")
                        .Fields(f => f
                            .Keyword(k => k.Name("raw"))
                            .Text(tt => tt.Name("prefix").Analyzer("edge_analyzer"))
                        )
                    )
                )
            )
        );

        return !res.IsValid ? null : new AllianceOpenSearchWorker(client, maxAllianceMembersCount);
    }
}