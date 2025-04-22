using NEventStore.Domain.Core;
using NEventStore.Domain.Persistence;
using NEventStore.Domain.Persistence.EventStore;
using NEventStore.Persistence.AcceptanceTests;
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

namespace NEventStore.Domain.Tests.Persistence.Async
{
#if MSTEST
    [TestClass]
#endif
    public abstract class using_a_configured_repository : SpecificationBase
    {
        protected IRepository? _repository;

        protected IStoreEvents? _storeEvents;

        protected override void Context()
        {
            _storeEvents = Wireup.Init().UsingInMemoryPersistence().Build();
            _repository = CreateRepository();
        }

        protected EventStoreRepository CreateRepository()
        {
            return new EventStoreRepository(_storeEvents!, new AggregateFactory(), new ConflictDetector());
        }
    }

    public class when_an_aggregate_is_persisted : using_a_configured_repository
    {
        private TestAggregate? _testAggregate;
        private ICommit? _commit;

        private Guid _id;

        protected override void Context()
        {
            base.Context();
            _id = Guid.NewGuid();
            _testAggregate = new TestAggregate(_id, "Test");
        }

        protected override async Task BecauseAsync()
        {
            _commit = await _repository!.SaveAsync(_testAggregate!, Guid.NewGuid(), null).ConfigureAwait(false);
        }

        [Fact]
        public void should_return_the_persisted_commit()
        {
            _commit.Should().NotBeNull();
        }

        [Fact]
        public async Task should_be_returned_when_loaded_by_id()
        {
            (await _repository!.GetByIdAsync<TestAggregate>(_id).ConfigureAwait(false))
                .Should().NotBeNull();
        }

        [Fact]
        public async Task version_should_be_one()
        {
            (await _repository!.GetByIdAsync<TestAggregate>(_id).ConfigureAwait(false))
                .Version.Should().Be(1);
        }

        [Fact]
        public async Task id_should_be_set()
        {
            (await _repository!.GetByIdAsync<TestAggregate>(_id).ConfigureAwait(false))
                .Id.Should().Be(_id);
        }

        [Fact]
        public async Task should_have_name_set()
        {
            (await _repository!.GetByIdAsync<TestAggregate>(_id).ConfigureAwait(false))
                .Name.Should().Be(_testAggregate!.Name);
        }
    }

    public class when_a_persisted_aggregate_is_updated : using_a_configured_repository
    {
        private Guid _id;

        private const string NewName = "UpdatedName";
        private ICommit? _commit;

        protected override Task ContextAsync()
        {
            base.Context();
            _id = Guid.NewGuid();
            return _repository!.SaveAsync(new TestAggregate(_id, "Test"), Guid.NewGuid(), null);
        }

        protected override async Task BecauseAsync()
        {
            var aggregate = await _repository!.GetByIdAsync<TestAggregate>(_id).ConfigureAwait(false);
            aggregate.ChangeName(NewName);
            _commit = await _repository!.SaveAsync(aggregate, Guid.NewGuid(), null).ConfigureAwait(false);
        }

        [Fact]
        public void should_return_the_persisted_commit()
        {
            _commit.Should().NotBeNull();
            _commit!.Events.Count.Should().Be(1);
            _commit.Events.First().Body.Should().BeOfType<NameChangedEvent>();
        }

        [Fact]
        public async Task should_have_updated_name()
        {
            (await _repository!.GetByIdAsync<TestAggregate>(_id))
                .Name.Should().Be(NewName);
        }

        [Fact]
        public async Task should_have_updated_version()
        {
            (await _repository!.GetByIdAsync<TestAggregate>(_id))
                .Version.Should().Be(2);
        }
    }

    public class when_a_loading_a_specific_aggregate_version : using_a_configured_repository
    {
        private Guid _id;

        private const string VersionOneName = "Test";
        private const string NewName = "UpdatedName";

        protected override Task ContextAsync()
        {
            base.Context();
            _id = Guid.NewGuid();
            return _repository!.SaveAsync(new TestAggregate(_id, VersionOneName), Guid.NewGuid(), null);
        }

        protected override async Task BecauseAsync()
        {
            var aggregate = _repository!.GetById<TestAggregate>(_id);
            aggregate.ChangeName(NewName);
            await _repository!.SaveAsync(aggregate, Guid.NewGuid(), null).ConfigureAwait(false);
            _repository!.Dispose();
        }

        [Fact]
        public async Task should_be_able_to_load_initial_version()
        {
            (await _repository!.GetByIdAsync<TestAggregate>(_id, 1))
                .Name.Should().Be(VersionOneName);
        }
    }

    public class when_an_aggregate_is_persisted_to_specific_bucket : using_a_configured_repository
    {
        private TestAggregate? _testAggregate;

        private Guid _id;

        private readonly string _bucket = "TenantB";
        private ICommit? _commit;

        protected override void Context()
        {
            base.Context();
            _id = Guid.NewGuid();
            _testAggregate = new TestAggregate(_id, "Test");
        }

        protected override async Task BecauseAsync()
        {
            _commit = await _repository!.SaveAsync(_bucket, _testAggregate!, Guid.NewGuid(), null).ConfigureAwait(false);
        }

