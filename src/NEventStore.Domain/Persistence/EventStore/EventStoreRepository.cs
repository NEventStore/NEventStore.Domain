using NEventStore.Persistence;

namespace NEventStore.Domain.Persistence.EventStore
{
    public partial class EventStoreRepository : IRepository
    {
        private const string AggregateTypeHeader = "AggregateType";

        private readonly IDetectConflicts _conflictDetector;

        private readonly IStoreEvents _eventStore;

        private readonly IConstructAggregates _factory;

        private readonly Dictionary<string, ISnapshot?> _snapshots = [];

        private readonly Dictionary<string, IEventStream> _streams = [];

        public EventStoreRepository(IStoreEvents eventStore, IConstructAggregates factory, IDetectConflicts conflictDetector)
        {
            _eventStore = eventStore;
            _factory = factory;
            _conflictDetector = conflictDetector;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

       public TAggregate GetById<TAggregate>(string bucketId, Guid id, int version) where TAggregate : class, IAggregate
        {
            var snapshot = GetSnapshot(bucketId, id, version);
            IEventStream stream = OpenStream(bucketId, id, version, snapshot);
            var aggregate = GetAggregate<TAggregate>(snapshot, stream);

            ApplyEventsToAggregate(version, stream, aggregate);

            return aggregate;
        }

        public void Save(string bucketId, IAggregate aggregate, Guid commitId, Action<IDictionary<string, object>>? updateHeaders)
        {
            Dictionary<string, object> headers = PrepareHeaders(aggregate, updateHeaders);
            while (true)
            {
                IEventStream stream = PrepareStream(bucketId, aggregate, headers);
                int commitEventCount = stream.CommittedEvents.Count;

                try
                {
                    stream.CommitChanges(commitId);
                    aggregate.ClearUncommittedEvents();
                    return;
                }
                catch (DuplicateCommitException)
                {
                    stream.ClearChanges();
                    // Issue: #4 and test: when_an_aggregate_is_persisted_using_the_same_commitId_twice
                    // should we rethrow the exception here? or provide a feedback whether the save was successful ?
                    return;
                }
                catch (ConcurrencyException e)
                {
                    var conflict = ThrowOnConflict(stream, commitEventCount);
                    stream.ClearChanges();

                    if (conflict)
                    {
                        throw new ConflictingCommandException(e.Message, e);
                    }
                }
                catch (StorageException e)
                {
                    throw new PersistenceException(e.Message, e);
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            lock (_streams)
            {
                foreach (var stream in _streams)
                {
                    stream.Value.Dispose();
                }

                _snapshots.Clear();
                _streams.Clear();
            }
        }

        private static void ApplyEventsToAggregate(int versionToLoad, IEventStream stream, IAggregate aggregate)
        {
            if (versionToLoad == 0 || aggregate.Version < versionToLoad)
            {
                foreach (var @event in stream.CommittedEvents.Select(x => x.Body))
                {
                    aggregate.ApplyEvent(@event);
                }
            }
        }

        private TAggregate GetAggregate<TAggregate>(ISnapshot? snapshot, IEventStream stream) where TAggregate : class, IAggregate
        {
            var memento = snapshot == null ? null : snapshot.Payload as IMemento;
            return (TAggregate)_factory.Build(typeof(TAggregate), stream.StreamId.ToGuid(), memento);
        }

        private ISnapshot? GetSnapshot(string bucketId, Guid id, int version)
        {
            var snapshotId = bucketId + id;
            if (!_snapshots.TryGetValue(snapshotId, out ISnapshot? snapshot))
            {
                _snapshots[snapshotId] = snapshot = _eventStore.Advanced.GetSnapshot(bucketId, id, version);
            }

            return snapshot;
        }

        private IEventStream OpenStream(string bucketId, Guid id, int version, ISnapshot? snapshot)
        {
            IEventStream stream;
            var streamId = bucketId + "+" + id;
            if (_streams.TryGetValue(streamId, out stream))
            {
                return stream;
            }

            stream = snapshot == null
                ? _eventStore.OpenStream(bucketId, id, 0, version)
                : _eventStore.OpenStream(snapshot, version);

            return _streams[streamId] = stream;
        }

        private IEventStream PrepareStream(string bucketId, IAggregate aggregate, Dictionary<string, object> headers)
        {
            var streamId = bucketId + "+" + aggregate.Id;
            if (!_streams.TryGetValue(streamId, out IEventStream stream))
            {
                _streams[streamId] = stream = _eventStore.CreateStream(bucketId, aggregate.Id);
            }

            foreach (var item in headers)
            {
                stream.UncommittedHeaders[item.Key] = item.Value;
            }

            aggregate.GetUncommittedEvents()
                     .Cast<object>()
                     .Select(x => new EventMessage { Body = x })
                     .ToList()
                     .ForEach(stream.Add);

            return stream;
        }

        private static Dictionary<string, object> PrepareHeaders(
            IAggregate aggregate, Action<IDictionary<string, object>>? updateHeaders)
        {
            var headers = new Dictionary<string, object>();

            headers[AggregateTypeHeader] = aggregate.GetType().FullName;
            updateHeaders?.Invoke(headers);

            return headers;
        }

        private bool ThrowOnConflict(IEventStream stream, int skip)
        {
            IEnumerable<object> committed = stream.CommittedEvents.Skip(skip).Select(x => x.Body);
            IEnumerable<object> uncommitted = stream.UncommittedEvents.Select(x => x.Body);
            return _conflictDetector.ConflictsWith(uncommitted, committed);
        }
    }
}