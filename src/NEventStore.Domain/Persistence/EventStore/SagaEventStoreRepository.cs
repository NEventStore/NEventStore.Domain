namespace NEventStore.Domain.Persistence.EventStore
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using NEventStore.Persistence;

	public class SagaEventStoreRepository : ISagaRepository, IDisposable
	{
		private const string SagaTypeHeader = "SagaType";

		private string UndispatchedMessageHeader = "UndispatchedMessage.";

		private readonly IStoreEvents _eventStore;

		private readonly IConstructSagas _factory;

		private readonly IDictionary<string, IEventStream> _streams = new Dictionary<string, IEventStream>();

		public SagaEventStoreRepository(IStoreEvents eventStore, IConstructSagas factory)
		{
			_eventStore = eventStore;
			_factory = factory;
		}

		/// <summary>
		/// Creates and instance of the Saga Repository
		/// </summary>
		/// <param name="eventStore"></param>
		/// <param name="factory"></param>
		/// <param name="undispatchedMessageHeader">allows the user to specify the prefix used to store the undispatched commands inside the Commit.Headers dictionary.
		/// It is useful for all those databases that do not allow to have . (dot) in property names (like MongoDB, if we chose to not serialize the Headers dictionary as
		/// ArrayOfArrays).</param>
		public SagaEventStoreRepository(IStoreEvents eventStore, IConstructSagas factory, string undispatchedMessageHeader)
			: this(eventStore, factory)
		{
			UndispatchedMessageHeader = undispatchedMessageHeader;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public TSaga GetById<TSaga>(string bucketId, string sagaId) where TSaga : class, ISaga
		{
			return BuildSaga<TSaga>(sagaId, OpenStream(bucketId, sagaId));
		}

		public void Save(string bucketId, ISaga saga, Guid commitId, Action<IDictionary<string, object>> updateHeaders)
		{
			if (saga == null)
			{
				throw new ArgumentNullException("saga", ExceptionMessages.NullArgument);
			}

			Dictionary<string, object> headers = PrepareHeaders(saga, updateHeaders);
			IEventStream stream = PrepareStream(bucketId, saga, headers);

			Persist(stream, commitId);

			saga.ClearUncommittedEvents();
			saga.ClearUndispatchedMessages();
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}

			lock (_streams)
			{
				foreach (var stream in _streams)
				{
					stream.Value.Dispose();
				}

				_streams.Clear();
			}
		}

		private IEventStream OpenStream(string bucketId, string sagaId)
		{
			IEventStream stream;
			var sagaKey = bucketId + "+" + sagaId;
			if (_streams.TryGetValue(sagaKey, out stream))
			{
				return stream;
			}

			try
			{
				stream = _eventStore.OpenStream(bucketId, sagaId, 0, int.MaxValue);
			}
			catch (StreamNotFoundException)
			{
				stream = _eventStore.CreateStream(bucketId, sagaId);
			}

			return _streams[sagaKey] = stream;
		}

		private TSaga BuildSaga<TSaga>(string sagaId, IEventStream stream) where TSaga : class, ISaga
		{
			var saga = (TSaga)_factory.Build(typeof(TSaga), sagaId);
			foreach (var @event in stream.CommittedEvents.Select(x => x.Body))
			{
				saga.Transition(@event);
			}

			saga.ClearUncommittedEvents();
			saga.ClearUndispatchedMessages();

			return saga;
		}

		private Dictionary<string, object> PrepareHeaders(
			ISaga saga, Action<IDictionary<string, object>> updateHeaders)
		{
			var headers = new Dictionary<string, object>();

			headers[SagaTypeHeader] = saga.GetType().FullName;
			if (updateHeaders != null)
			{
				updateHeaders(headers);
			}

			int i = 0;
			foreach (var command in saga.GetUndispatchedMessages())
			{
				headers[UndispatchedMessageHeader + i++] = command;
			}

			return headers;
		}

		private IEventStream PrepareStream(string bucketId, ISaga saga, Dictionary<string, object> headers)
		{
			IEventStream stream;
			var sagaKey = bucketId + "+" + saga.Id;
			if (!_streams.TryGetValue(sagaKey, out stream))
			{
				_streams[sagaKey] = stream = _eventStore.CreateStream(bucketId, saga.Id);
			}

			foreach (var item in headers)
			{
				stream.UncommittedHeaders[item.Key] = item.Value;
			}

			saga.GetUncommittedEvents().Cast<object>().Select(x => new EventMessage { Body = x }).ToList().ForEach(stream.Add);

			return stream;
		}

		private static void Persist(IEventStream stream, Guid commitId)
		{
			try
			{
				stream.CommitChanges(commitId);
			}
			catch (DuplicateCommitException)
			{
				stream.ClearChanges();
			}
			catch (StorageException e)
			{
				throw new PersistenceException(e.Message, e);
			}
		}
	}
}