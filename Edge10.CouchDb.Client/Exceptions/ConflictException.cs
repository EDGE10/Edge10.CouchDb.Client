using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Edge10.CouchDb.Client.Exceptions
{
	/// <summary>
	/// An exception class thrown when there is a data conflict.
	/// </summary>
	[Serializable]
	public class ConflictException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ConflictException"/> class.
		/// </summary>
		public ConflictException() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ConflictException"/> class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public ConflictException(string message) : base(message) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ConflictException"/> class.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
		public ConflictException(string message, Exception innerException) : base(message, innerException) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ConflictException"/> class.
		/// </summary>
		/// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
		/// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
		[ExcludeFromCodeCoverage]
		protected ConflictException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}