using Autofac;
using Autofac.Extensions.DependencyInjection;
using IProcessManager;
using OrderService;
using Oxygen.IocModule;
using Oxygen.Server.Kestrel.Implements;
using Saga;
using Saga.PubSub.Dapr;
using Saga.Store.Dapr;

var builder = OxygenApplication.CreateBuilder(config =>
{
    config.Port = 80;
    config.PubSubCompentName = "pubsub";
    config.StateStoreCompentName = "statestore";
    config.TracingHeaders = "Authentication";
    config.UseCors = true;
});
OxygenStartup.ConfigureServices(builder.Services);
builder.Services.AddSaga(new SagaConfiguration("EshopSample", "OrderService", "amqp://guest:123456@192.168.1.253:5672", "127.0.0.1:6379,prefix=test_", new CreateOrderTopicConfiguration()));
builder.Services.AddSagaStore();
builder.Host.ConfigureContainer<ContainerBuilder>(builder =>
{
    //ע��oxygen����
    builder.RegisterOxygenModule();
    builder.RegisterType<OrderHandler>().As<IOrderHandler>().InstancePerLifetimeScope();
});
builder.Services.AddAutofac();
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
var app = builder.Build();
OxygenStartup.Configure(app, app.Services);
app.RegisterSagaHandler(async (ctx, err) => {
    Console.WriteLine($"����{err.SourceTopic}�����쳣,ԭʼ����json:{err.SourceDataJson},{(err.ErrorDataJson == null ? "" : $"�쳣�ص�json:{err.ErrorDataJson}��")}��Ҫ�˹�����");
    await Task.CompletedTask;
});
await app.RunAsync();