        [Fact]
        public void should_return_the_persisted_commit()
        {
            _commit.Should().NotBeNull();
            _commit!.BucketId.Should().Be(_bucket);
        }

        [Fact]
        public async Task should_be_returned_when_loaded_by_id()
        {
            (await _repository!.GetByIdAsync<TestAggregate>(_bucket, _id).ConfigureAwait(false))
                .Name.Should().Be(_testAggregate!.Name);
        }
    }

    /// <summary>
    /// <para>
    /// Idempotency Check:
    /// Internally a DuplicateCommitException will be raised and catch by the repository,
    /// the whole commit will be discarded, we assume the it's the same commit issued twice.
    /// </para>
    /// <para>Issue: #4</para>
    /// </summary>
    public class when_an_aggregate_is_persisted_using_the_same_commitId_twice : using_a_configured_repository
    {
        private TestAggregate? _testAggregate;
        private ICommit? _commit;

        private Guid _id;

        protected override void Context()
        {
            base.Context();
            _id = Guid.NewGuid();
            _testAggregate = new TestAggregate(_id, "Test");
        }

        protected override async Task BecauseAsync()
        {
            var commitId = Guid.NewGuid();
            await _repository!.SaveAsync(_testAggregate!, commitId, null).ConfigureAwait(false);

            _testAggregate!.ChangeName("one");

            _commit = await _repository!.SaveAsync(_testAggregate, commitId).ConfigureAwait(false);
        }

        [Fact]
        public void persisted_commit_should_be_null()
        {
            _commit.Should().BeNull();
        }

        [Fact]
        public async Task the_second_commit_was_silently_discarded_and_not_written_to_database()
        {
            var aggregate = (await _repository!.GetByIdAsync<TestAggregate>(_id).ConfigureAwait(false));
            aggregate.Name.Should().Be("Test");
            aggregate.Version.Should().Be(1);
        }

        [Fact]
        public void the_aggregate_still_has_pending_changes()
        {
            var uncommittedEvents = ((IAggregate)_testAggregate!).GetUncommittedEvents();
            uncommittedEvents.Count.Should().BeGreaterThan(0);
            var enumerator = uncommittedEvents.GetEnumerator();
            enumerator.MoveNext();
            enumerator.Current.Should().BeOfType<NameChangedEvent>();
        }
    }

    public class when_an_aggregate_is_persisted_by_two_repositories : SpecificationBase
    {
        protected IRepository? _repository1;
        protected IRepository? _repository2;

        protected IStoreEvents? _storeEvents;
        private Guid _aggregateId;
        private TestAggregate? aggregate;
        private Exception? _thrown;

        protected override void Context()
        {
            base.Context();

            _storeEvents = Wireup.Init().UsingInMemoryPersistence().Build();
            _repository1 = new EventStoreRepository(_storeEvents, new AggregateFactory(), new ConflictDetector());
            _repository2 = new EventStoreRepository(_storeEvents, new AggregateFactory(), new ConflictDetector());

            _aggregateId = Guid.NewGuid();
            aggregate = new TestAggregate(_aggregateId, "my name is..");
        }

        protected override async Task BecauseAsync()
        {
            await _repository1!.SaveAsync(aggregate!, Guid.NewGuid()).ConfigureAwait(false);
            aggregate!.ChangeName("one");

            _thrown = await Catch.ExceptionAsync(() => _repository2!.SaveAsync(aggregate, Guid.NewGuid()));
        }

        [Fact]
        public void should_not_throw_a_ConflictingCommandException()
        {
            _thrown.Should().BeNull();
        }

        [Fact]
        public async Task should_have_updated_name_if_loaded_by_repository_that_saved_it_last()
        {
            (await _repository2!.GetByIdAsync<TestAggregate>(_aggregateId).ConfigureAwait(false))
                .Name.Should().Be("one");
        }

        /// <summary>
        /// current repository implementation act as a session cache!
        /// </summary>
        [Fact]
        public async Task should_have_original_name_if_loaded_by_repository_that_saved_it_first()
        {
            (await _repository1!.GetByIdAsync<TestAggregate>(_aggregateId).ConfigureAwait(false))
                .Name.Should().Be("my name is..");
        }
    }

    public class when_an_aggregate_is_persisted_concurrently_by_two_clients : SpecificationBase
    {
        protected IRepository? _repository1;
        protected IRepository? _repository2;

        protected IStoreEvents? _storeEvents;
        private Guid _aggregateId;
        private Exception? _thrown;

        protected override Task ContextAsync()
        {
            _storeEvents = Wireup.Init().UsingInMemoryPersistence().Build();
            _repository1 = new EventStoreRepository(_storeEvents, new AggregateFactory(), new ConflictDetector());
            _repository2 = new EventStoreRepository(_storeEvents, new AggregateFactory(), new ConflictDetector());

            _aggregateId = Guid.NewGuid();
            var aggregate = new TestAggregate(_aggregateId, "my name is..");
            return _repository1.SaveAsync(aggregate, Guid.NewGuid());
        }

