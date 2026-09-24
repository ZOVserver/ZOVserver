using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;
using NATS.Client.Serializers.Json;
using ZOVserver.Services.Global.GameGlobalEventsService.Grpc;
using ZOVserver.Services.Global.GameGlobalEventsService.Services;
using ZOVserver.Services.Global.GameGlobalEventsService.Settings;
using ZOVserver.Shared.Contracts.Proto;
using ZOVserver.Shared.TitanRemnants.Helper;
using ZOVserver.Shared.TitanRemnants.PAssets.Tables;

namespace ZOVserver.Services.Global.GameGlobalEventsService;

public static class Program
{
    public static void Main(string[] args)
    {
        LogoWriter.ShowLogo();

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddGrpc();

        builder.Host.ConfigureServices((ctx, services) =>
        {
            var cfg = ctx.Configuration;

            var natsSection = cfg.GetSection("GlobalEventsNats");

            if (!natsSection.Exists())
                throw new InvalidOperationException("Section 'GlobalEventsNats' not found");

            var host = natsSection["Host"] ?? throw new InvalidOperationException("NATS Host not found");
            var port = natsSection["Port"] ?? "4222";
            var user = natsSection["Username"];
            var pass = natsSection["Password"];

            services.AddSingleton<INatsConnection>(_ =>
            {
                var auth = !string.IsNullOrEmpty(user)
                    ? new NatsAuthOpts { Username = user, Password = pass }
                    : NatsAuthOpts.Default;

                var opts = NatsOpts.Default with
                {
                    Url = $"nats://{host}:{port}",
                    AuthOpts = auth,
                    SerializerRegistry = new NatsJsonSerializerRegistry()
                };

                return new NatsConnection(opts);
            });

            var fsu = cfg.GetConnectionString("FileServerUrl") ??
                      throw new InvalidOperationException("ConnectionStrings::FileServerUrl not found");

            var fsuChannel = GrpcChannel.ForAddress(fsu, new GrpcChannelOptions
            {
                MaxReceiveMessageSize = 256 * 1024 * 1024,
                MaxSendMessageSize = 256 * 1024 * 1024
            });

            var fsuClient = new FileServerService.FileServerServiceClient(fsuChannel);
            LogicDataTables.LoadFrom(fsuClient);

            var hs = fsuClient.GetFile(new FileRequest { Path = "Settings/home_settings.yml" }).Data.ToStringUtf8();

            HomeSettings.Load(hs);

            services.AddHostedService<GameEventSlotGlobalEventPublisherService>();
            services.AddHostedService<TrophySeasonDataGlobalEventPublisherService>();
        });


        var app = builder.Build();

        app.MapGrpcService<GameGlobalEventsGrpcService>();

        app.Run();
    }
}