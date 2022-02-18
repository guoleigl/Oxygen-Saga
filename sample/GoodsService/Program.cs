using Autofac;
using Autofac.Extensions.DependencyInjection;
using GoodsService;
using IProcessManager;
using Oxygen.IocModule;
using Oxygen.Server.Kestrel.Implements;
using Saga;
using Saga.PubSub.Dapr;
using Saga.Store.Dapr;
using System.Text;

var builder = OxygenApplication.CreateBuilder(config =>
{
    config.Port = 80;
    config.PubSubCompentName = "pubsub";
    config.StateStoreCompentName = "statestore";
    config.TracingHeaders = "Authentication";
    config.UseCors = true;
});
OxygenStartup.ConfigureServices(builder.Services);
builder.Services.AddSaga(new SagaConfiguration("EshopSample", "GoodsService", "amqp://guest:123456@192.168.1.253:5672", "127.0.0.1:6379,prefix=test_", new CreateOrderTopicConfiguration()));
builder.Services.AddSagaStore();
builder.Host.ConfigureContainer<ContainerBuilder>(builder =>
{
    //ע��oxygen����
    builder.RegisterOxygenModule();
    builder.RegisterType<GoodsHandler>().As<IGoodsHandler>().InstancePerLifetimeScope();
});
builder.Services.AddAutofac();
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
var app = builder.Build();
app.MapGet("/start",async ctx =>
{
    await ctx.RequestServices.GetService<ISagaManager>().StartOrNext(Topics.GoodsHandler.PreDeductInventory, new WordsDto() { Words = "������������;" });
    await ctx.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("ok"));
});
OxygenStartup.Configure(app, app.Services);
app.RegisterSagaHandler(async (x) => {
    Console.WriteLine($"����{x.SourceTopic}�����쳣,ԭʼ����json:{x.SourceDataJson},{(x.ErrorDataJson == null ? "" : $"�쳣�ص�json:{x.ErrorDataJson}��")}��Ҫ�˹�����");
    await Task.CompletedTask;
});
await app.RunAsync();