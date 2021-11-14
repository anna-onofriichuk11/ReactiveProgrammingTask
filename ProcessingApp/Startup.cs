using System;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using ProcessingApp.Crypto_Service_Idl.Src.Service;
using ProcessingApp.Crypto_Service.Src.Service.External;
using ProcessingApp.Crypto_Service.Src.Service.External.Utils;
using ProcessingApp.Price_Service_Idl.Src.Service;
using ProcessingApp.Price_Service.Src.Service.Impl;
using ProcessingApp.Sockets;
using ProcessingApp.Trade_Service_Idl.Src.Service;
using ProcessingApp.Trade_Service.Src.Repository;
using ProcessingApp.Trade_Service.Src.Repository.impl;
using ProcessingApp.Trade_Service.Src.Service.impl;

namespace ProcessingApp
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<CryptoCompareClient>();
            services.AddTransient<ICryptoService, CryptoCompareService>();
            services.AddTransient<IPriceService, DefaultPriceService>();
            services.AddTransient<IMessageUnpacker, PriceMessageUnpacker>();
            services.AddTransient<IMessageUnpacker, TradeMessageUnpacker>();
            services.AddTransient<ITradeRepository>(sp =>
                new H2TradeRepository(sp.GetService<ILogger<H2TradeRepository>>(),
                    Configuration.GetConnectionString("TradesContext")));
            services.AddTransient<ITradeRepository>(sp =>
                new MongoTradeRepository(sp.GetService<ILogger<MongoTradeRepository>>(),
                    new MongoClient("mongodb://localhost:27017")));
            services.AddTransient<ITradeService, DefaultTradeService>();
            services.AddTransient<WsHandler>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            var serviceScopeFactory = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
            var serviceProvider = serviceScopeFactory.CreateScope().ServiceProvider;
            var serializerSettings = new JsonSerializerSettings();
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

            WsHandler wsHandler = serviceProvider.GetService<WsHandler>();
            app
                .UseWebSockets()
                .Use(async (context, next) =>
                {
                    if (context.Request.Path == "/stream")
                    {
                        if (context.WebSockets.IsWebSocketRequest)
                        {
                            WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();

                            await wsHandler.Handle(Observable.Create<string>(async (observer, ct) =>
                                {
                                    var buffer = new byte[1024 * 4];
                                    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                                    while(!result.EndOfMessage)
                                    {
                                        buffer = new byte[1024 * 4];
                                        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                                        observer.OnNext(Encoding.UTF8.GetString(buffer, 0, result.Count));
                                    }
                                }))
                                .Select(m => JsonConvert.SerializeObject(m, serializerSettings))
                                .Do(async message =>
                                {
                                    byte[] output = Encoding.UTF8.GetBytes(message);
                                    await webSocket.SendAsync(new ArraySegment<byte>(output, 0, output.Length),
                                        WebSocketMessageType.Text, true, CancellationToken.None);
                                })
                                .LastAsync();
                        }
                        else
                        {
                            context.Response.StatusCode = 400;
                        }
                    }
                    else
                    {
                        await next();
                    }
                });

            app.UseDefaultFiles();
            app.UseStaticFiles();
        }
    }
}