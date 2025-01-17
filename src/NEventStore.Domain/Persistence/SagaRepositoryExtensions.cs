namespace NEventStore.Domain.Persistence
{
    public static class SagaRepositoryExtensions
    {
        public static TSaga GetById<TSaga>(this ISagaRepository sagaRepository, string sagaId)
            where TSaga : class, ISaga
        {
            return sagaRepository.GetById<TSaga>(Bucket.Default, sagaId);
        }

        public static TSaga GetById<TSaga>(this ISagaRepository sagaRepository, Guid sagaId)
            where TSaga : class, ISaga
        {
            return sagaRepository.GetById<TSaga>(Bucket.Default, sagaId.ToString());
        }

        public static void Save(
            this ISagaRepository sagaRepository,
            ISaga saga,
            Guid commitId,
            Action<IDictionary<string, object>>? updateHeaders)
        {
            sagaRepository.Save(Bucket.Default, saga, commitId, updateHeaders);
        }

        public static Task<TSaga> GetByIdAsync<TSaga>(this ISagaRepository sagaRepository, string sagaId, CancellationToken cancellationToken = default)
            where TSaga : class, ISaga
        {
            return sagaRepository.GetByIdAsync<TSaga>(Bucket.Default, sagaId, cancellationToken);
        }

        public static Task<TSaga> GetByIdAsync<TSaga>(this ISagaRepository sagaRepository, Guid sagaId, CancellationToken cancellationToken = default)
            where TSaga : class, ISaga
        {
            return sagaRepository.GetByIdAsync<TSaga>(Bucket.Default, sagaId.ToString(), cancellationToken);
        }

        public static Task SaveAsync(
            this ISagaRepository sagaRepository,
            ISaga saga,
            Guid commitId,
            Action<IDictionary<string, object>>? updateHeaders,
            CancellationToken cancellationToken = default)
        {
            return sagaRepository.SaveAsync(Bucket.Default, saga, commitId, updateHeaders, cancellationToken);
        }
    }
}