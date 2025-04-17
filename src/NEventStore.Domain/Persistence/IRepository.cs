namespace NEventStore.Domain.Persistence
{
    public interface IRepository : IDisposable
    {
        TAggregate GetById<TAggregate>(string bucketId, Guid id, int version) where TAggregate : class, IAggregate;

        Task<TAggregate> GetByIdAsync<TAggregate>(string bucketId, Guid id, int version, CancellationToken cancellationToken = default) where TAggregate : class, IAggregate;

        ICommit? Save(string bucketId, IAggregate aggregate, Guid commitId, Action<IDictionary<string, object>>? updateHeaders);

        Task<ICommit?> SaveAsync(string bucketId, IAggregate aggregate, Guid commitId, Action<IDictionary<string, object>>? updateHeaders, CancellationToken cancellationToken = default);
    }
}