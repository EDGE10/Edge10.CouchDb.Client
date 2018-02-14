using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Edge10.CouchDb.Client.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Edge10.CouchDb.Client.Configuration
{
	/// <summary>
	/// An implementation of <see cref="ICouchConfigurationApi" /> that uses a <see cref="HttpClient" />
	/// to communicate with Couch.
	/// </summary>
	public sealed class CouchConfigurationApi : ICouchConfigurationApi
	{
		private IHttpClientFacade _client;
		private readonly string _url;

		public CouchConfigurationApi(string host, int port, HttpClientHandler httpClientHandler = null)
			: this(host, port, new HttpClientFacade(httpClientHandler ?? new HttpClientHandler()))
		{ }

		internal CouchConfigurationApi(string host, int port, IHttpClientFacade httpClient)
		{
			host.ThrowIfNullOrEmpty(nameof(host));

			_client = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			_url = GetServerUrl(host, port);
		}

		public void Authorize(string username, string password)
		{
			_client.SetAuthorizationHeader(new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"))));
		}

		public async Task<bool> UserExists(string username)
		{
			username = WebUtility.UrlEncode(username);

			var response = await _client.GetAsync($"{_url}/_users/org.couchdb.user:{username}");

			if (response.StatusCode == HttpStatusCode.NotFound)
				return false;

			await response.ThrowErrorIfNotSuccess();
			return true;
		}

		public async Task<bool> DatabaseExists(string databaseName)
		{
			var response = await _client.GetAsync($"{_url}/{databaseName}");

			if (response.StatusCode == HttpStatusCode.NotFound)
				return false;

			await response.ThrowErrorIfNotSuccess();
			return true;
		}

		public async Task CreateAdminUser(string username, string password)
		{
			username = WebUtility.UrlEncode(username);

			using (var content = new StringContent($"\"{password}\""))
			using (var response = await _client.PutAsync($"{_url}/_config/admins/{username}", content))
				await response.ThrowErrorIfNotSuccess();

			var user = new
			{
				_id   = $"org.couchdb.user:{username}",
				type  = "user",
				name  = username,
				roles = new object[0]
			};
			using (var content = new ObjectContent<dynamic>(user, new JsonMediaTypeFormatter()))
			using (var response = await _client.PutAsync($"{_url}/_users/org.couchdb.user:{username}", content))
				await response.ThrowErrorIfNotSuccess();
		}

		public async Task CreateDatabase(string databaseName)
		{
			using (var response = await _client.PutAsync($"{_url}/{databaseName}", null))
				await response.ThrowErrorIfNotSuccess();
		}

		public async Task CreateDatabaseAdminUser(string username, string databaseName)
		{
			var security = new
			{
				admins = new
				{
					names = new[] { username },
					roles = new object[0]
				},
				readers = new
				{
					names = new [] { username },
					roles = new object[0]
				}
			};
			using (var content = new ObjectContent<dynamic>(security, new JsonMediaTypeFormatter()))
			using (var response = await _client.PutAsync($"{_url}/{databaseName}/_security", content))
				await response.ThrowErrorIfNotSuccess();
		}

		public async Task TriggerReplication(string source, string target, bool continuous, string sourceAuthorizationHeader = null, string targetAuthorizationHeader = null)
		{
			var replication = new
			{
				source = GetDbDetails(source, sourceAuthorizationHeader),
				target = GetDbDetails(target, targetAuthorizationHeader),
				continuous
			};
			using (var content = new ObjectContent<dynamic>(replication, new JsonMediaTypeFormatter()))
			using (var response = await _client.PostAsync($"{_url}/_replicate", content))
				await response.ThrowErrorIfNotSuccess();
		}

		public async Task SetConfigKey(string section, string key, string value)
		{
			using (var content = new StringContent($"\"{value}\""))
			using (var response = await _client.PutAsync($"{_url}/_config/{section}/{key}", content))
				await response.ThrowErrorIfNotSuccess();
		}

		public async Task<string> GetVersion()
		{
			var response = await _client.GetAsync(_url);
			await response.ThrowErrorIfNotSuccess();

			var content = await response.Content.ReadAsStreamAsync();
			using (var sr = new StreamReader(content))
			using (var jsonTextReader = new JsonTextReader(sr))
			{
				var details = JToken.ReadFrom(jsonTextReader);
				return details.Value<string>("version");
			}
		}

		private static dynamic GetDbDetails(string url, string header)
		{
			if (header == null)
				return url;

			return new
			{
				url,
				headers = new
				{
					Authorization = header
				}
			};
		}

		public void Dispose()
		{
			_client?.Dispose();

			_client = null;
		}

		private static string GetServerUrl(string host, int port)
		{
			return $"{((host.StartsWith("http://") || host.StartsWith("https://")) ? string.Empty : "http://")}{host}:{port}";
		}
	}
}
