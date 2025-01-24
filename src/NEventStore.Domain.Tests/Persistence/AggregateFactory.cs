using System.Reflection;
using NEventStore.Domain.Persistence;

namespace NEventStore.Domain.Tests.Persistence
{
    internal class AggregateFactory : IConstructAggregates
    {
        public IAggregate Build(Type type, Guid id, IMemento? snapshot)
        {
            var constructor = type.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance, null, [typeof(Guid)], null);

            if (constructor == null)
            {
                throw new NotSupportedException(string.Format("The type '{0}' does not have a constructor accepting a Guid.", type));
            }

            return (IAggregate)constructor!.Invoke([id]);
        }
    }
}