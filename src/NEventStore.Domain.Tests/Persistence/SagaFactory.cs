using System.Reflection;
using NEventStore.Domain.Persistence;

namespace NEventStore.Domain.Tests.Persistence
{
    internal class SagaFactory : IConstructSagas
    {
        public ISaga Build(Type type, string id)
        {
            var constructor = type.GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(string) },
                null);

            if (constructor == null)
            {
                throw new NotSupportedException(string.Format("The type '{0}' does not have a constructor accepting a Guid.", type));
            }

            return (ISaga)constructor.Invoke(new object[] { id });
        }
    }
}