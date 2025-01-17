namespace NEventStore.Domain.Persistence
{
    public interface IRepository : IDisposable
    {
        TAggregate GetById<TAggregate>(string bucketId, Guid id, int version) where TAggregate : class, IAggregate;

        void Save(string bucketId, IAggregate aggregate, Guid commitId, Action<IDictionary<string, object>> updateHeaders);
    }
}