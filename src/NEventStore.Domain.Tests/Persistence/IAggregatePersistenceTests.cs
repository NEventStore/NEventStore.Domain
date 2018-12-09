namespace NEventStore.Domain.Tests.Persistence
{
    using System;
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
    using NUnit.Framework;
#endif
#if XUNIT
    using Xunit;
    using Xunit.Should;
#endif

#if MSTEST
    [TestClass]
#endif
    public abstract class using_a_configured_repository : SpecificationBase
    {
        protected IRepository _repository;

        protected IStoreEvents _storeEvents;

        protected override void Context()
        {
            this._storeEvents = Wireup.Init().UsingInMemoryPersistence().Build();
            this._repository = CreateRepository();
        }

        protected EventStoreRepository CreateRepository()
        {
            return new EventStoreRepository(this._storeEvents, new AggregateFactory(), new ConflictDetector());
        }
    }

    public class when_an_aggregate_is_persisted : using_a_configured_repository
    {
        private TestAggregate _testAggregate;

        private Guid _id;

        protected override void Context()
        {
            base.Context();
            _id = Guid.NewGuid();
            _testAggregate = new TestAggregate(_id, "Test");
        }

        protected override void Because()
        {
            _repository.Save(_testAggregate, Guid.NewGuid(), null);
        }

        [Fact]
        public void should_be_returned_when_loaded_by_id()
        {
            _repository.GetById<TestAggregate>(_id).Should().NotBeNull();
        }

        [Fact]
        public void version_should_be_one()
        {
            _repository.GetById<TestAggregate>(_id).Version.Should().Be(1);
        }

        [Fact]
        public void id_should_be_set()
        {
            _repository.GetById<TestAggregate>(_id).Id.Should().Be(_id);
        }

        [Fact]
        public void should_have_name_set()
        {
            _repository.GetById<TestAggregate>(_id).Name.Should().Be(_testAggregate.Name);
        }
    }

    public class when_a_persisted_aggregate_is_updated : using_a_configured_repository
    {
        private Guid _id;

        private const string NewName = "UpdatedName";

        protected override void Context()
        {
            base.Context();
            _id = Guid.NewGuid();
            _repository.Save(new TestAggregate(_id, "Test"), Guid.NewGuid(), null);
        }

        protected override void Because()
        {
            var aggregate = _repository.GetById<TestAggregate>(_id);
            aggregate.ChangeName(NewName);
            _repository.Save(aggregate, Guid.NewGuid(), null);
        }

        [Fact]
        public void should_have_updated_name()
        {
            _repository.GetById<TestAggregate>(_id).Name.Should().Be(NewName);
        }

        [Fact]
        public void should_have_updated_version()
        {
            _repository.GetById<TestAggregate>(_id).Version.Should().Be(2);
        }
    }

    public class when_a_loading_a_specific_aggregate_version : using_a_configured_repository
    {
        private Guid _id;

        private const string VersionOneName = "Test";
        private const string NewName = "UpdatedName";

        protected override void Context()
        {
            base.Context();
            _id = Guid.NewGuid();
            _repository.Save(new TestAggregate(_id, VersionOneName), Guid.NewGuid(), null);
        }

        protected override void Because()
        {
            var aggregate = _repository.GetById<TestAggregate>(_id);
            aggregate.ChangeName(NewName);
            _repository.Save(aggregate, Guid.NewGuid(), null);
            _repository.Dispose();
        }

        [Fact]
        public void should_be_able_to_load_initial_version()
        {
            _repository.GetById<TestAggregate>(_id, 1).Name.Should().Be(VersionOneName);
        }
    }

    public class when_an_aggregate_is_persisted_to_specific_bucket : using_a_configured_repository
    {
        private TestAggregate _testAggregate;

        private Guid _id;

        private string _bucket;

        protected override void Context()
        {
            base.Context();
            _id = Guid.NewGuid();
            _bucket = "TenantB";
            _testAggregate = new TestAggregate(_id, "Test");
        }

        protected override void Because()
        {
            _repository.Save(_bucket, _testAggregate, Guid.NewGuid(), null);
        }

        [Fact]
        public void should_be_returned_when_loaded_by_id()
        {
            _repository.GetById<TestAggregate>(_bucket, _id).Name.Should().Be(_testAggregate.Name);
        }
    }

    /// <summary>
    /// Idempotency Check:
    /// Internally a DuplicateCommitException will be raised and catch by the repository,
    /// the whole commit will be discarded, we assume the it's the same commit issued twice.
    /// 
    /// Issue: #4
    /// </summary>
    public class when_an_aggregate_is_persisted_using_the_same_commitId_twice : using_a_configured_repository
    {
        private TestAggregate _testAggregate;

        private Guid _id;

        protected override void Context()
        {
            base.Context();
            _id = Guid.NewGuid();
            _testAggregate = new TestAggregate(_id, "Test");
        }

        protected override void Because()
        {
            var commitId = Guid.NewGuid();
            _repository.Save(_testAggregate, commitId, null);

            _testAggregate.ChangeName("one");

            _repository.Save(_testAggregate, commitId);
        }

        [Fact]
        public void the_second_commit_was_silently_discarded_and_not_written_to_database()
        {
            var aggregate = _repository.GetById<TestAggregate>(_id);
            aggregate.Name.Should().Be("Test");
            aggregate.Version.Should().Be(1);
        }

        [Fact]
        public void the_aggregate_still_has_pending_changes()
        {
            var uncommittedEvents = ((IAggregate)_testAggregate).GetUncommittedEvents();
            uncommittedEvents.Should().NotBeEmpty();
            var enumerator = uncommittedEvents.GetEnumerator();
            enumerator.MoveNext();
            enumerator.Current.Should().BeOfType<NameChangedEvent>();
        }
    }

    public class when_an_aggregate_is_persisted_by_two_repositories : SpecificationBase
    {
        protected IRepository _repository1;
        protected IRepository _repository2;

        protected IStoreEvents _storeEvents;
        private Guid _aggregateId;
        private TestAggregate aggregate;
        private Exception _thrown;

        protected override void Context()
        {
            base.Context();

            this._storeEvents = Wireup.Init().UsingInMemoryPersistence().Build();
            this._repository1 = new EventStoreRepository(this._storeEvents, new AggregateFactory(), new ConflictDetector());
            this._repository2 = new EventStoreRepository(this._storeEvents, new AggregateFactory(), new ConflictDetector());

            _aggregateId = Guid.NewGuid();
            aggregate = new TestAggregate(_aggregateId, "my name is..");
        }

        protected override void Because()
        {
            _repository1.Save(aggregate, Guid.NewGuid());
            aggregate.ChangeName("one");

            _thrown = Catch.Exception(() => _repository2.Save(aggregate, Guid.NewGuid()));
        }

        [Fact]
        public void should_not_throw_a_ConflictingCommandException()
        {
            _thrown.Should().BeNull();
        }

        [Fact]
        public void should_have_updated_name_if_loaded_by_repository_that_saved_it_last()
        {
            _repository2.GetById<TestAggregate>(_aggregateId).Name.Should().Be("one");
        }

        /// <summary>
        /// current repository implementation act as a session cache!
        /// </summary>
        [Fact]
        public void should_have_original_name_if_loaded_by_repository_that_saved_it_first()
        {
            _repository1.GetById<TestAggregate>(_aggregateId).Name.Should().Be("my name is..");
        }
    }

    public class when_an_aggregate_is_persisted_concurrently_by_two_clients : SpecificationBase
    {
        protected IRepository _repository1;
        protected IRepository _repository2;

        protected IStoreEvents _storeEvents;
        private Guid _aggregateId;
        private Exception _thrown;

        protected override void Context()
        {
            base.Context();

            this._storeEvents = Wireup.Init().UsingInMemoryPersistence().Build();
            this._repository1 = new EventStoreRepository(this._storeEvents, new AggregateFactory(), new ConflictDetector());
            this._repository2 = new EventStoreRepository(this._storeEvents, new AggregateFactory(), new ConflictDetector());

            _aggregateId = Guid.NewGuid();
            var aggregate = new TestAggregate(_aggregateId, "my name is..");
            _repository1.Save(aggregate, Guid.NewGuid());
        }

        protected override void Because()
        {
            var agg1 = _repository1.GetById<TestAggregate>(_aggregateId);
            var agg2 = _repository2.GetById<TestAggregate>(_aggregateId);
            agg1.ChangeName("one");
            agg2.ChangeName("two");

            _repository1.Save(agg1, Guid.NewGuid());

            _thrown = Catch.Exception(() => _repository2.Save(agg2, Guid.NewGuid()));
        }

        [Fact]
        public void should_throw_a_ConflictingCommandException()
        {
            _thrown.Should().BeOfType<ConflictingCommandException>();
        }
    }

    public class when_an_aggregate_is_persisted_concurrently_by_two_clients_using_new_operator : SpecificationBase
    {
        protected IRepository _repository1;
        protected IRepository _repository2;

        protected IStoreEvents _storeEvents;
        private Guid _aggregateId;
        private Exception _thrown;

        protected override void Context()
        {
            base.Context();

            this._storeEvents = Wireup.Init().UsingInMemoryPersistence().Build();
            this._repository1 = new EventStoreRepository(this._storeEvents, new AggregateFactory(), new ConflictDetector());
            this._repository2 = new EventStoreRepository(this._storeEvents, new AggregateFactory(), new ConflictDetector());

            _aggregateId = Guid.NewGuid();
            var aggregate = new TestAggregate(_aggregateId, "my name is..");
            _repository1.Save(aggregate, Guid.NewGuid());
        }

        protected override void Because()
        {
            var agg1 = _repository1.GetById<TestAggregate>(_aggregateId);
            var agg2 = new TestAggregate(_aggregateId, "two");
            agg1.ChangeName("one");

            _repository1.Save(agg1, Guid.NewGuid());

            _thrown = Catch.Exception(() => _repository2.Save(agg2, Guid.NewGuid()));
        }

        [Fact]
        public void should_throw_a_ConflictingCommandException()
        {
            _thrown.Should().BeOfType<ConflictingCommandException>();
        }
    }

    public class when_the_same_aggregate_is_created_and_persisted_concurrently : SpecificationBase
    {
        protected IRepository _repository1;
        protected IRepository _repository2;

        protected IStoreEvents _storeEvents;
        private Guid _aggregateId;
        private Exception _thrown;

        protected override void Context()
        {
            base.Context();

            this._storeEvents = Wireup.Init().UsingInMemoryPersistence().Build();
            this._repository1 = new EventStoreRepository(this._storeEvents, new AggregateFactory(), new ConflictDetector());
            this._repository2 = new EventStoreRepository(this._storeEvents, new AggregateFactory(), new ConflictDetector());

            _aggregateId = Guid.NewGuid();
        }


        protected override void Because()
        {
            var agg1 = new TestAggregate(_aggregateId, "one");
            var agg2 = new TestAggregate(_aggregateId, "two");

            _repository1.Save(agg1, Guid.NewGuid());

            _thrown = Catch.Exception(() => _repository2.Save(agg2, Guid.NewGuid()));
        }

        [Fact]
        public void should_throw_a_ConflictingCommandException()
        {
            _thrown.Should().BeOfType<ConflictingCommandException>();
        }
    }

    public class when_aggregate_is_reloaded_with_snapshot : using_a_configured_repository
    {
        private TestAggregate _testAggregate;
        private TestAggregate _reloadedAggregate;
        private Guid _id;

        protected override void Context()
        {
            base.Context();
            _id = Guid.NewGuid();
            _testAggregate = new TestAggregate(_id, "Test");
            _repository.Save(_testAggregate, Guid.NewGuid()); //save at version 1.
        }

        protected override void Because()
        {
            var otherRepository = CreateRepository();
            var aggregate = otherRepository.GetById<TestAggregate>(_id); //load at version 1
            aggregate.ChangeName("Name changed");
            otherRepository.Save(Bucket.Default, aggregate, Guid.NewGuid()); //save in version 2
            //Now save the snapshot.

            var memento = ((IAggregate)aggregate).GetSnapshot();
            var snapshot = new Snapshot(Bucket.Default, aggregate.Id.ToString(), aggregate.Version, memento);

            _storeEvents.Advanced.AddSnapshot(snapshot);

            //now reload, 
            _reloadedAggregate = otherRepository.GetById<TestAggregate>(_id);
        }

        [Fact]
        public void should_have_correct_version()
        {
            _reloadedAggregate.Version.Should().Be(2);
        }
    }
}