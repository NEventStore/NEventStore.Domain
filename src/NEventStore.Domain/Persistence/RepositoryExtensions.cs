namespace NEventStore.Domain.Persistence
{
    public static class RepositoryExtensions
    {
        static readonly Action<IDictionary<string, object>> DoNotUpdateHeaders = _ => { };

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

        public static void Save(this IRepository repository, IAggregate aggregate, Guid commitId, Action<IDictionary<string, object>> updateHeaders)
        {
            repository.Save(Bucket.Default, aggregate, commitId, updateHeaders);
        }

        public static void Save(this IRepository repository, IAggregate aggregate, Guid commitId)
        {
            repository.Save(aggregate, commitId, DoNotUpdateHeaders);
        }

        public static void Save(this IRepository repository, string bucketId, IAggregate aggregate, Guid commitId)
        {
            repository.Save(bucketId, aggregate, commitId, DoNotUpdateHeaders);
        }
    }
}