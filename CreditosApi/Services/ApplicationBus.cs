using CreditosApi.Interfaces;
using System.Reflection;

namespace CreditosApi.Services;

public class ApplicationBus : IApplicationBus
{
    private readonly IServiceProvider _serviceProvider;

    public ApplicationBus(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> SendCommand<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResponse));
        var handler = _serviceProvider.GetService(handlerType);

        if (handler == null)
            throw new InvalidOperationException($"CommandHandler não encontrado para: {command.GetType().Name}");

        try
        {
            var method = handlerType.GetMethod("Handle");
            var result = method?.Invoke(handler, new object[] { command, cancellationToken });
            return await (Task<TResponse>)result;
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException ?? ex;
        }
    }

    public async Task SendCommand(ICommand command, CancellationToken cancellationToken = default)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        var handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());
        var handler = _serviceProvider.GetService(handlerType);

        if (handler == null)
            throw new InvalidOperationException($"CommandHandler não encontrado para: {command.GetType().Name}");

        try
        {
            var method = handlerType.GetMethod("Handle");
            var result = method.Invoke(handler, new object[] { command, cancellationToken });
            await (Task)result;
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException ?? ex;
        }
    }

    public async Task<TResponse> SendQuery<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResponse));
        var handler = _serviceProvider.GetService(handlerType);

        if (handler == null)
            throw new InvalidOperationException($"QueryHandler não encontrado para: {query.GetType().Name}");

        try
        {
            var method = handlerType.GetMethod("Handle");
            var result = method.Invoke(handler, new object[] { query, cancellationToken });
            return await (Task<TResponse>)result;
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException ?? ex;
        }
    }

    public async Task PublishEvent(IEvent @event, CancellationToken cancellationToken = default)
    {
        if (@event == null)
            throw new ArgumentNullException(nameof(@event));

        var eventType = @event.GetType();
        var handlerType = typeof(IEventHandler<>).MakeGenericType(eventType);

        var handlers = _serviceProvider.GetServices(handlerType);

        if (!handlers.Any())
            return;

        var tasks = new List<Task>();

        foreach (var handler in handlers)
        {
            try
            {
                var method = handlerType.GetMethod("Handle");
                var result = method.Invoke(handler, new object[] { @event, cancellationToken });

                if (result is Task task)
                    tasks.Add(task);
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException ?? ex;
            }
        }

        if (tasks.Count > 0)
            await Task.WhenAll(tasks);
    }

    public Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        if (notification is IEvent @event)
            return PublishEvent(@event, cancellationToken);

        throw new InvalidOperationException($"Tipo de notification não suportado: {notification.GetType().Name}");
    }
}