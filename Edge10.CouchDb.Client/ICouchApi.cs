using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Edge10.CouchDb.Client.Changes;
using Edge10.CouchDb.Client.Tasks;
using Newtonsoft.Json;

namespace Edge10.CouchDb.Client
{
	/// <summary>
	/// An interface for API objects that provide access to CouchDB.
	/// </summary>
	public interface ICouchApi : IDisposable
	{
		/// <summary>
		/// Checks the connection to the server.
		/// </summary>
		/// <returns></returns>
		Task CheckConnection();

		/// <summary>
		/// Checks whether or not an attachment named <paramref name="attachmentName"/> exists
		/// on a document with an ID of <paramref name="documentId"/>.
		/// </summary>
		/// <param name="documentId">The document ID.</param>
		/// <param name="attachmentName">The name of the attachment.</param>
		/// <returns><c>true</c> if the attachment exists; otherwise, <c>false</c>.</returns>
		Task<bool> AttachmentExistsAsync(string documentId, string attachmentName);

		/// <summary>
		/// Gets the stream data for an attachment named <paramref name="attachmentName"/>
		/// on a document with an ID of <paramref name="documentId"/>.
		/// </summary>
		/// <param name="documentId">The document ID.</param>
		/// <param name="attachmentName">The name of the attachment.</param>
		/// <returns>The stream content of the attachment.</returns>
		Task<Stream> GetAttachmentStreamAsync(string documentId, string attachmentName);

		/// <summary>
		/// Tries to get the stream data for an attachment named <paramref name="attachmentName"/>
		/// on a document with an ID of <paramref name="documentId"/>.
		/// </summary>
		/// <param name="documentId">The document ID.</param>
		/// <param name="attachmentName">The name of the attachment.</param>
		/// <returns>The stream content of the attachment, or <c>null</c> if the attachment does not exist.</returns>
		Task<Stream> TryGetAttachmentStreamAsync(string documentId, string attachmentName);

		/// <summary>
		/// Gets an attachment named <paramref name="attachmentName"/>
		/// on a document with an ID of <paramref name="documentId"/>.
		/// </summary>
		/// <param name="documentId">The document ID.</param>
		/// <param name="attachmentName">The name of the attachment.</param>
		/// <returns>The content of the attachment.</returns>
		Task<HttpContent> GetAttachmentAsync(string documentId, string attachmentName);

		/// <summary>
		/// Deletes the attachment named <paramref name="attachmentName"/> from the document
		/// with an ID of <paramref name="documentId"/>.
		/// </summary>
		/// <remarks>
		/// This will resolve the current document revision prior to deleting.
		/// </remarks>
		/// <param name="documentId">The document ID.</param>
		/// <param name="attachmentName">Name of the attachment.</param>
		/// <returns></returns>
		Task DeleteAttachmentAsync(string documentId, string attachmentName);

		/// <summary>
		/// Tries to get an attachment named <paramref name="attachmentName"/>
		/// on a document with an ID of <paramref name="documentId"/>.
		/// </summary>
		/// <param name="documentId">The document ID.</param>
		/// <param name="attachmentName">The name of the attachment.</param>
		/// <returns>The content of the attachment, or <c>null</c> if the attachment does not exist.</returns>
		Task<HttpContent> TryGetAttachmentAsync(string documentId, string attachmentName);

		/// <summary>
		/// Executes the view defined by <paramref name="viewParameters" />, then serializes
		/// and returns each row in the result to an instance of <typeparamref name="TRow" />.
		/// </summary>
		/// <typeparam name="TRow">The type of the row.</typeparam>
		/// <param name="viewParameters">The view parameters.</param>
		/// <returns>The rows returned by the view.</returns>
		Task<IEnumerable<TRow>> GetViewRowsAsync<TRow>(IViewParameters viewParameters);

