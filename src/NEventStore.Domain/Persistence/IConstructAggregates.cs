namespace NEventStore.Domain.Persistence
{
    using System;

    public interface IConstructAggregates
	{
		IAggregate Build(Type type, Guid id, IMemento snapshot);
	}
}