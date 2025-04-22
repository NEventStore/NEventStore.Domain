namespace NEventStore.Domain.Persistence
{
    public static class RepositoryExtensions
    {
        public static TAggregate GetById<TAggregate>(this IRepository repository, Guid id) where TAggregate : class, IAggregate
        {
            return repository.GetById<TAggregate>(Bucket.Default, id, int.MaxValue);
        }

        public static TAggregate GetById<TAggregate>(this IRepository repository, Guid id, int version) where TAggregate : class, IAggregate
        {
            return repository.GetById<TAggregate>(Bucket.Default, id, version);
        }

        public static TAggregate GetById<TAggregate>(this IRepository repository, string bucketId, Guid id) where TAggregate : class, IAggregate
        {
            return repository.GetById<TAggregate>(bucketId, id, int.MaxValue);
        }

        public static ICommit? Save(this IRepository repository, IAggregate aggregate, Guid commitId, Action<IDictionary<string, object>>? updateHeaders)
        {
            return repository.Save(Bucket.Default, aggregate, commitId, updateHeaders);
        }

        public static ICommit? Save(this IRepository repository, IAggregate aggregate, Guid commitId)
        {
            return repository.Save(aggregate, commitId, null);
        }

        public static ICommit? Save(this IRepository repository, string bucketId, IAggregate aggregate, Guid commitId)
        {
            return repository.Save(bucketId, aggregate, commitId, null);
        }

        public static Task<TAggregate> GetByIdAsync<TAggregate>(this IRepository repository, Guid id, CancellationToken cancellationToken = default) where TAggregate : class, IAggregate
        {
            return repository.GetByIdAsync<TAggregate>(Bucket.Default, id, int.MaxValue, cancellationToken);
        }

        public static Task<TAggregate> GetByIdAsync<TAggregate>(this IRepository repository, Guid id, int version, CancellationToken cancellationToken = default) where TAggregate : class, IAggregate
        {
            return repository.GetByIdAsync<TAggregate>(Bucket.Default, id, version, cancellationToken);
        }

        public static Task<TAggregate> GetByIdAsync<TAggregate>(this IRepository repository, string bucketId, Guid id, CancellationToken cancellationToken = default) where TAggregate : class, IAggregate
        {
            return repository.GetByIdAsync<TAggregate>(bucketId, id, int.MaxValue, cancellationToken);
        }

        public static Task<ICommit?> SaveAsync(this IRepository repository, IAggregate aggregate, Guid commitId, Action<IDictionary<string, object>>? updateHeaders, CancellationToken cancellationToken = default)
        {
            return repository.SaveAsync(Bucket.Default, aggregate, commitId, updateHeaders, cancellationToken);
        }

        public static Task<ICommit?> SaveAsync(this IRepository repository, IAggregate aggregate, Guid commitId, CancellationToken cancellationToken = default)
        {
            return repository.SaveAsync(aggregate, commitId, null, cancellationToken);
        }

        public static Task<ICommit?> SaveAsync(this IRepository repository, string bucketId, IAggregate aggregate, Guid commitId, CancellationToken cancellationToken = default)
        {
            return repository.SaveAsync(bucketId, aggregate, commitId, null, cancellationToken);
        }
    }
}