namespace NEventStore.Domain.Tests
{
    using NEventStore.Domain.Core;

    public class TestSaga : SagaBase<TestSagaMessage>
	{
		public TestSaga(string id)
		{
			Id = id;
		}
	}

	public abstract class TestSagaMessage { }
}