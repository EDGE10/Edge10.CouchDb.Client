﻿using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Edge10.CouchDb.Client.Utils
{
	/// <summary>
	/// An interface for facade implementations wrapping <see cref="HttpClient"/>.
	/// </summary>
	internal interface IHttpClientFacade : IDisposable
	{
		void SetAuthorizationHeader(AuthenticationHeaderValue header);

		Uri BaseAddress { get; set; }

		HttpRequestHeaders DefaultRequestHeaders { get; }
		void CancelPendingRequests();
		long MaxResponseContentBufferSize { get; set; }
		Task<HttpResponseMessage> DeleteAsync(string requestUri);
		TimeSpan Timeout { get; set; }
		Task<HttpResponseMessage> DeleteAsync(Uri requestUri);
		Task<HttpResponseMessage> DeleteAsync(string requestUri, CancellationToken cancellationToken);
		Task<HttpResponseMessage> DeleteAsync(Uri requestUri, CancellationToken cancellationToken);
		Task<HttpResponseMessage> GetAsync(string requestUri);
		Task<HttpResponseMessage> GetAsync(Uri requestUri);
		Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken);
		Task<HttpResponseMessage> GetAsync(string requestUri, HttpCompletionOption completionOption);
		Task<HttpResponseMessage> GetAsync(Uri requestUri, CancellationToken cancellationToken);
		Task<HttpResponseMessage> GetAsync(Uri requestUri, HttpCompletionOption completionOption);
		Task<HttpResponseMessage> GetAsync(string requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken);
		Task<HttpResponseMessage> GetAsync(Uri requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken);
		Task<byte[]> GetByteArrayAsync(string requestUri);
		Task<byte[]> GetByteArrayAsync(Uri requestUri);
		Task<Stream> GetStreamAsync(string requestUri);
		Task<Stream> GetStreamAsync(Uri requestUri);
		Task<string> GetStringAsync(string requestUri);
		Task<string> GetStringAsync(Uri requestUri);
		Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content);
		Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent content);
		Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content, CancellationToken cancellationToken);
		Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken);
		Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content);
		Task<HttpResponseMessage> PutAsync(Uri requestUri, HttpContent content);
		Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content, CancellationToken cancellationToken);
		Task<HttpResponseMessage> PutAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken);
		Task<HttpResponseMessage> SendAsync(HttpRequestMessage request);
		Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
		Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption);
		Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken);
	}
}