		/// <summary>
		/// Executes the view defined by <paramref name="viewParameters" />, then serializes
		/// and returns each document in the result to an instance of <typeparamref name="TRow" />.
		/// Should be used with IncludeDocs set to <c>true</c>.
		/// </summary>
		/// <typeparam name="TDocument">The type of the document.</typeparam>
		/// <param name="viewParameters">The view parameters.</param>
		/// <returns>The documents returned by the view.</returns>
		Task<IEnumerable<TDocument>> GetViewDocumentsAsync<TDocument>(IViewParameters viewParameters);

		/// <summary>
		/// Executes the view defined by <paramref name="viewParameters" />, then serializes
		/// and returns each document in the result to an instance of <typeparamref name="TDocument" />.
		/// Should be used with IncludeDocs set to<c>true</c>.
		/// </summary>
		/// <typeparam name="TDocument">The type of the document</typeparam>
		/// <returns>The paged result returned by view.</returns>
		Task<IPagedResult<TDocument>> GetPagedViewDocumentsAsync<TDocument>(IViewParameters viewParameters);

		/// <summary>
		/// Executes the view defined by <paramref name="viewParameters" /> and returns the
		/// IDs of the returned documents.
		/// </summary>
		/// <param name="viewParameters">The view parameters.</param>
		/// <returns>The IDs of the documents returned by the view.</returns>
		Task<IEnumerable<string>> GetViewDocumentIdsAsync(IViewParameters viewParameters);

		/// <summary>
		/// Retrieves the document with the specified <paramref name="documentId"/> then serializes
		/// it into an instance of <typeparamref name="TDocument"/>.
		/// </summary>
		/// <typeparam name="TDocument">The type of the document.</typeparam>
		/// <param name="documentId">The document ID.</param>
		/// <returns>The matching document.</returns>
		Task<TDocument> GetDocumentAsync<TDocument>(string documentId);

		/// <summary>
		/// Retrieves all documents with the specified <paramref name="documentIds"/> then serializes
		/// it into an instance of <typeparamref name="TDocument"/>.
		/// </summary>
		/// <typeparam name="TDocument">The type of the document.</typeparam>
		/// <param name="documentIds">The IDs of the documents to retrieve.</param>
		/// <returns>The matching documents.</returns>
		Task<IEnumerable<TDocument>> GetDocumentsAsync<TDocument>(IEnumerable<string> documentIds);

		/// <summary>
		/// Retrieves the document with the specified <paramref name="documentId" /> then serializes
		/// it into an instance of <typeparamref name="TDocument" />.
		/// </summary>
		/// <typeparam name="TDocument">The type of the document.</typeparam>
		/// <param name="documentId">The document ID.</param>
		/// <param name="revision">The revision.</param>
		/// <returns>The matching document.</returns>
		Task<TDocument> GetDocumentAsync<TDocument>(string documentId, string revision);

		/// <summary>
		/// Retrieves the document with the specified <paramref name="documentId"/> then serializes
		/// it into an instance of <typeparamref name="TDocument"/>.
		/// </summary>
		/// <typeparam name="TDocument">The type of the document.</typeparam>
		/// <param name="documentId">The document ID.</param>
		/// <returns>The matching document.</returns>
		Task<TDocument> TryGetDocumentAsync<TDocument>(string documentId);

		/// <summary>
		/// Puts the attachmentName name <paramref name="attachmentName"/> 
		/// with is content <paramref name="httpContent"/>
		/// to the document id <paramref name="documentId"/>
		/// </summary>
		/// <param name="documentId">The document ID.</param>
		/// <param name="attachmentName">The name of the attachment.</param>
		/// <param name="httpContent">The content to send</param>
		/// <returns></returns>
		Task PutAsync(string documentId, string attachmentName, HttpContent httpContent);

		/// <summary>
		/// Checks whether or not an document named <paramref name="documentId"/> exists
		/// on a database
		/// </summary>
		/// <param name="documentId">The document ID.</param>
		/// <returns><c>true</c> if the document exists; otherwise, <c>false</c>.</returns>
		Task<bool> DocumentExistsAsync(string documentId);