        protected override async Task BecauseAsync()
        {
            var agg1 = await _repository1!.GetByIdAsync<TestAggregate>(_aggregateId).ConfigureAwait(false);
            var agg2 = await _repository2!.GetByIdAsync<TestAggregate>(_aggregateId).ConfigureAwait(false);
            agg1.ChangeName("one");
            agg2.ChangeName("two");

            await _repository1!.SaveAsync(agg1, Guid.NewGuid()).ConfigureAwait(false);

            _thrown = await Catch.ExceptionAsync(() => _repository2!.SaveAsync(agg2, Guid.NewGuid())).ConfigureAwait(false);
        }

        [Fact]
        public void should_throw_a_ConflictingCommandException()
        {
            _thrown.Should().BeOfType<ConflictingCommandException>();
        }
    }

    public class when_an_aggregate_is_persisted_concurrently_by_two_clients_using_new_operator : SpecificationBase
    {
        protected IRepository? _repository1;
        protected IRepository? _repository2;

        protected IStoreEvents? _storeEvents;
        private Guid _aggregateId;
        private Exception? _thrown;

        protected override Task ContextAsync()
        {
            _storeEvents = Wireup.Init().UsingInMemoryPersistence().Build();
            _repository1 = new EventStoreRepository(_storeEvents, new AggregateFactory(), new ConflictDetector());
            _repository2 = new EventStoreRepository(_storeEvents, new AggregateFactory(), new ConflictDetector());

            _aggregateId = Guid.NewGuid();
            var aggregate = new TestAggregate(_aggregateId, "my name is..");
            return _repository1.SaveAsync(aggregate, Guid.NewGuid());
        }

        protected override async Task BecauseAsync()
        {
            var agg1 = await _repository1!.GetByIdAsync<TestAggregate>(_aggregateId).ConfigureAwait(false);
            var agg2 = new TestAggregate(_aggregateId, "two");
            agg1.ChangeName("one");

            await _repository1!.SaveAsync(agg1, Guid.NewGuid()).ConfigureAwait(false);

            _thrown = await Catch.ExceptionAsync(() => _repository2!.SaveAsync(agg2, Guid.NewGuid())).ConfigureAwait(false);
        }

        [Fact]
        public void should_throw_a_ConflictingCommandException()
        {
            _thrown.Should().BeOfType<ConflictingCommandException>();
        }
    }

    public class when_the_same_aggregate_is_created_and_persisted_concurrently : SpecificationBase
    {
        protected IRepository? _repository1;
        protected IRepository? _repository2;

        protected IStoreEvents? _storeEvents;
        private Guid _aggregateId;
        private Exception? _thrown;

        protected override void Context()
        {
            _storeEvents = Wireup.Init().UsingInMemoryPersistence().Build();
            _repository1 = new EventStoreRepository(_storeEvents, new AggregateFactory(), new ConflictDetector());
            _repository2 = new EventStoreRepository(_storeEvents, new AggregateFactory(), new ConflictDetector());

            _aggregateId = Guid.NewGuid();
        }

        protected override async Task BecauseAsync()
        {
            var agg1 = new TestAggregate(_aggregateId, "one");
            var agg2 = new TestAggregate(_aggregateId, "two");

            await _repository1!.SaveAsync(agg1, Guid.NewGuid()).ConfigureAwait(false);

            _thrown = await Catch.ExceptionAsync(() => _repository2!.SaveAsync(agg2, Guid.NewGuid())).ConfigureAwait(false);
        }

        [Fact]
        public void should_throw_a_ConflictingCommandException()
        {
            _thrown.Should().BeOfType<ConflictingCommandException>();
        }
    }

    public class when_aggregate_is_reloaded_with_snapshot : using_a_configured_repository
    {
        private TestAggregate? _testAggregate;
        private TestAggregate? _reloadedAggregate;
        private Guid _id;

        protected override Task ContextAsync()
        {
            base.Context();
            _id = Guid.NewGuid();
            _testAggregate = new TestAggregate(_id, "Test");
            return _repository!.SaveAsync(_testAggregate, Guid.NewGuid()); //save at version 1.
        }

        protected override async Task BecauseAsync()
        {
            var otherRepository = CreateRepository();
            var aggregate = await otherRepository.GetByIdAsync<TestAggregate>(_id).ConfigureAwait(false); //load at version 1
            aggregate.ChangeName("Name changed");
            await otherRepository.SaveAsync(Bucket.Default, aggregate, Guid.NewGuid()).ConfigureAwait(false); //save in version 2
            //Now save the snapshot.

            var memento = ((IAggregate)aggregate).GetSnapshot();
            var snapshot = new Snapshot(Bucket.Default, aggregate.Id.ToString(), aggregate.Version, memento!);

            await _storeEvents!.Advanced.AddSnapshotAsync(snapshot, CancellationToken.None).ConfigureAwait(false);

            //now reload, 
            _reloadedAggregate = await otherRepository.GetByIdAsync<TestAggregate>(_id).ConfigureAwait(false);
        }

        [Fact]
        public void should_have_correct_version()
        {
            _reloadedAggregate!.Version.Should().Be(2);
        }
    }
}