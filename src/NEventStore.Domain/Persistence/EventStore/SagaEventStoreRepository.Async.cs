using NEventStore.Persistence;

namespace NEventStore.Domain.Persistence.EventStore
{
    public partial class SagaEventStoreRepository
    {
        async Task<TSaga> ISagaRepository.GetByIdAsync<TSaga>(string bucketId, string sagaId, CancellationToken cancellationToken)
        {
            var stream = await OpenStreamAsync(bucketId, sagaId, cancellationToken).ConfigureAwait(false);
            return BuildSaga<TSaga>(sagaId, stream);
        }

        public async Task SaveAsync(string bucketId, ISaga saga, Guid commitId, Action<IDictionary<string, object>>? updateHeaders, CancellationToken cancellationToken)
        {
            if (saga == null)
            {
                throw new ArgumentNullException(nameof(saga), ExceptionMessages.NullArgument);
            }

            Dictionary<string, object> headers = PrepareHeaders(saga, updateHeaders);
            IEventStream stream = PrepareStream(bucketId, saga, headers);

            await PersistAsync(stream, commitId, cancellationToken).ConfigureAwait(false);

            saga.ClearUncommittedEvents();
            saga.ClearUndispatchedMessages();
        }

        private async Task<IEventStream> OpenStreamAsync(string bucketId, string sagaId, CancellationToken cancellationToken)
        {
            var sagaKey = bucketId + "+" + sagaId;
            if (_streams.TryGetValue(sagaKey, out IEventStream stream))
            {
                return stream;
            }

            try
            {
                stream = await _eventStore.OpenStreamAsync(bucketId, sagaId, 0, int.MaxValue, cancellationToken).ConfigureAwait(false);
            }
            catch (StreamNotFoundException)
            {
                stream = _eventStore.CreateStream(bucketId, sagaId);
            }

            return _streams[sagaKey] = stream;
        }

        private static async Task PersistAsync(IEventStream stream, Guid commitId, CancellationToken cancellationToken)
        {
            try
            {
                await stream.CommitChangesAsync(commitId, cancellationToken);
            }
            catch (DuplicateCommitException)
            {
                stream.ClearChanges();
            }
            catch (StorageException e)
            {
                throw new PersistenceException(e.Message, e);
            }
        }
    }
}
