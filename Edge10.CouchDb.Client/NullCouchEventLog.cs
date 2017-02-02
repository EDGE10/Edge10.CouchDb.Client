using System;
using Edge10.CouchDb.Client.Utils;

namespace Edge10.CouchDb.Client
{
	/// <summary>
	/// Null-pattern implementation of <see cref="ICouchEventLog"/>.
	/// </summary>
	public class NullCouchEventLog : ICouchEventLog
	{
		public static ICouchEventLog Instance = new NullCouchEventLog();

		private NullCouchEventLog()
		{ }

		/// <summary>
		/// Logs a view-related event.
		/// </summary>
		/// <param name="parameters">The view parameters parameters.</param>
		/// <returns>
		/// A token to be disposed when the work is complete.
		/// </returns>
		public IDisposable LogViewEvent(IViewParameters parameters)
		{
			return DisposableWrapper<object>.NoOp;
		}

		/// <summary>
		/// Logs a document-related event.
		/// </summary>
		/// <param name="documentId">The document identifier.</param>
		/// <param name="action">The action.</param>
		/// <returns>
		/// A token to be disposed when the work is complete.
		/// </returns>
		public IDisposable LogDocumentEvent(string documentId, string action)
		{
			return DisposableWrapper<object>.NoOp;
		}
	}
}