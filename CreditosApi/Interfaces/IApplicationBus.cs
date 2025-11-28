namespace CreditosApi.Interfaces;

using System.Threading;
using System.Threading.Tasks;

public interface ICommand<TResponse> { }

public interface ICommand { }

public interface IQuery<TResponse> { }

public interface IEvent { }

public interface ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<TResponse> Handle(TCommand command, CancellationToken cancellationToken);
}

public interface ICommandHandler<TCommand>
    where TCommand : ICommand
{
    Task Handle(TCommand command, CancellationToken cancellationToken);
}

public interface IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<TResponse> Handle(TQuery query, CancellationToken cancellationToken);
}

public interface IEventHandler<TEvent>
    where TEvent : IEvent
{
    Task Handle(TEvent @event, CancellationToken cancellationToken);
}

public interface IApplicationBus
{
    // Commands COM retorno
    Task<TResponse> SendCommand<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);

    // Commands SEM retorno (void)
    Task SendCommand(ICommand command, CancellationToken cancellationToken = default);

    // Queries
    Task<TResponse> SendQuery<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);

    // Events
    Task PublishEvent(IEvent @event, CancellationToken cancellationToken = default);

    // Método de compatibilidade para notifications genéricas
    Task Publish(object notification, CancellationToken cancellationToken = default);
}