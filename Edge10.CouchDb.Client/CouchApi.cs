using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Edge10.CouchDb.Client.Changes;
using Edge10.CouchDb.Client.Exceptions;
using Edge10.CouchDb.Client.Results;
using Edge10.CouchDb.Client.Tasks;
using Edge10.CouchDb.Client.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Edge10.CouchDb.Client
{
	/// <summary>
	/// An implementation of <see cref="ICouchApi" /> that uses a <see cref="HttpClient" />
	/// to communicate with Couch.
	/// </summary>
	public sealed class CouchApi : ICouchApi
	{
		private IHttpClientFacade _client;
		private readonly ICouchEventLog _eventLog;
		private readonly SerializationStrategy _serializationStrategy;
		private readonly string _url;
		private readonly string _databaseName;
		private Action<JsonSerializerSettings> _settingsChanges;

		/// <summary>
		/// Initializes a new instance of the <see cref="CouchApi" /> class.
		/// </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <param name="eventLog">The couch event log</param>
		/// <param name="serializationStrategy">The serialization strategy.</param>
		public CouchApi(ICouchDbConnectionStringBuilder connectionString, ICouchEventLog eventLog = null, SerializationStrategy serializationStrategy = null) 
			: this(connectionString, new HttpClientFacade(new HttpClientHandler()), eventLog ?? NullCouchEventLog.Instance, serializationStrategy)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CouchApi" /> class.
		/// </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <param name="httpClient">The HTTP client facade.</param>
		/// <param name="eventLog">The couch event log.</param>
		/// <param name="serializationStrategy">The serialization strategy.</param>
		internal CouchApi(ICouchDbConnectionStringBuilder connectionString, IHttpClientFacade httpClient, ICouchEventLog eventLog, SerializationStrategy serializationStrategy)
		{
			connectionString.ThrowIfNull(nameof(connectionString));
			httpClient.ThrowIfNull(nameof(httpClient));
			eventLog.ThrowIfNull(nameof(eventLog));

			_client                = httpClient;
			_eventLog              = eventLog;
			_serializationStrategy = serializationStrategy;
			_databaseName          = connectionString.DatabaseName;
			_url                   = GetServerUrl(connectionString);
			_client.Timeout        = TimeSpan.FromMinutes(20);

			_client.SetAuthorizationHeader(new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{connectionString.User}:{connectionString.Password}"))));
		}

		/// <summary>
		/// Checks the connection to the server.
		/// </summary>
		/// <returns></returns>
		public async Task CheckConnection()
		{
			var result = await _client.GetAsync($"{_url}/{_databaseName}");

			await result.ThrowErrorIfNotSuccess();
		}

		/// <summary>
		/// Checks whether or not an attachment named <paramref name="attachmentName" /> exists
		/// on a document with an ID of <paramref name="documentId" />.
		/// </summary>
		/// <param name="documentId">The document ID.</param>
		/// <param name="attachmentName">The name of the attachment.</param>
		/// <returns>
		///   <c>true</c> if the attachment exists; otherwise, <c>false</c>.
		/// </returns>
		public async Task<bool> AttachmentExistsAsync(string documentId, string attachmentName)
		{
			documentId.ThrowIfNullOrEmpty(nameof(documentId));
			attachmentName.ThrowIfNullOrEmpty(nameof(attachmentName));

			var result = await _client.GetAsync(
				GetAttachmentUrl(documentId, attachmentName),
				HttpCompletionOption.ResponseHeadersRead);

			using (result)
				return result.IsSuccessStatusCode;
		}

		/// <summary>
		/// Gets an attachment named <paramref name="attachmentName" />
		/// on a document with an ID of <paramref name="documentId" />.
		/// </summary>
		/// <param name="documentId">The document ID.</param>
		/// <param name="attachmentName">The name of the attachment.</param>
		/// <returns>
		/// The stream content of the attachment.
		/// </returns>
		public async Task<Stream> GetAttachmentStreamAsync(string documentId, string attachmentName)
		{
			return await GetAttachmentStream(documentId, attachmentName, throwOnError: true);
		}

		/// <summary>
		/// Tries to get the stream data for an attachment named <paramref name="attachmentName" />
		/// on a document with an ID of <paramref name="documentId" />.
		/// </summary>
		/// <param name="documentId">The document ID.</param>
		/// <param name="attachmentName">The name of the attachment.</param>
		/// <returns>
		/// The stream content of the attachment, or <c>null</c> if the attachment does not exist.
		/// </returns>
		public async Task<Stream> TryGetAttachmentStreamAsync(string documentId, string attachmentName)
		{
			return await GetAttachmentStream(documentId, attachmentName, throwOnError: false);
		}

		/// <summary>
		/// Gets an attachment named <paramref name="attachmentName" />
		/// on a document with an ID of <paramref name="documentId" />.
		/// </summary>
		/// <param name="documentId">The document ID.</param>
		/// <param name="attachmentName">The name of the attachment.</param>
		/// <returns>
		/// The stream content of the attachment.
		/// </returns>
		public async Task<HttpContent> GetAttachmentAsync(string documentId, string attachmentName)
		{
			return await GetAttachment(documentId, attachmentName, throwOnError: true);
		}

		/// <summary>
		/// Tries to get an attachment named <paramref name="attachmentName" />
		/// on a document with an ID of <paramref name="documentId" />.
		/// </summary>
		/// <param name="documentId">The document ID.</param>
		/// <param name="attachmentName">The name of the attachment.</param>
		/// <returns>
		/// The content of the attachment, or <c>null</c> if the attachment does not exist.
		/// </returns>
		public async Task<HttpContent> TryGetAttachmentAsync(string documentId, string attachmentName)
		{
			return await GetAttachment(documentId, attachmentName, throwOnError: false);
		}

		/// <summary>
		/// Deletes the attachment named <paramref name="attachmentName" /> from the document
		/// with an ID of <paramref name="documentId" />.
		/// </summary>
		/// <param name="documentId">The document ID.</param>
		/// <param name="attachmentName">Name of the attachment.</param>
		/// <returns></returns>
		/// <remarks>
		/// This will resolve the current document revision prior to deleting.
		/// </remarks>
		public async Task DeleteAttachmentAsync(string documentId, string attachmentName)
		{
			documentId.ThrowIfNullOrEmpty(nameof(documentId));
			attachmentName.ThrowIfNullOrEmpty(nameof(attachmentName));

			var requestUri = await GetAttachmentUrlWithRevision(documentId, attachmentName);
			var response   = await _client.DeleteAsync(requestUri);

			await response.ThrowErrorIfNotSuccess();
		}

		/// <summary>
		/// Executes the view defined by <paramref name="viewParameters" />, then deserializes
		/// and returns each row in the result to an instance of <typeparamref name="TRow" />.
		/// </summary>
		/// <typeparam name="TRow">The type of the row.</typeparam>
		/// <param name="viewParameters">The view parameters.</param>
		/// <returns>
		/// The rows returned by the view.
		/// </returns>
		public async Task<IEnumerable<TRow>> GetViewRowsAsync<TRow>(IViewParameters viewParameters)
		{
			viewParameters.ThrowIfNull(nameof(viewParameters));

			//we only use the value below so there's no need to fetch the documents
			viewParameters.IncludeDocs = false;

			var viewResult = await GetListResult<ViewResult<object, TRow>>(viewParameters);
			return viewResult.Rows.Select(r => r.Value);
		}

		/// <summary>
		/// Executes the view defined by <paramref name="viewParameters" />, then serializes
		/// and returns each document in the result to an instance of <typeparamref name="TRow" />.
		/// Should be used with IncludeDocs set to <c>true</c>.
		/// </summary>
		/// <typeparam name="TDocument">The type of the document.</typeparam>
		/// <param name="viewParameters">The view parameters.</param>
		/// <returns>
		/// The documents returned by the view.
		/// </returns>
		public async Task<IEnumerable<TDocument>> GetViewDocumentsAsync<TDocument>(IViewParameters viewParameters)
		{
			var viewResult = await GetListResult<ViewResult<TDocument, object>>(viewParameters);
			return viewResult.Rows.Select(r => r.Document);
		}

		/// <summary>
		/// Executes the view defined by <paramref name="viewParameters" />, then serializes
		/// and returns each document in the result to an instance of <typeparamref name="TDocument" />.
		/// Should be used with IncludeDocs set to<c>true</c>.
		/// </summary>
		/// <typeparam name="TDocument">The type of the document</typeparam>
		/// <returns>The paged result returned by view.</returns>
		public async Task<IPagedResult<TDocument>> GetPagedViewDocumentsAsync<TDocument>(IViewParameters viewParameters)
		{
			viewParameters.Reduce      = false;
			var viewResult             = await GetListResult<ViewResult<TDocument, object>>(viewParameters);
			viewParameters.Reduce      = true;
			viewParameters.Skip        = null;
			viewParameters.Limit       = null;
			viewParameters.IncludeDocs = false;
			var countResult            = await GetListResult<ViewResult<object, long>>(viewParameters);
			var count                  = countResult.Rows.FirstOrDefault()?.Value ?? 0;
			return new PagedResult<TDocument>(viewResult.Rows.Select(r => r.Document).ToArray(), count);
		}

		/// <summary>
		/// Retrieves the document with the specified <paramref name="documentId" /> then serializes
		/// it into an instance of <typeparamref name="TDocument" />.
		/// </summary>
		/// <typeparam name="TDocument">The type of the document.</typeparam>
		/// <param name="documentId">The document ID.</param>
		/// <returns>The matching document.</returns>
		public async Task<TDocument> GetDocumentAsync<TDocument>(string documentId)
		{
			documentId.ThrowIfNullOrEmpty(nameof(documentId));
			var url = GetDocumentUrl(documentId);

			using (_eventLog.LogDocumentEvent(documentId, "get"))
			using (var result = await _client.GetAsync(url))
				return await GetSerializedResultContent<TDocument>(result);
		}

		/// <summary>
		/// Retrieves the document with the specified <paramref name="documentId" /> then serializes
		/// it into an instance of <typeparamref name="TDocument" />.
		/// </summary>
		/// <typeparam name="TDocument">The type of the document.</typeparam>
		/// <param name="documentId">The document ID.</param>
		/// <param name="revision">The revision.</param>
		/// <returns>
		/// The matching document.
		/// </returns>
		public async Task<TDocument> GetDocumentAsync<TDocument>(string documentId, string revision)
		{
			documentId.ThrowIfNullOrEmpty(nameof(documentId));
			revision.ThrowIfNullOrEmpty(nameof(revision));

			var url = GetDocumentUrl(documentId, revision);
			using (_eventLog.LogDocumentEvent(documentId, "get"))
			using (var result = await _client.GetAsync(url))
				return await GetSerializedResultContent<TDocument>(result);
		}

		/// <summary>
		/// Retrieves the document with the specified <paramref name="documentId" /> then serializes
		/// it into an instance of <typeparamref name="TDocument" />.
		/// </summary>
		/// <typeparam name="TDocument">The type of the document.</typeparam>
		/// <param name="documentId">The document ID.</param>
		/// <returns>The matching document.</returns>
		public async Task<TDocument> TryGetDocumentAsync<TDocument>(string documentId)
		{
			documentId.ThrowIfNullOrEmpty(nameof(documentId));

			var url = GetDocumentUrl(documentId);
			using (_eventLog.LogDocumentEvent(documentId, "get"))
			using (var result = await _client.GetAsync(url))
			{
				if (!result.IsSuccessStatusCode)
					return default(TDocument);

				return await GetSerializedResultContent<TDocument>(result);
			}
		}

		/// <summary>
		/// Puts the attachmentName name <paramref name="attachmentName"/> 
		/// with is content <paramref name="httpContent"/>
		/// to the document id <paramref name="documentId"/>
		/// </summary>
		/// <param name="documentId">The document ID.</param>
		/// <param name="attachmentName">The name of the attachment.</param>
		/// <param name="httpContent">The content to send</param>
		/// <returns></returns>
		public async Task PutAsync(string documentId, string attachmentName, HttpContent httpContent)
		{
			documentId.ThrowIfNullOrEmpty(nameof(documentId));
			attachmentName.ThrowIfNullOrEmpty(nameof(attachmentName));
			httpContent.ThrowIfNull(nameof(httpContent));

			var requestUriPutAsync   = await GetAttachmentUrlWithRevision(documentId, attachmentName);
			var httpResponsePutAsync = await _client.PutAsync(requestUriPutAsync, httpContent);

			await httpResponsePutAsync.ThrowErrorIfNotSuccess();
		}

		/// <summary>
		/// Checks whether or not an document named <paramref name="documentId"/> exists
		/// on a database
		/// </summary>
		/// <param name="documentId">The document ID.</param>
		/// <returns><c>true</c> if the document exists; otherwise, <c>false</c>.</returns>
		public async Task<bool> DocumentExistsAsync(string documentId)
		{
			documentId.ThrowIfNullOrEmpty(nameof(documentId));

			using (_eventLog.LogDocumentEvent(documentId, "get"))
			{
				var httpResponse = await _client.GetAsync(GetDocumentUrl(documentId));
				if (httpResponse == null) return false;
				return httpResponse.IsSuccessStatusCode;
			}
		}

		/// <summary>
		/// Creates an document with the id <paramref name="documentId"/>
		/// </summary>
		/// <param name="documentId">The document ID.</param>
		public async Task CreateDocumentAsync(string documentId)
		{
			documentId.ThrowIfNullOrEmpty(nameof(documentId));

			using (_eventLog.LogDocumentEvent(documentId, "create"))
			{
				var httpResponse = await _client.PutAsync(GetDocumentUrl(documentId),
														  new StringContent("{}", Encoding.UTF8, "application/json"));

				await httpResponse.ThrowErrorIfNotSuccess();
			}
		}

		/// <summary>
		/// Creates a new document containing <param name="document" />
		/// </summary>
		/// <typeparam name="TDocument"></typeparam>
		/// <param name="document"></param>
		/// <returns></returns>
		public Task CreateDocumentAsync<TDocument>(TDocument document)
			where TDocument : ICouchModel
		{
			document.ThrowIfNull(nameof(document));

			document.Id = string.IsNullOrWhiteSpace(document.Id) ? Guid.NewGuid().ToString() : document.Id;

			using (_eventLog.LogDocumentEvent(document.Id, "create"))
				return UpdateOrCreateDocumentAsync(document, true);
		}

		/// <summary>
		/// Updates the specified <param name="document" />.
		/// </summary>
		/// <typeparam name="TDocument">The type of the document</typeparam>
		/// <param name="document">The updated document content.</param>
		public Task UpdateDocumentAsync<TDocument>(TDocument document)
			where TDocument : ICouchModel
		{
			document.ThrowIfNull(nameof(document));

			using (_eventLog.LogDocumentEvent(document.Id, "update"))
				return UpdateOrCreateDocumentAsync(document, false);
		}

		private async Task UpdateOrCreateDocumentAsync<TDocument>(TDocument document, bool isNew)
			where TDocument : ICouchModel
		{
			document.ThrowIfNull(nameof(document));
			var documentUri = GetDocumentUrl(document.Id);
			if (!isNew && string.IsNullOrWhiteSpace(document.Rev))
			{
				var revision = await GetDocumentRevision(documentUri);
				document.Rev = revision;
			}

			document.Type = typeof(TDocument).Name;

			var content  = SerializeDocument(document);
			var response = await _client.PutAsync(documentUri, content);

			if (response.StatusCode == HttpStatusCode.Conflict)
				throw new ConflictException();

			await response.ThrowErrorIfNotSuccess();

			await UpdateDocumentRevision(document, response);
		}

		/// <summary>
		/// Performs a bulk update for the specified collection of <param name="documents" />.
		/// </summary>
		/// <typeparam name="TDocument">The type of the document.</typeparam>
		/// <param name="documents">The updated documents.</param>
		public async Task BulkUpdateAsync<TDocument>(IEnumerable<TDocument> documents) where TDocument : ICouchModel
		{
			documents.ThrowIfNull(nameof(documents));

			var docsArray = documents.ToArray();
			foreach (TDocument document in docsArray)
			{
				document.Type = typeof(TDocument).Name;
			}

			using (_eventLog.LogDocumentEvent(string.Join(", ", docsArray.Select(x => x.Id.ToString())), "bulk"))
			{
				var url      = $"{_url}/{_databaseName}/_bulk_docs";
				var content  = SerializeDocument(new { Docs = docsArray });
				var response = await _client.PostAsync(url, content);

				await response.ThrowErrorIfNotSuccess();

				var responseContent = await response.Content.ReadAsStringAsync();
				var updateResults   = JsonConvert.DeserializeObject<CouchBulkUpdateResponseItem[]>(responseContent);

				var conflictIds = updateResults.Where(x => x.Error == "conflict").Select(x => x.Id.ToString()).ToArray();
				if (conflictIds.Any())
					throw new ConflictException(string.Join(", ", conflictIds));

				var errorResults = updateResults.Where(x => !x.Ok && !string.IsNullOrWhiteSpace(x.Error)).ToArray();
				if (errorResults.Any())
				{
					var messages = errorResults.Select(x => $"id: {x.Id}, error: {x.Error}, reason: {x.Reason}");
					throw new HttpRequestException(string.Join("; ", messages));
				}

				var revisionsById = updateResults.ToDictionary(x => x.Id, x => x.Rev);
				foreach (var document in docsArray)
				{
					document.Rev = revisionsById[document.Id];
				}
			}
		}

		/// <summary>
		/// Gets the data from the _changes feed.
		/// </summary>
		/// <typeparam name="TDocument">The type of the changed document.</typeparam>
		/// <param name="parameters">The parameters.</param>
		/// <returns>
		/// Details of the changes.
		/// </returns>
		public async Task<ChangesResult<TDocument>> GetChangesAsync<TDocument>(IChangesParameters parameters)
		{
			var response = await GetChangesResponse(parameters);

			return await GetSerializedResultContent<ChangesResult<TDocument>>(response);
		}

		/// <summary>
		/// Gets the data from the _changes feed.
		/// </summary>
		/// <param name="parameters">The parameters.</param>
		/// <returns>
		/// Details of the changes.
		/// </returns>
		public async Task<ChangesResult> GetChangesAsync(IChangesParameters parameters)
		{
			var response = await GetChangesResponse(parameters);

			return await GetSerializedResultContent<ChangesResult>(response);
		}

		private async Task<HttpResponseMessage> GetChangesResponse(IChangesParameters parameters)
		{
			parameters.ThrowIfNull("parameters");

			var url = $"{_url}/{_databaseName}/_changes{parameters.CreateQueryString()}";

			var response = await _client.GetAsync(url);
			await response.ThrowErrorIfNotSuccess();
			return response;
		}

		/// <summary>
		/// Gets the revision of the latest change made to the docuument.
		/// </summary>
		/// <param name="documentId">The ID of the document.</param>
		/// <returns>
		/// The latest revision, or <c>null</c> if no change can be found.
		/// </returns>
		public async Task<ChangeRevision> GetLatestDocumentRevision(string documentId)
		{
			documentId.ThrowIfNullOrEmpty("documentId");

			var url = $"{_url}/{_databaseName}/_all_docs?keys=[%22{documentId}%22]";

			using (_eventLog.LogDocumentEvent(documentId, "get"))
			using (var response = await _client.GetAsync(url))
			{
				var viewResult = await GetSerializedResultContent<ViewResult<object, ChangeRevision>>(response);
				return viewResult.Rows.Select(r => r.Value).FirstOrDefault();
			}
		}

		/// <summary>
		/// Gets the revisions of the latest change made to each document.
		/// </summary>
		/// <param name="documentIds">The IDs of the documents.</param>
		/// <returns>
		/// The latest revision for each document, or <c>null</c> if no change can be found.
		/// </returns>
		public async Task<IDictionary<string, ChangeRevision>> GetLatestDocumentRevisions(IEnumerable<string> documentIds)
		{
			documentIds.ThrowIfNull(nameof(documentIds));

			var distinctIds = documentIds.Distinct().ToArray();
			if (!distinctIds.Any()) return new Dictionary<string, ChangeRevision>();

			var url = $"{_url}/{_databaseName}/_all_docs";

			if (distinctIds.Count() <= MaxDocumentsPerRequest)
			{
				var parameters = new
				{
					keys = distinctIds
				};

				using (var keysContent = new ObjectContent<dynamic>(parameters, new JsonMediaTypeFormatter()))
				using (var response = await _client.PostAsync(url, keysContent))
				{
					await response.ThrowErrorIfNotSuccess();

					var viewResult = await GetSerializedResultContent<ViewResult<object, ChangeRevision>>(response);
					var revisionsById = viewResult.Rows.ToDictionary(r => r.Id, r => r.Value);

					return distinctIds.ToDictionary(id => id, id => revisionsById.ContainsKey(id) ? revisionsById[id] : null);
				}
			}

			var idBlocks = SplitToBlocks(distinctIds, MaxDocumentsPerRequest);
			var results  = new Dictionary<string, ChangeRevision>();

			//note: running these sequentially to avoid multiple
			//simultaneous threads causing memory problems
			foreach (var idBlock in idBlocks)
			{
				var blockResults = await GetLatestDocumentRevisions(idBlock);
				foreach (var result in blockResults)
				{
					results.Add(result.Key, result.Value);
				}
			}

			return results;
		}

		/// <summary>
		/// Executes the view defined by <paramref name="viewParameters" /> and returns the
		/// IDs of the returned documents.
		/// </summary>
		/// <param name="viewParameters">The view parameters.</param>
		/// <returns>The IDs of the documents returned by the view.</returns>
		public async Task<IEnumerable<string>> GetViewDocumentIdsAsync(IViewParameters viewParameters)
		{
			var viewResult = await GetListResult<ViewResult<object, object>>(viewParameters);
			return viewResult.Rows.Select(r => r.Id);
		}

		/// <summary>
		/// Retrieves all documents with the specified <paramref name="documentIds"/> then serializes
		/// it into an instance of <typeparamref name="TDocument"/>.
		/// </summary>
		/// <typeparam name="TDocument">The type of the document.</typeparam>
		/// <param name="documentIds">The IDs of the documents to retrieve.</param>
		/// <returns>The matching documents.</returns>
		public async Task<IEnumerable<TDocument>> GetDocumentsAsync<TDocument>(IEnumerable<string> documentIds)
		{
			documentIds.ThrowIfNull(nameof(documentIds));

			var documentIdsArray = documentIds as string[] ?? documentIds.ToArray();
			if (!documentIdsArray.Any()) return Enumerable.Empty<TDocument>();

			var url = $"{_url}/{_databaseName}/_all_docs?include_docs=true";

			if (documentIdsArray.Length <= MaxDocumentsPerRequest)
			{
				var parameters = new
				{
					keys = documentIdsArray
				};

				using (var keysContent = new ObjectContent<dynamic>(parameters, new JsonMediaTypeFormatter()))
				using (var response = await _client.PostAsync(url, keysContent))
				{
					await response.ThrowErrorIfNotSuccess();

					var result = await GetSerializedResultContent<ViewResult<TDocument, object>>(response);
					return result.Rows.Select(r => r.Document).ToList();
				}
			}

			var idBlocks = SplitToBlocks(documentIdsArray, MaxDocumentsPerRequest);
			var results = new List<TDocument>();

			//note: running these sequentially to avoid multiple
			//simultaneous threads causing memory problems
			foreach (var idBlock in idBlocks)
				results.AddRange(await GetDocumentsAsync<TDocument>(idBlock));

			return results;
		}

		private IEnumerable<IEnumerable<T>> SplitToBlocks<T>(IEnumerable<T> sequence, int size)
		{
			var partition = new List<T>(size);
			foreach (var item in sequence)
			{
				partition.Add(item);
				if (partition.Count == size)
				{
					yield return partition;
					partition = new List<T>(size);
				}
			}

			if (partition.Count > 0)
				yield return partition;
		}

		/// <summary>
		/// Executes the specified view and parses the result into <typeparamref name="TResult" />.
		/// </summary>
		/// <typeparam name="TResult">The type of the result.</typeparam>
		/// <param name="viewParameters">The view parameters.</param>
		/// <returns>
		/// The result of the list.
		/// </returns>
		public async Task<TResult> GetListResult<TResult>(IViewParameters viewParameters)
		{
			viewParameters.ThrowIfNull(nameof(viewParameters));

			using (_eventLog.LogViewEvent(viewParameters))
			{
				var result = await GetViewHttpResponse(viewParameters);
				return GetSerializedContent<TResult>(result);
			}
		}

		/// <summary>
		/// Gets the active replication tasks.
		/// </summary>
		/// <returns>The active replication tasks.</returns>
		public async Task<IEnumerable<ReplicationTask>> GetActiveReplicationTasks()
		{
			return await GetActiveTasks<ReplicationTask>(ReplicationTask.TaskType);
		}

		/// <summary>
		/// Applies the specified <paramref name="settingsChanges"/> to the serializer (combined
		/// with the defaults) until the returned token is disposed.
		/// </summary>
		/// <param name="settingsChanges">The settings to be applied.</param>
		/// <returns>A token that, when disposed, removes the applied settings.</returns>
		public IDisposable CustomSettings(Action<JsonSerializerSettings> settingsChanges)
		{
			_settingsChanges = settingsChanges;
			return new DisposableWrapper<Action<JsonSerializerSettings>>(_settingsChanges, () =>
			{
				_settingsChanges = null;
			});
		}

		/// <summary>
		/// Gets or sets the maximum number of documents to retrieve per request.
		/// </summary>
		/// <value>
		/// The maximum documents per request.
		/// </value>
		public int MaxDocumentsPerRequest { get; set; } = 500;

		/// <summary>
		/// Gets a list of custom converters that are applied during serialization.
		/// </summary>
		/// <value>
		/// The converters.
		/// </value>
		public IList<JsonConverter> Converters { get; } = new List<JsonConverter>();

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			_client?.Dispose();

			_client = null;
		}

		private async Task<TData> GetSerializedResultContent<TData>(HttpResponseMessage result)
		{
			await result.ThrowErrorIfNotSuccess();

			var content = await result.Content.ReadAsStreamAsync();
			return GetSerializedContent<TData>(content);
		}

		private TData GetSerializedContent<TData>(Stream content)
		{
			using (var sr = _serializationStrategy != null ? _serializationStrategy.ReaderFactory(content) : new StreamReader(content))
			using (var jsonTextReader = new JsonTextReader(sr))
			{
				var serializer = CreateSerializer();
				return serializer.Deserialize<TData>(jsonTextReader);
			}
		}

		private async Task<Stream> GetViewHttpResponse(IViewParameters viewParameters)
		{
			var url = GetViewUrl(viewParameters);

			if (viewParameters.Keys != null)
			{
				var keyData = new { keys = viewParameters.Keys };

				using (var keysContent = new ObjectContent<dynamic>(keyData, new JsonMediaTypeFormatter()))
				using (var request = new HttpRequestMessage(HttpMethod.Post, url))
				{
					request.Content = keysContent;
					var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
					await response.ThrowErrorIfNotSuccess(CouchExceptionFactory);
					return await response.Content.ReadAsStreamAsync();
				}
			}

			return await _client.GetStreamAsync(url);
		}

		[ExcludeFromCodeCoverage]  //unable to test with null content
		private static Exception CouchExceptionFactory(HttpResponseMessage message, string content)
		{
			if (content?.ToLowerInvariant().IndexOf("timeout") != -1)
				return new CouchTimeoutException();

			return new HttpRequestException($"{(int)message.StatusCode} {message.ReasonPhrase} {content}");
		}

		private string GetViewUrl(IViewParameters viewParameters)
		{
			var viewName = string.IsNullOrEmpty(viewParameters.ListName) ? $"_view/{viewParameters.ViewName}" : $"_list/{viewParameters.ListName}/{viewParameters.ViewName}";

			var viewUrl = $"{_url}/{_databaseName}/_design/{viewParameters.DesignDocument}/{viewName}{viewParameters.CreateQueryString()}";

			return viewUrl;
		}

		private JsonSerializer CreateSerializer()
		{
			var settings = new JsonSerializerSettings
			{
				TypeNameHandling = TypeNameHandling.Objects,
				ContractResolver = new CamelCasePropertyNamesContractResolver()
			};

			foreach (var converter in Converters)
				settings.Converters.Add(converter);

			_settingsChanges?.Invoke(settings);

			return JsonSerializer.Create(settings);
		}

		private string GetAttachmentUrl(string documentId, string attachmentName)
		{
			return $"{_url}/{_databaseName}/{documentId}/{attachmentName}";
		}

		private static string GetServerUrl(ICouchDbConnectionStringBuilder connectionString)
		{
			return $"{((connectionString.Server.StartsWith("http://") || connectionString.Server.StartsWith("https://")) ? string.Empty : "http://")}{connectionString.Server}:{connectionString.Port}";
		}

		private async Task<Stream> GetAttachmentStream(string documentId, string attachmentName, bool throwOnError)
		{
			var content = await GetAttachment(documentId, attachmentName, throwOnError);
			if (content == null) return null;

			return await content.ReadAsStreamAsync();
		}

		private async Task<HttpContent> GetAttachment(string documentId, string attachmentName, bool throwOnError)
		{
			documentId.ThrowIfNullOrEmpty("documentId");
			attachmentName.ThrowIfNullOrEmpty("attachmentName");

			var result = await _client.GetAsync(GetAttachmentUrl(documentId, attachmentName));

			if (throwOnError)
				await result.ThrowErrorIfNotSuccess();
			else if (!result.IsSuccessStatusCode)
				return null;

			return result.Content;
		}

		private string GetDocumentUrl(string documentId)
		{
			return $"{_url}/{_databaseName}/{documentId}";
		}

		private string GetDocumentUrl(string documentId, string revision)
		{
			return $"{_url}/{_databaseName}/{documentId}?rev={revision}";
		}

		private async Task<string> GetDocumentRevision(string documentUri)
		{
			// getRevVersion - "A HEAD request returns basic information about the document, including its current revision".
			var httpResponseSendAsync = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Head, documentUri));
			await httpResponseSendAsync.ThrowErrorIfNotSuccess();

			return httpResponseSendAsync.Headers.ETag.Tag.Replace('\"', ' ').Trim();
		}

		private async Task<string> GetAttachmentUrlWithRevision(string documentId, string attachmentName)
		{
			var documentUri = GetDocumentUrl(documentId);
			var revision = await GetDocumentRevision(documentUri);

			var requestUriPutAsync = $"{documentUri}/{attachmentName}?rev={revision}";
			return requestUriPutAsync;
		}

		private HttpContent SerializeDocument<TDocument>(TDocument document)
		{
			var serializer = CreateSerializer();
			var builder    = new StringBuilder();

			using (var sw = _serializationStrategy != null ? _serializationStrategy.WriterFactory(builder) : new StringWriter(builder))
			using (var writer = new JsonTextWriter(sw))
				serializer.Serialize(writer, document);

			return new StringContent(builder.ToString(), Encoding.UTF8, "application/json");
		}

		private static async Task UpdateDocumentRevision<TDocument>(TDocument document, HttpResponseMessage response) where TDocument : ICouchModel
		{
			var responseContent = await response.Content.ReadAsStringAsync();
			var updateDetails   = JsonConvert.DeserializeObject<CouchUpdateResponse>(responseContent);
			document.Rev        = updateDetails.Rev;
		}

		private async Task<IEnumerable<TTask>> GetActiveTasks<TTask>(string taskType)
		{
			var url = $"{_url}/_active_tasks";

			var tasks = new List<TTask>();
			using (var response = await _client.GetAsync(url))
			{
				var allTasks = await GetSerializedResultContent<JArray>(response);
				foreach (var task in allTasks)
				{
					var type = task["type"];
					if (type != null && type.Value<string>() == taskType)
						tasks.Add(task.ToObject<TTask>());
				}
			}

			return tasks;
		}
	}
}