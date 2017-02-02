using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Edge10.CouchDb.Client.Utils;
using log4net;

namespace Edge10.CouchDb.Client
{
	/// <summary>
	/// Couch event log with duration details.
	/// </summary>
	[ExcludeFromCodeCoverage]
	public class CouchTimelineEventLog : ICouchEventLog
	{
		private readonly ILog _log;

		/// <summary>
		/// Creates new instnce of <see cref="CouchTimelineEventLog"/>
		/// </summary>
		public CouchTimelineEventLog(ILog log)
		{
			_log = log;
		}

		/// <summary>
		/// Logs the view.
		/// </summary>
		/// <param name="parameters">The parameters.</param>
		/// <returns></returns>
		public IDisposable LogViewEvent(IViewParameters parameters)
		{
			return Time(
				string.Format(CultureInfo.InvariantCulture, "{0}/{1}", parameters.DesignDocument, parameters.ViewName),
				parameters.CreateQueryString());
		}

		/// <summary>
		/// Logs the document action.
		/// </summary>
		/// <param name="documentId">The document identifier.</param>
		/// <param name="action">The action.</param>
		/// <returns></returns>
		public IDisposable LogDocumentEvent(string documentId, string action)
		{
			return Time(action, documentId);
		}

		private IDisposable Time(string eventName, string eventSubText)
		{
			var start = DateTime.UtcNow;

			return new DisposableWrapper<object>(null, () =>
			{
				var end = DateTime.UtcNow;
				_log.Debug($"Couch event: {eventName}, details: {eventSubText}, duration: {end - start}");
			});
		}
	}
}
