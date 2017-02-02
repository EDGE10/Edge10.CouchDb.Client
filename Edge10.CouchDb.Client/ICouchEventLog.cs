using System;

namespace Edge10.CouchDb.Client
{
	/// <summary>
	/// Handles couch-related events
	/// </summary>
	public interface ICouchEventLog
	{
		/// <summary>
		/// Logs a view-related event.
		/// </summary>
		/// <param name="parameters">The view parameters parameters.</param>
		/// <returns>A token to be disposed when the work is complete.</returns>
		IDisposable LogViewEvent(IViewParameters parameters);

		/// <summary>
		/// Logs a document-related event.
		/// </summary>
		/// <param name="documentId">The document identifier.</param>
		/// <param name="action">The action.</param>
		/// <returns>A token to be disposed when the work is complete.</returns>
		IDisposable LogDocumentEvent(string documentId, string action);
	}
}