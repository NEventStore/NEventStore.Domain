using NEventStore.Persistence;

namespace NEventStore.Domain.Persistence.EventStore
{
    public partial class EventStoreRepository
    {
        async Task<TAggregate> IRepository.GetByIdAsync<TAggregate>(string bucketId, Guid id, int version, CancellationToken cancellationToken)
        {
            var snapshot = await GetSnapshotAsync(bucketId, id, version, cancellationToken).ConfigureAwait(false);
            IEventStream stream = await OpenStreamAsync(bucketId, id, version, snapshot, cancellationToken: cancellationToken).ConfigureAwait(false);
            var aggregate = GetAggregate<TAggregate>(snapshot, stream);

            ApplyEventsToAggregate(version, stream, aggregate);

            return aggregate;
        }

        public async Task<ICommit?> SaveAsync(string bucketId, IAggregate aggregate, Guid commitId, Action<IDictionary<string, object>>? updateHeaders, CancellationToken cancellationToken)
        {
            Dictionary<string, object> headers = PrepareHeaders(aggregate, updateHeaders);
            while (true)
            {
                IEventStream stream = PrepareStream(bucketId, aggregate, headers);
                int commitEventCount = stream.CommittedEvents.Count;

                try
                {
                    var commit = await stream.CommitChangesAsync(commitId, cancellationToken).ConfigureAwait(false);
                    aggregate.ClearUncommittedEvents();
                    return commit;
                }
                catch (DuplicateCommitException)
                {
                    stream.ClearChanges();
                    // Issue: #4 and test: when_an_aggregate_is_persisted_using_the_same_commitId_twice
                    // should we rethrow the exception here? or provide a feedback whether the save was successful ?
                    return null;
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

        private async Task<ISnapshot?> GetSnapshotAsync(string bucketId, Guid id, int version, CancellationToken cancellationToken)
        {
            var snapshotId = bucketId + id;
            if (!_snapshots.TryGetValue(snapshotId, out ISnapshot? snapshot))
            {
                snapshot = await _eventStore.Advanced.GetSnapshotAsync(bucketId, id, version, cancellationToken).ConfigureAwait(false);
                _snapshots[snapshotId] = snapshot;
            }

            return snapshot;
        }

        private async Task<IEventStream> OpenStreamAsync(string bucketId, Guid id, int version, ISnapshot? snapshot, CancellationToken cancellationToken)
        {
            var streamId = bucketId + "+" + id;
            if (_streams.TryGetValue(streamId, out IEventStream stream))
            {
                return stream;
            }

            stream = snapshot == null
                ? await _eventStore.OpenStreamAsync(bucketId, id, 0, version, cancellationToken: cancellationToken).ConfigureAwait(false)
                : await _eventStore.OpenStreamAsync(snapshot, version, cancellationToken).ConfigureAwait(false);

            return _streams[streamId] = stream;
        }
    }
}
