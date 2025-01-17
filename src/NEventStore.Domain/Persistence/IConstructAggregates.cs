namespace NEventStore.Domain.Persistence
{
    public interface IConstructAggregates
    {
        IAggregate Build(Type type, Guid id, IMemento? snapshot);
    }
}