using System;
using System.Threading.Tasks;

namespace Shared.Infrastructure.MessageBrokers
{
    public interface IMessageBroker
    {
        void Publish<T>(T message, string exchange, string routingKey) where T : class;
        
        void Subscribe<TEvent>(
            string exchangeName,
            string queueName, 
            string routingKey,
            Func<TEvent, Task> handler) where TEvent : class;
    }
}