		/// <summary>
		/// Creates an document with the id <paramref name="documentId"/>
		/// </summary>
		/// <param name="documentId">The document ID.</param>
		Task CreateDocumentAsync(string documentId);

		/// <summary>
		/// Creates a new document containing <param name="document" />
		/// </summary>
		/// <typeparam name="TDocument">The type of the document.</typeparam>
		/// <param name="document">The document.</param>
		Task CreateDocumentAsync<TDocument>(TDocument document)
			where TDocument : ICouchModel;

		/// <summary>
		/// Updates the specified <param name="document" />.
		/// </summary>
		/// <typeparam name="TDocument">The type of the document.</typeparam>
		/// <param name="document">The updated document content.</param>
		Task UpdateDocumentAsync<TDocument>(TDocument document)
			where TDocument : ICouchModel;

		/// <summary>
		/// Performs a bulk update for the specified collection of <param name="documents" />.
		/// </summary>
		/// <typeparam name="TDocument">The type of the document.</typeparam>
		/// <param name="documents">The updated documents.</param>
		Task BulkUpdateAsync<TDocument>(IEnumerable<TDocument> documents)
			where TDocument : ICouchModel;

		/// <summary>
		/// Gets the data from the _changes feed.
		/// </summary>
		/// <typeparam name="TDocument">The type of the changed document.</typeparam>
		/// <param name="parameters">The parameters.</param>
		/// <returns>
		/// Details of the changes.
		/// </returns>
		Task<ChangesResult<TDocument>> GetChangesAsync<TDocument>(IChangesParameters parameters);

		/// <summary>
		/// Gets the data from the _changes feed.
		/// </summary>
		/// <param name="parameters">The parameters.</param>
		/// <returns>Details of the changes.</returns>
		Task<ChangesResult> GetChangesAsync(IChangesParameters parameters);

		/// <summary>
		/// Gets the revision of the latest change made to the docuument.
		/// </summary>
		/// <param name="documentId">The ID of the document.</param>
		/// <returns>
		/// The latest revision, or <c>null</c> if no change can be found.
		/// </returns>
		Task<ChangeRevision> GetLatestDocumentRevision(string documentId);

		/// <summary>
		/// Gets the revisions of the latest change made to each document.
		/// </summary>
		/// <param name="documentIds">The IDs of the documents.</param>
		/// <returns>
		/// The latest revision for each document, or <c>null</c> if no change can be found.
		/// </returns>
		Task<IDictionary<string, ChangeRevision>> GetLatestDocumentRevisions(IEnumerable<string> documentIds);

		/// <summary>
		/// Executes the specified view and parses the result into <typeparamref name="TResult"/>.
		/// </summary>
		/// <typeparam name="TResult">The type of the result.</typeparam>
		/// <param name="viewParameters">The view parameters.</param>
		/// <returns>The result of the list.</returns>
		Task<TResult> GetListResult<TResult>(IViewParameters viewParameters);

		/// <summary>
		/// Gets the active replication tasks.
		/// </summary>
		/// <returns>The active replication tasks.</returns>
		Task<IEnumerable<ReplicationTask>> GetActiveReplicationTasks();

		/// <summary>
		/// Applies the specified <paramref name="settingsChanges"/> to the serializer (combined
		/// with the defaults) until the returned token is disposed.
		/// </summary>
		/// <param name="settingsChanges">The settings to be applied.</param>
		/// <returns>A token that, when disposed, removes the applied settings.</returns>
		IDisposable CustomSettings(Action<JsonSerializerSettings> settingsChanges);

		/// <summary>
		/// Gets a list of custom converters that are applied during serialization.
		/// </summary>
		/// <value>
		/// The converters.
		/// </value>
		IList<JsonConverter> Converters { get; }
	}
}