using Autofac;
using Autofac.Extensions.DependencyInjection;
using IProcessManager;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Oxygen.IocModule;
using Oxygen.Server.Kestrel.Implements;
using Saga;
using Saga.PubSub.Dapr;
using Saga.Store.Dapr;
using System;
using System.Text;

namespace GoodsService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateDefaultHost(args).Build().Run();
        }

        static IHostBuilder CreateDefaultHost(string[] args) => Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webhostbuilder =>
            {
                //ע���Ϊoxygen����ڵ�
                webhostbuilder.StartOxygenServer<OxygenStartup>((config) =>
                {
                    config.Port = 80;
                    config.PubSubCompentName = "pubsub";
                    config.StateStoreCompentName = "statestore";
                    config.TracingHeaders = "Authentication";
                    config.UseCors = true;
                });
                webhostbuilder.ConfigureServices(services =>
                {
                    services.AddScoped<IGoodsHandler, GoodsHandler>();
                    services.AddSaga(new SagaConfiguration("EshopSample", "GoodsService", "amqp://guest:123456@192.168.1.253:5672", "127.0.0.1:6379,prefix=test_", new CreateOrderTopicConfiguration()));
                    services.AddSagaStore();
                    services.AddControllers();
                }).
                Configure((ctx, app) =>
                {
                    if (ctx.HostingEnvironment.IsDevelopment())
                    {
                        app.UseDeveloperExceptionPage();
                    }
                    app.Map("/start", builder => builder.Run(async ctx =>
                    {
                        await ctx.RequestServices.GetService<ISagaManager>().StartOrNext(Topics.GoodsHandler.PreDeductInventory, new WordsDto() { Words = "������������;" });
                        await ctx.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("ok"));
                    }));
                    app.UseRouting();
                    app.UseAuthorization();
                    app.RegisterSagaHandler((x) => { Console.WriteLine($"����{x.SourceTopic}�����쳣,ԭʼ����json:{x.SourceDataJson},{(x.ErrorDataJson == null ? "" : $"�쳣�ص�json:{x.ErrorDataJson}��")}��Ҫ�˹�����"); });
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllers();
                    });
                });
            })
            .ConfigureContainer<ContainerBuilder>(builder =>
            {
                //ע��oxygen����
                builder.RegisterOxygenModule();
            })
            .ConfigureServices((context, services) =>
            {
                services.AddAutofac();
            })
            .UseServiceProviderFactory(new AutofacServiceProviderFactory());
    }
}
