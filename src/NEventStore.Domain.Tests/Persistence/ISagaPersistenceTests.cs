namespace NEventStore.Domain.Tests.Persistence.EventStore
{
    using System;
    using NEventStore.Domain.Persistence;
    using NEventStore.Domain.Persistence.EventStore;
    using NEventStore.Persistence.AcceptanceTests.BDD;
    using FluentAssertions;
#if MSTEST
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
#if NUNIT
    using NUnit.Framework;
#endif
#if XUNIT
    using Xunit;
    using Xunit.Should;
#endif

#if MSTEST
    [TestClass]
#endif
    public class using_a_sagaeventstorerepository : SpecificationBase
	{
		protected ISagaRepository _repository;

		protected IStoreEvents _storeEvents;

		protected override void Context()
		{
			this._storeEvents = Wireup.Init().UsingInMemoryPersistence().Build();
			this._repository = new SagaEventStoreRepository(this._storeEvents, new SagaFactory());
		}
	}

	public class when_a_saga_is_loaded : using_a_sagaeventstorerepository
	{
		private TestSaga _testSaga;

		private string _id;

		protected override void Context()
		{
			base.Context();
			_id = "something";
			_testSaga = new TestSaga(_id);
		}

		protected override void Because()
		{
			_repository.Save(_testSaga, Guid.NewGuid(), null);
		}

		[Fact]
		public void should_be_returned_when_loaded_by_id()
		{
			_repository.GetById<TestSaga>(_id).Id.Should().Be(_testSaga.Id);
		}
	}
}
