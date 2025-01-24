namespace NEventStore.Domain.Persistence
{
    public interface IConstructSagas
    {
        ISaga Build(Type type, string id);
    }
}