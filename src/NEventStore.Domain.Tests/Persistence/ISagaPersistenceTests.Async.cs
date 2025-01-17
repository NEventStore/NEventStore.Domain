using NEventStore.Domain.Persistence;
using NEventStore.Domain.Persistence.EventStore;
using NEventStore.Persistence.AcceptanceTests.BDD;
using FluentAssertions;
#if MSTEST
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
#if NUNIT
#endif
#if XUNIT
using Xunit;
using Xunit.Should;
#endif

namespace NEventStore.Domain.Tests.Persistence.EventStore.Async
{
#if MSTEST
    [TestClass]
#endif
    public class using_a_sagaeventstorerepository : SpecificationBase
    {
        protected ISagaRepository? _repository;

        protected IStoreEvents? _storeEvents;

        protected override void Context()
        {
            _storeEvents = Wireup.Init().UsingInMemoryPersistence().Build();
            _repository = new SagaEventStoreRepository(_storeEvents, new SagaFactory());
        }
    }

    public class when_a_saga_is_loaded : using_a_sagaeventstorerepository
    {
        private TestSaga? _testSaga;

        private string _id = "something";

        protected override void Context()
        {
            base.Context();
            _testSaga = new TestSaga(_id);
        }

        protected override Task BecauseAsync()
        {
            return _repository!.SaveAsync(_testSaga!, Guid.NewGuid(), null);
        }

        [Fact]
        public async Task should_be_returned_when_loaded_by_id()
        {
            (await _repository!.GetByIdAsync<TestSaga>(_id).ConfigureAwait(false)).Id.Should().Be(_testSaga!.Id);
        }
    }
}
