using System.Collections.Generic;
using System.Linq;

namespace NEventStore.Domain.Tests
{
    /// <summary>
	/// Extension methods especially useful for testing
	/// </summary>
	public static class TestExtensions
    {
        public static IEnumerable<TEvent> RaisedEvents<TEvent>(this IAggregate aggregate)
            where TEvent : IDomainEvent
        {
            return aggregate.GetUncommittedEvents().OfType<TEvent>();
        }

        public static TEvent LastRaisedEvent<TEvent>(this IAggregate aggregate)
            where TEvent : IDomainEvent
        {
            return aggregate.GetUncommittedEvents().OfType<TEvent>().LastOrDefault();
        }

        public static bool HasRaisedEvent<TEvent>(this IAggregate aggregate)
            where TEvent : IDomainEvent
        {
            return aggregate.GetUncommittedEvents().OfType<TEvent>().Any();
        }

        public static IEnumerable<IDomainEvent> GetEventStream(this IStoreEvents eventStore, string streamId)
        {
            var stream = eventStore.OpenStream(streamId);
            return stream.CommittedEvents.Select(evt => (IDomainEvent)evt.Body);
        }

        public static IEnumerable<IDomainEvent> GetEventStream(this IStoreEvents eventStore, string bucketId, string streamId)
        {
            var stream = eventStore.OpenStream(bucketId, streamId, int.MinValue, int.MaxValue);
            return stream.CommittedEvents.Select(evt => (IDomainEvent)evt.Body);
        }

        /// <summary>
        /// recupera l'ultimo evento del tipo indicato
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="eventStream"></param>
        /// <returns></returns>
		public static TEvent LastOfType<TEvent>(this IEnumerable<IDomainEvent> eventStream)
            where TEvent : IDomainEvent
        {
            return eventStream.OfType<TEvent>().LastOrDefault();
        }

        /// <summary>
        /// verifica se un evento del tipo indicato è stato lanciato
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="eventStream"></param>
        /// <returns></returns>
		public static bool HasAnyOfType<TEvent>(this IEnumerable<IDomainEvent> eventStream)
            where TEvent : IDomainEvent
        {
            return eventStream.OfType<TEvent>().Any();
        }
    }
}
