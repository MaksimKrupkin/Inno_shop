using MassTransit;
using MassTransit.RabbitMqTransport;
using RabbitMQ.Client;
using System;
using System.Threading.Tasks;

namespace Shared.Infrastructure.MessageBrokers
{
    public class RabbitMqBroker : IMessageBroker
    {
        private readonly IBusControl _busControl;

        public RabbitMqBroker(IBusControl busControl)
        {
            _busControl = busControl;
        }

        // Реализация метода Publish
        public void Publish<T>(T message, string exchange, string routingKey) where T : class
        {
            var endpoint = _busControl.GetSendEndpoint(new Uri($"exchange:{exchange}")).Result;
            endpoint.Send(message, context => context.SetRoutingKey(routingKey)).Wait();
        }

        public void Subscribe<TEvent>(
    string exchangeName,
    string queueName,
    string routingKey,
    Func<TEvent, Task> handler) where TEvent : class
{
    _busControl.ConnectReceiveEndpoint(queueName, cfg =>
    {
        cfg.ConfigureConsumeTopology = false;
        
        if (cfg is IRabbitMqReceiveEndpointConfigurator rmq)
        {
            rmq.Bind(exchangeName, b =>
            {
                b.RoutingKey = routingKey;
                b.ExchangeType = ExchangeType.Direct;
            });
        }

        cfg.Handler<TEvent>(async context => 
        {
            await handler(context.Message);
        });
    });
}
    }
}