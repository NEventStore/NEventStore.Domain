namespace NEventStore.Domain.Tests.Persistence
{
    using System;
    using System.Reflection;
    using NEventStore.Domain.Persistence;

    internal class SagaFactory : IConstructSagas
	{
		public ISaga Build(Type type, string id)
		{
			ConstructorInfo constructor = type.GetConstructor(
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
				null,
				new[] { typeof(string) },
				null);

			return constructor.Invoke(new object[] { id }) as ISaga;
		}
	}
}