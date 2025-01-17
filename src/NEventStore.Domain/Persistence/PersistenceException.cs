namespace NEventStore.Domain.Persistence
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    ///   Represents a general failure of the persistence infrastructure.
    /// </summary>
    [Serializable]
    public class PersistenceException : Exception
    {
        /// <summary>
        ///   Initializes a new instance of the PersistenceException class.
        /// </summary>
        public PersistenceException()
        { }

        /// <summary>
        ///   Initializes a new instance of the PersistenceException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public PersistenceException(string message)
            : base(message)
        { }

        /// <summary>
        ///   Initializes a new instance of the PersistenceException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The message that is the cause of the current exception.</param>
        public PersistenceException(string message, Exception innerException)
            : base(message, innerException)
        { }

#if !NETSTANDARD1_6
        /// <summary>
        ///   Initializes a new instance of the PersistenceException class.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data of the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected PersistenceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
#endif
    }
}