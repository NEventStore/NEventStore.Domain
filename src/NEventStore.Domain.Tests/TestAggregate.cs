namespace NEventStore.Domain.Tests
{
    using System;
    using NEventStore.Domain.Core;

    internal class TestAggregate : AggregateBase
	{
		private TestAggregate(Guid id)
		{
			this.Id = id;
		}

		public TestAggregate(Guid id, string name)
			: this(id)
		{
			this.RaiseEvent(new TestAggregateCreatedEvent { Id = this.Id, Name = name });
		}

        /// <summary>
        /// State should be private, but this is a Test Aggregate and we need a way to probe for it
        /// </summary>
		public string Name { get; set; }

		public void ChangeName(string newName)
		{
			this.RaiseEvent(new NameChangedEvent { Id = this.Id, Name = newName });
		}

		private void Apply(TestAggregateCreatedEvent @event)
		{
			this.Name = @event.Name;
		}

		private void Apply(NameChangedEvent @event)
		{
			this.Name = @event.Name;
		}
	}

	public interface IDomainEvent
	{
        /// <summary>
        /// technically the Id is not really needed
        /// you need to know it up-front to create the Aggregate
        /// and to load the Stream, nonetheless you need to 
        /// identify the aggregate that raised the event in the rest of the program.
        /// </summary>
        Guid Id { get; set; }
    }

	[Serializable]
	public class NameChangedEvent : IDomainEvent
	{
        public Guid Id { get; set; }

        public string Name { get; set; }
	}

	[Serializable]
	public class TestAggregateCreatedEvent : IDomainEvent
	{
		public Guid Id { get; set; }

		public string Name { get; set; }
	}
}