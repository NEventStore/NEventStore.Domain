namespace NEventStore.Domain.Persistence
{
    public interface ISagaRepository
    {
        TSaga GetById<TSaga>(string bucketId, string sagaId) where TSaga : class, ISaga;

        Task<TSaga> GetByIdAsync<TSaga>(string bucketId, string sagaId, CancellationToken cancellationToken = default) where TSaga : class, ISaga;

        void Save(string bucketId, ISaga saga, Guid commitId, Action<IDictionary<string, object>>? updateHeaders);

        Task SaveAsync(string bucketId, ISaga saga, Guid commitId, Action<IDictionary<string, object>>? updateHeaders, CancellationToken cancellationToken = default);
    }
}