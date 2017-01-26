using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Edge10.CouchDb.Client.Changes;
using Edge10.CouchDb.Client.Exceptions;
using Edge10.CouchDb.Client.Results;
using Edge10.CouchDb.Client.Utils;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;

namespace Edge10.CouchDb.Client.Tests
{
	[TestFixture]
	public class TestCouchApi
	{
		private CouchApi _couchApi;
		private ICouchDbConnectionStringBuilder _connectionString;
		private Mock<IHttpClientFacade> _httpClient;

		[SetUp]
		public void Init()
		{
			_connectionString = CreateConnectionString();
			_httpClient       = new Mock<IHttpClientFacade>();

			//check that the constructor sets the auth header
			_httpClient.Setup(hc => hc.SetAuthorizationHeader(It.IsAny<AuthenticationHeaderValue>()))
				.Callback<AuthenticationHeaderValue>(CheckHeader);

			_couchApi =  new CouchApi(_connectionString, _httpClient.Object);
		}

		[Test]
		public void Constructor_Throws_Exception_On_Null_Parameters()
		{
			Assert.Throws<ArgumentNullException>(() => new CouchApi(null, _httpClient.Object));
			Assert.Throws<ArgumentNullException>(() => new CouchApi(_connectionString, null));
			Assert.Throws<ArgumentNullException>(() => new CouchApi(null));
		}

		[Test]
		public async Task Constructor_Extracts_URLs_Correctly()
		{
			await TestUrlGenerationFromServerString("http://server", "http://server:1234/database/document/attachment");
			await TestUrlGenerationFromServerString("server", "http://server:1234/database/document/attachment");
			await TestUrlGenerationFromServerString("https://server", "https://server:1234/database/document/attachment");
		}

		[Test]
		public void Dispose_Cleans_Up_HttpClient()
		{
			_couchApi.Dispose();

			_httpClient.Verify(hc => hc.Dispose(), Times.Once(), "Should have disposed HTTP client");

			_couchApi.Dispose();

			_httpClient.Verify(hc => hc.Dispose(), Times.Once(), "Should only have disposed HTTP client once");
		}

		[Test]
		public void AttachmentExistsAsync_Throws_On_Invalid_Parameters()
		{
			Assert.ThrowsAsync<ArgumentNullException>(() => _couchApi.AttachmentExistsAsync(null, "attachment"));
			Assert.ThrowsAsync<ArgumentNullException>(() => _couchApi.AttachmentExistsAsync("document", null));

			Assert.ThrowsAsync<ArgumentException>(() => _couchApi.AttachmentExistsAsync(string.Empty, "attachment"));
			Assert.ThrowsAsync<ArgumentException>(() => _couchApi.AttachmentExistsAsync("document", string.Empty));
		}

		[Test]
		public async Task AttachmentExistsAsync_Returns_True_For_Successful_Http_Code()
		{
			//setup call to the HTTP client
			_httpClient.Setup(hc => hc.GetAsync(
					"https://server:1234/database/document/attachment",
					HttpCompletionOption.ResponseHeadersRead))
				.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

			var exists = await _couchApi.AttachmentExistsAsync("document", "attachment");
			Assert.IsTrue(exists, "Should exist for a successful status code");
		}

		[Test]
		public async Task AttachmentExistsAsync_Returns_False_For_Failure_Code()
		{
			var failureCodes = new[] {
				HttpStatusCode.NotFound,
				HttpStatusCode.Forbidden,
				HttpStatusCode.InternalServerError,
				HttpStatusCode.ServiceUnavailable
			};

			foreach (var code in failureCodes)
			{
				//setup call to the HTTP client
				_httpClient.Setup(hc => hc.GetAsync(
						"https://server:1234/database/document/attachment",
						HttpCompletionOption.ResponseHeadersRead))
					.ReturnsAsync(new HttpResponseMessage(code));

				var exists = await _couchApi.AttachmentExistsAsync("document", "attachment");
				Assert.IsFalse(exists, "Should no exist for a failure status code");
			}
		}

		[Test]
		public void GetAttachmentStreamAsync_Throws_On_Failure_Code()
		{
			var failureCodes = new[] {
				HttpStatusCode.NotFound,
				HttpStatusCode.Forbidden,
				HttpStatusCode.InternalServerError,
				HttpStatusCode.ServiceUnavailable
			};

			foreach (var code in failureCodes)
			{
				//setup call to the HTTP client
				_httpClient.Setup(hc => hc.GetAsync("https://server:1234/database/document/attachment"))
					.ReturnsAsync(new HttpResponseMessage(code));

				Assert.ThrowsAsync<HttpRequestException>(() => _couchApi.GetAttachmentStreamAsync("document", "attachment"));
			}
		}

		[Test]
		public void GetAttachmentStreamAsync_Throws_On_Invalid_Parameters()
		{
			Assert.ThrowsAsync<ArgumentNullException>(() => _couchApi.GetAttachmentStreamAsync(null, "attachment"));
			Assert.ThrowsAsync<ArgumentNullException>(() => _couchApi.GetAttachmentStreamAsync("document", null));

			Assert.ThrowsAsync<ArgumentException>(() => _couchApi.GetAttachmentStreamAsync(string.Empty, "attachment"));
			Assert.ThrowsAsync<ArgumentException>(() => _couchApi.GetAttachmentStreamAsync("document", string.Empty));
		}

		[Test]
		public async Task GetAttachmentStreamAsync_Returns_Stream_For_Successful_Http_Code()
		{
			var response     = new HttpResponseMessage(HttpStatusCode.OK);
			var stream       = new MemoryStream(new byte[] { 1, 2, 3 });
			response.Content = new StreamContent(stream);

			//setup call to the HTTP client
			_httpClient.Setup(hc => hc.GetAsync("https://server:1234/database/document/attachment"))
				.ReturnsAsync(response);

			var data      = await _couchApi.GetAttachmentStreamAsync("document", "attachment");
			var dataBytes = new byte[3];
			data.Read(dataBytes, 0, 3);
			Assert.AreEqual(new byte[] { 1, 2, 3 }, dataBytes, "Returned stream should be from response");
		}

		[Test]
		public async Task TryGetAttachmentStreamAsync_Returns_Null_On_Failure_Code()
		{
			var failureCodes = new[] {
				HttpStatusCode.NotFound,
				HttpStatusCode.Forbidden,
				HttpStatusCode.InternalServerError,
				HttpStatusCode.ServiceUnavailable
			};

			foreach (var code in failureCodes)
			{
				//setup call to the HTTP client
				_httpClient.Setup(hc => hc.GetAsync("https://server:1234/database/document/attachment"))
					.ReturnsAsync(new HttpResponseMessage(code));

				var result = await _couchApi.TryGetAttachmentStreamAsync("document", "attachment");
				Assert.IsNull(result, "Should have returned null");
			}
		}

		[Test]
		public void TryGetAttachmentStreamAsync_Throws_On_Invalid_Parameters()
		{
			Assert.ThrowsAsync<ArgumentNullException>(() => _couchApi.TryGetAttachmentStreamAsync(null, "attachment"));
			Assert.ThrowsAsync<ArgumentNullException>(() => _couchApi.TryGetAttachmentStreamAsync("document", null));

			Assert.ThrowsAsync<ArgumentException>(() => _couchApi.TryGetAttachmentStreamAsync(string.Empty, "attachment"));
			Assert.ThrowsAsync<ArgumentException>(() => _couchApi.TryGetAttachmentStreamAsync("document", string.Empty));
		}

		[Test]
		public async Task TryGetAttachmentStreamAsync_Returns_Stream_For_Successful_Http_Code()
		{
			var response = new HttpResponseMessage(HttpStatusCode.OK);
			var stream = new MemoryStream(new byte[] { 1, 2, 3 });
			response.Content = new StreamContent(stream);

			//setup call to the HTTP client
			_httpClient.Setup(hc => hc.GetAsync("https://server:1234/database/document/attachment"))
				.ReturnsAsync(response);

			var data = await _couchApi.TryGetAttachmentStreamAsync("document", "attachment");
			var dataBytes = new byte[3];
			data.Read(dataBytes, 0, 3);
			Assert.AreEqual(new byte[] { 1, 2, 3 }, dataBytes, "Returned stream should be from response");
		}

		[Test]
		public void GetAttachmentAsync_Throws_On_Failure_Code()
		{
			var failureCodes = new[] {
				HttpStatusCode.NotFound,
				HttpStatusCode.Forbidden,
				HttpStatusCode.InternalServerError,
				HttpStatusCode.ServiceUnavailable
			};

			foreach (var code in failureCodes)
			{
				//setup call to the HTTP client
				_httpClient.Setup(hc => hc.GetAsync("https://server:1234/database/document/attachment"))
					.ReturnsAsync(new HttpResponseMessage(code));

				Assert.ThrowsAsync<HttpRequestException>(() => _couchApi.GetAttachmentAsync("document", "attachment"));
			}
		}

		[Test]
		public void GetAttachmentAsync_Throws_On_Invalid_Parameters()
		{
			Assert.ThrowsAsync<ArgumentNullException>(() => _couchApi.GetAttachmentAsync(null, "attachment"));
			Assert.ThrowsAsync<ArgumentNullException>(() => _couchApi.GetAttachmentAsync("document", null));

			Assert.ThrowsAsync<ArgumentException>(() => _couchApi.GetAttachmentAsync(string.Empty, "attachment"));
			Assert.ThrowsAsync<ArgumentException>(() => _couchApi.GetAttachmentAsync("document", string.Empty));
		}

		[Test]
		public async Task GetAttachmentAsync_Returns__For_Successful_Http_Code()
		{
			var response     = new HttpResponseMessage(HttpStatusCode.OK);
			var content      = new StringContent("");
			response.Content = content;

			//setup call to the HTTP client
			_httpClient.Setup(hc => hc.GetAsync("https://server:1234/database/document/attachment"))
				.ReturnsAsync(response);

			var data = await _couchApi.GetAttachmentAsync("document", "attachment");
			Assert.AreEqual(content, data, "Should return content object from request");
		}

		[Test]
		public async Task TryGetAttachmentAsync_Returns_Null_On_Failure_Code()
		{
			var failureCodes = new[] {
				HttpStatusCode.NotFound,
				HttpStatusCode.Forbidden,
				HttpStatusCode.InternalServerError,
				HttpStatusCode.ServiceUnavailable
			};

			foreach (var code in failureCodes)
			{
				//setup call to the HTTP client
				_httpClient.Setup(hc => hc.GetAsync("https://server:1234/database/document/attachment"))
					.ReturnsAsync(new HttpResponseMessage(code));

				var result = await _couchApi.TryGetAttachmentAsync("document", "attachment");
				Assert.IsNull(result, "Should have returned null");
			}
		}

		[Test]
		public void TryGetAttachmentAsync_Throws_On_Invalid_Parameters()
		{
			Assert.ThrowsAsync<ArgumentNullException>(() => _couchApi.TryGetAttachmentAsync(null, "attachment"));
			Assert.ThrowsAsync<ArgumentNullException>(() => _couchApi.TryGetAttachmentAsync("document", null));

			Assert.ThrowsAsync<ArgumentException>(() => _couchApi.TryGetAttachmentAsync(string.Empty, "attachment"));
			Assert.ThrowsAsync<ArgumentException>(() => _couchApi.TryGetAttachmentAsync("document", string.Empty));
		}

		[Test]
		public async Task TryGetAttachmentAsync_Returns__For_Successful_Http_Code()
		{
			var response     = new HttpResponseMessage(HttpStatusCode.OK);
			var content      = new StringContent("");
			response.Content = content;

			//setup call to the HTTP client
			_httpClient.Setup(hc => hc.GetAsync("https://server:1234/database/document/attachment"))
				.ReturnsAsync(response);

			var data = await _couchApi.TryGetAttachmentAsync("document", "attachment");
			Assert.AreEqual(content, data, "Should return content object from request");
		}

		[Test]
		public async Task CheckConnection_Invokes_Db_Url()
		{
			_httpClient.Setup(hc => hc.GetAsync("https://server:1234/database"))
				.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

			await _couchApi.CheckConnection();
			//no exceptions so pass

			//now set up an invalid response
			_httpClient.Setup(hc => hc.GetAsync("https://server:1234/database"))
				.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

			Assert.ThrowsAsync<HttpRequestException>(() => _couchApi.CheckConnection());
		}

		[Test]
		public void GetViewRowsAsync_Throws_On_Null_ViewParameters()
		{
			Assert.ThrowsAsync<ArgumentNullException>(() => _couchApi.GetViewRowsAsync<string>(null));
		}

		[Test]
		public void GetViewRowsAsync_Throws_On_Unsuccessful_GET()
		{
			IViewParameters viewParameters;
			string expectedUrl;
			SetupViewParameters(out viewParameters, out expectedUrl, false);  //should not include docs even though specified

			//setup an error on the HTTP call
			_httpClient.Setup(c => c.GetStreamAsync(expectedUrl))
				.ThrowsAsync(new HttpRequestException());

			//make the call and check for the exception
			Assert.ThrowsAsync<HttpRequestException>(() => _couchApi.GetViewRowsAsync<string>(viewParameters));
		}

		[Test]
		public void GetViewRowsAsync_Throws_Timeout_Exception_On_Timeout_Error()
		{
			TestCouchTimeoutExceptions(viewParameters => _couchApi.GetViewRowsAsync<object>(viewParameters), false);
		}

		[Test]
		public async Task GetViewRowsAsync_Returns_Deserialized_Results()
		{
			//setup the view parameters
			IViewParameters viewParameters;
			string expectedUrl;
			SetupViewParameters(out viewParameters, out expectedUrl, false);  //should not include docs even though specified
			viewParameters.IncludeDocs = true;

			var data = new[] {
				"one", "two", "three", "with _id", "with _rev"
			};

			//create a stream
			var stream = CreateStreamWithContent(CreateViewResultWithRows(data));

			//setup a successful HTTP call
			_httpClient.Setup(c => c.GetStreamAsync(expectedUrl))
				.ReturnsAsync(stream);

			//make the call and check the result
			var result = await _couchApi.GetViewRowsAsync<string>(viewParameters);
			Assert.AreEqual(
				new[] { "one", "two", "three", "with _id", "with _rev" },
				result,
				"Data should have been serialized");
		}

		[Test]
		public void GetViewDocumentsAsync_Throws_On_Null_ViewParameters()
		{
			Assert.ThrowsAsync<ArgumentNullException>(() => _couchApi.GetViewDocumentsAsync<string>(null));
		}

		[Test]
		public void GetViewDocumentsAsync_Throws_On_Unsuccessful_GET()
		{
			IViewParameters viewParameters;
			string expectedUrl;
			SetupViewParameters(out viewParameters, out expectedUrl);

			//setup an error on the HTTP call
			_httpClient.Setup(c => c.GetStreamAsync(expectedUrl))
				.ThrowsAsync(new HttpRequestException());

			//make the call and check for the exception
			Assert.ThrowsAsync<HttpRequestException>(() => _couchApi.GetViewDocumentsAsync<string>(viewParameters));
		}

		[Test]
		public async Task GetViewDocumentsAsync_Returns_Deserialized_Results()
		{
			//setup the view parameters
			IViewParameters viewParameters;
			string expectedUrl;
			SetupViewParameters(out viewParameters, out expectedUrl);

			var data = new[] {
				"one", "two", "three", "with _id", "with _rev"
			};

			//create a stream
			var stream = CreateStreamWithContent(CreateViewResultWithDocuments(data));

			//setup a successful HTTP call
			_httpClient.Setup(c => c.GetStreamAsync(expectedUrl))
				.ReturnsAsync(stream);

			//make the call and check the result
			var result = await _couchApi.GetViewDocumentsAsync<string>(viewParameters);
			Assert.AreEqual(
				new[] { "one", "two", "three", "with _id", "with _rev" },
				result.ToList(),
				"Data should have been serialized");
		}

		[Test]
		public void GetPagedViewDocumentsAsync_Throws_On_Unsuccessful_GET()
		{
			IViewParameters viewParameters;
			string expectedUrl;
			SetupViewParameters(out viewParameters, out expectedUrl, expectReduce: false);

			//setup an error on the HTTP call
			_httpClient.Setup(c => c.GetStreamAsync(expectedUrl))
				.ThrowsAsync(new HttpRequestException());

			Assert.ThrowsAsync<HttpRequestException>(() => _couchApi.GetPagedViewDocumentsAsync<string>(viewParameters));
		}

		[Test]
		public async Task GetPagedViewDocumentsAsync_Returns_Deserialized_Results()
		{
			IViewParameters viewParameters;
			string expectedUrl;
			SetupViewParameters(out viewParameters, out expectedUrl, expectReduce: false);

			var data = new[] {
				"one", "two", "three", "with _id", "with _rev"
			};

			var stream = CreateStreamWithContent(CreateViewResultWithDocuments(data));
			var countStream = CreateStreamWithContent(CreateViewResultWithRows(10));

			_httpClient.Setup(c => c.GetStreamAsync(expectedUrl))
				.ReturnsAsync(stream);
			const string countUrl = "https://server:1234/database/_design/design/_view/view?include_docs=false&reduce=true";
			_httpClient.Setup(c => c.GetStreamAsync(countUrl))
				.ReturnsAsync(countStream);

			var result = await _couchApi.GetPagedViewDocumentsAsync<string>(viewParameters);
			Assert.AreEqual(
				new[] { "one", "two", "three", "with _id", "with _rev" },
				result.Rows);
			Assert.AreEqual(10, result.TotalRows);
		}

		[Test]
		public async Task GetPagedViewDocumentsAsync_Returns_Count_Of_0_When_No_Rows()
		{
			IViewParameters viewParameters;
			string expectedUrl;
			SetupViewParameters(out viewParameters, out expectedUrl, expectReduce: false);

			var stream      = CreateStreamWithContent(CreateViewResultWithDocuments(new string[0]));
			var countStream = CreateStreamWithContent(CreateViewResultWithRows<int>());

			_httpClient.Setup(c => c.GetStreamAsync(expectedUrl))
				.ReturnsAsync(stream);
			const string countUrl = "https://server:1234/database/_design/design/_view/view?include_docs=false&reduce=true";
			_httpClient.Setup(c => c.GetStreamAsync(countUrl))
				.ReturnsAsync(countStream);

			var result = await _couchApi.GetPagedViewDocumentsAsync<string>(viewParameters);
			Assert.AreEqual(0, result.TotalRows);
		}

		[Test]
		public void GetViewDocumentIdsAsync_Throws_On_Null_ViewParameters()
		{
			Assert.ThrowsAsync<ArgumentNullException>(() => _couchApi.GetViewDocumentIdsAsync(null));
		}

		[Test]
		public void GetViewDocumentIdsAsync_Throws_On_Unsuccessful_GET()
		{
			IViewParameters viewParameters;
			string expectedUrl;
			SetupViewParameters(out viewParameters, out expectedUrl);

			//setup an error on the HTTP call
			_httpClient.Setup(c => c.GetStreamAsync(expectedUrl))
				.ThrowsAsync(new HttpRequestException());

			//make the call and check for the exception
			Assert.ThrowsAsync<HttpRequestException>(() => _couchApi.GetViewDocumentIdsAsync(viewParameters));
		}

		[Test]
		public void GetViewDocumentsAsync_Throws_Timeout_Exception_On_Timeout_Error()
		{
			TestCouchTimeoutExceptions(viewParameters => _couchApi.GetViewDocumentsAsync<object>(viewParameters));
		}

		[Test]
		public async Task GetViewDocumentIdsAsync_Returns_Deserialized_Results()
		{
			//setup the view parameters
			IViewParameters viewParameters;
			string expectedUrl;
			SetupViewParameters(out viewParameters, out expectedUrl);

			var data = new[] {
				Guid.NewGuid(),
				Guid.NewGuid(),
				Guid.NewGuid(),
			};
			var couchResult = new ViewResult<string, object> { TotalRows = data.Length };

			((List<ViewResultRow<string, object>>)couchResult.Rows).AddRange(data.Select(r =>
				new ViewResultRow<string, object>
				{
					Document = r.ToString(),
					Id = r
				}));

			//create a stream
			var stream = CreateStreamWithContent(couchResult);

			//setup a successful HTTP call
			_httpClient.Setup(c => c.GetStreamAsync(expectedUrl))
				.ReturnsAsync(stream);

			//make the call and check the result
			var result = await _couchApi.GetViewDocumentIdsAsync(viewParameters);
			Assert.AreEqual(
				data,
				result.ToList(),
				"Document IDs should have been returned");
		}

		[Test]
		public async Task GetViewRowsAsync_Posts_When_Multiple_Query_Keys_Specified()
		{
			//setup the view parameters
			IViewParameters viewParameters;
			string expectedUrl;
			SetupViewParameters(out viewParameters, out expectedUrl, false);  //should not include docs even though specified
			viewParameters.Keys = new object[] {
				"key one",
				2,
				"key three"
			};

			var data = new[] {
				"one", "two", "three", "with _id", "with _rev"
			};

			//create a response
			var response = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(JsonConvert.SerializeObject(CreateViewResultWithRows(data)))
			};

			//setup a successful HTTP call
			_httpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), HttpCompletionOption.ResponseHeadersRead))
				.ReturnsAsync(response)
				.Callback<HttpRequestMessage, HttpCompletionOption>((request, completionOption) =>
				{
					Assert.AreEqual(HttpMethod.Post, request.Method, "Request should be a post");
					Assert.AreEqual(expectedUrl, request.RequestUri.ToString(), "Request URI should match");

					Assert.AreEqual("application/json", request.Content.Headers.ContentType.MediaType, "application/json is required");
					var stringContent = request.Content.ReadAsStringAsync().Result;
					Assert.AreEqual(@"{""keys"":[""key one"",2,""key three""]}", stringContent, "Keys should have been serialized");
				});

			//make the call and check the result
			var result = await _couchApi.GetViewRowsAsync<string>(viewParameters);
			Assert.AreEqual(
				new[] { "one", "two", "three", "with _id", "with _rev" },
				result,
				"Data should have been serialized");
		}

		[Test]
		public void GetDocumentAsync_Throws_On_Invalid_Id()
		{
			Assert.ThrowsAsync<ArgumentNullException>(() => _couchApi.GetDocumentAsync<string>(null));
			Assert.ThrowsAsync<ArgumentException>(() => _couchApi.GetDocumentAsync<string>(string.Empty));
		}

		[Test]
		public void GetDocumentAsync_Throws_On_Invalid_Revision()
		{
			Assert.ThrowsAsync<ArgumentNullException>(() => _couchApi.GetDocumentAsync<string>("id", null));
			Assert.ThrowsAsync<ArgumentException>(() => _couchApi.GetDocumentAsync<string>("id", string.Empty));
		}

		[Test]
		public void GetDocumentAsync_Throws_On_Unsuccessful_Get()
		{
			var expectedUrl = "https://server:1234/database/document";
			var response    = new HttpResponseMessage(HttpStatusCode.NotFound);

			//setup a failed HTTP call
			_httpClient.Setup(c => c.GetAsync(expectedUrl))
				.ReturnsAsync(response);

			Assert.ThrowsAsync<HttpRequestException>(() => _couchApi.GetDocumentAsync<string>("document"));
		}

		[Test]
		public void GetDocumentAsync_With_Revision_Throws_On_Unsuccessful_Get()
		{
			var expectedUrl = "https://server:1234/database/document?rev=rev1";
			var response    = new HttpResponseMessage(HttpStatusCode.NotFound);

			//setup a failed HTTP call
			_httpClient.Setup(c => c.GetAsync(expectedUrl))
				.ReturnsAsync(response);

			Assert.ThrowsAsync<HttpRequestException>(() => _couchApi.GetDocumentAsync<string>("document", "rev1"));
		}

		[Test]
		public async Task TryGetDocumentAsync_Returns_Null_On_Unsuccessful_Get()
		{
			var expectedUrl = "https://server:1234/database/document";
			var response    = new HttpResponseMessage(HttpStatusCode.NotFound);

			//setup a failed HTTP call
			_httpClient.Setup(c => c.GetAsync(expectedUrl))
				.ReturnsAsync(response);

			var document = await _couchApi.TryGetDocumentAsync<string>("document");
			Assert.IsNull(document);
		}

		[Test]
		public void GetDocumentsAsync_Throws_On_Null_ID_Array()
		{
			Assert.ThrowsAsync<ArgumentNullException>(() => _couchApi.GetDocumentsAsync<string>(null));
		}

		[Test]
		public async Task GetDocumentsAsync_Ignores_Empty_Array()
		{
			var result = await _couchApi.GetDocumentsAsync<string>(Enumerable.Empty<string>());
			Assert.AreEqual(0, result.Count(), "No results should be returned");
			_httpClient.Verify(c => c.PostAsync(
				It.IsAny<string>(),
				It.IsAny<HttpContent>()), Times.Never(), "No calls should have been made to the server");
		}

		[Test]
		public async Task GetDocumentsAsync_Returns_Documents_From_Server()
		{
			var ids         = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
			var expectedUrl = "https://server:1234/database/_all_docs?include_docs=true";
			var response    = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(JsonConvert.SerializeObject(new
				{
					rows = new[]
					{
						new
						{
							doc = new
							{
								_id = ids[0],
								_rev = "rev1",
							}
						},
						new
						{
							doc = new
							{
								_id = ids[1],
								_rev = "rev2",
							}
						},
					}
				}))
			};

			//setup a successful HTTP call
			string actualContent = null;
			_httpClient.Setup(c => c.PostAsync(expectedUrl,
				It.IsAny<HttpContent>()))
				.ReturnsAsync(response)
				.Callback<string, HttpContent>((_, content) => actualContent = content.ReadAsStringAsync().Result);

			var documents = await _couchApi.GetDocumentsAsync<DummyCouchModel>(ids.Select(i => i.ToString()));

			//check the content passed to the client
			Assert.IsNotNull(actualContent, "Content should have been included in the request");
			Assert.AreEqual(
				JsonConvert.SerializeObject(new { keys = ids }),
				actualContent,
				"The serialized IDs should have been included in the request");

			Assert.AreEqual(2, documents.Count(), "2 documents should be returned");

			Assert.AreEqual(ids[0], documents.ElementAt(0).Id);
			Assert.AreEqual("rev1", documents.ElementAt(0).Rev);
			Assert.AreEqual(ids[1], documents.ElementAt(1).Id);
			Assert.AreEqual("rev2", documents.ElementAt(1).Rev);
			Assert.ThrowsAsync<ObjectDisposedException>(() => response.Content.ReadAsStringAsync());
		}

		[Test]
		public async Task GetDocumentsAsync_Splits_Large_Requests_Into_Packets()
		{
			var ids             = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
			var expectedUrl     = "https://server:1234/database/_all_docs?include_docs=true";
			var packet1Response = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(JsonConvert.SerializeObject(new
				{
					rows = new[]
					{
						new
						{
							doc = new
							{
								_id = ids[0],
								_rev = "rev1",
							}
						},
						new
						{
							doc = new
							{
								_id = ids[1],
								_rev = "rev2",
							}
						},
					}
				}))
			};

			var packet2Response = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(JsonConvert.SerializeObject(new
				{
					rows = new[]
					{
						new
						{
							doc = new
							{
								_id = ids[2],
								_rev = "rev3",
							}
						}
					}
				}))
			};

			var responses = new Queue<HttpResponseMessage>(new[] { packet1Response, packet2Response });
			var requestContents = new List<string>();
			_httpClient.Setup(c => c.PostAsync(expectedUrl, It.IsAny<HttpContent>()))
				.Returns(() => Task.FromResult(responses.Dequeue()))
				.Callback<string, HttpContent>((s, content) => requestContents.Add(content.ReadAsStringAsync().Result));

			_couchApi.MaxDocumentsPerRequest = 2;

			var documents = await _couchApi.GetDocumentsAsync<DummyCouchModel>(ids.Select(id => id.ToString()));

			Assert.AreEqual(2, requestContents.Count, "2 separate requests should have been made");
			Assert.AreEqual(
				JsonConvert.SerializeObject(new { keys = new[] { ids[0], ids[1] } }),
				requestContents[0],
				"The first packet of serialized IDs should have been included in the request");
			Assert.AreEqual(
				JsonConvert.SerializeObject(new { keys = new[] { ids[2] } }),
				requestContents[1],
				"The second packet of serialized IDs should have been included in the request");

			Assert.AreEqual(3, documents.Count(), "3 documents should be returned");

			Assert.AreEqual(ids[0], documents.ElementAt(0).Id);
			Assert.AreEqual("rev1", documents.ElementAt(0).Rev);
			Assert.AreEqual(ids[1], documents.ElementAt(1).Id);
			Assert.AreEqual("rev2", documents.ElementAt(1).Rev);
			Assert.AreEqual(ids[2], documents.ElementAt(2).Id);
			Assert.AreEqual("rev3", documents.ElementAt(2).Rev);
		}

		private void DocumentExistsAsync_Set_HttpClientFacade_Setup(string requestUri, HttpResponseMessage httpResponseMensager)
		{
			_httpClient.Setup(httpcf =>httpcf.GetAsync(requestUri))
				.ReturnsAsync(httpResponseMensager);
		}

		[Test]
		public void DocumentExistsAsync_Throws_Exception_When_Pass_Invalid_Parameters()
		{
			Assert.ThrowsAsync<ArgumentNullException>(() => _couchApi.DocumentExistsAsync(null));
			Assert.ThrowsAsync<ArgumentException>(() => _couchApi.DocumentExistsAsync(string.Empty));
		}

		[Test]
		public async Task DocumentExistsAsync_Return_False_When_Document_Does_Not_Exist()
		{
			var documentID = "non-existant";

			DocumentExistsAsync_Set_HttpClientFacade_Setup(GetDocumentUri(documentID)
				, new HttpResponseMessage(HttpStatusCode.NotFound));

			var result = await _couchApi.DocumentExistsAsync(documentID);
			Assert.IsFalse(result, "Document shouldn't exist");
		}

		[Test]
		public async Task DocumentExistsAsync_Return_False_When_Http_Client_Returns_null()
		{
			var documentID = "non-existant";

			DocumentExistsAsync_Set_HttpClientFacade_Setup(GetDocumentUri(documentID), null);

			var result = await _couchApi.DocumentExistsAsync(documentID);
			Assert.IsFalse(result, "Document shouldn't exist");
		}

		[Test]
		public async Task DocumentExistsAsync_Return_True_When_Http_Client_Returns_Success_Response()
		{
			var documentID = "existant";

			DocumentExistsAsync_Set_HttpClientFacade_Setup(GetDocumentUri(documentID), new HttpResponseMessage(HttpStatusCode.OK));

			var result = await _couchApi.DocumentExistsAsync(documentID);
			Assert.IsTrue(result, "Document should exist");
		}

		private void CreateDocumentAsync_Set_HttpClientFacade_Setup(string documentId, HttpResponseMessage httpResponseMensager)
		{
			_httpClient.Setup(httpcf =>
				  httpcf.PutAsync(GetDocumentUri(documentId),
				  It.Is<HttpContent>(content =>
					content.Headers.ContentType.MediaType == "application/json" &&
					content.Headers.ContentType.CharSet == "utf-8")))
				  .ReturnsAsync(httpResponseMensager);
		}

		[Test]
		public void CreateDocumentAsync_Throws_Exception_When_Pass_Invalid_Parameters()
		{
			Assert.ThrowsAsync<ArgumentNullException>(() => _couchApi.CreateDocumentAsync(null));
			Assert.ThrowsAsync<ArgumentException>(() => _couchApi.CreateDocumentAsync(string.Empty));
		}

		[Test]
		public void CreateDocumentAsync_Throws_Exception_When_Http_Client_Fail_To_Create_Document()
		{
			CreateDocumentAsync_Set_HttpClientFacade_Setup("newDocumentID", new HttpResponseMessage(HttpStatusCode.InternalServerError));

			Assert.ThrowsAsync<HttpRequestException>(() => _couchApi.CreateDocumentAsync("newDocumentID"));
		}

		[Test]
		public async Task CreateDocumentAsync_When_Http_Client_Returns_Success_Response()
		{
			CreateDocumentAsync_Set_HttpClientFacade_Setup("newDocumentID", new HttpResponseMessage(HttpStatusCode.OK));

			await _couchApi.CreateDocumentAsync("newDocumentID");
		}

		[Test]
		public void CreateDocumentAsync_Throws_On_Null_Document()
		{
			Assert.ThrowsAsync<ArgumentNullException>(() => _couchApi.CreateDocumentAsync<DummyCouchModel>(null));
		}

		[Test]
		public void CreateDocumentAsync_Rethrows_Http_Exceptions()
		{
			var model = new DummyCouchModel { Id = Guid.NewGuid().ToString() };

			//setup a PUT call that returns a failure code
			_httpClient.Setup(c => c.PutAsync(GetDocumentUri(model.Id.ToString()), It.IsAny<HttpContent>()))
				.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

			//make the call and check the exception is thrown
			Assert.ThrowsAsync<HttpRequestException>(() =>
				_couchApi.CreateDocumentAsync(model));
		}

		[Test]
		public async Task CreateDocumentAsync_Sends_Correct_Content_To_Server()
		{
			var model = new DummyCouchModel();

			//create a server response
			var couchResponse = new CouchUpdateResponse { Rev = "new rev" };
			var response      = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(JsonConvert.SerializeObject(couchResponse))
			};

			//setup a PUT call that returns a success call and records the content
			HttpContent content = null;
			_httpClient.Setup(c => c.PutAsync(GetDocumentUri(model.Id.ToString()), It.IsAny<HttpContent>()))
				.ReturnsAsync(response)
				.Callback<string, HttpContent>((_, c) => content = c);

			//make the call and check the content passed to the server
			await _couchApi.CreateDocumentAsync(model);

			Assert.AreNotEqual(Guid.Empty, model.Id, "The ID should have been updated");
			Assert.IsNotNull(content, "Content should have been passed to the server");
			var stringContent = await content.ReadAsStringAsync();
			var expected = @"{
""$type"":""Edge10.CouchDb.Client.Tests.TestCouchApi+DummyCouchModel, Edge10.CouchDb.Client.Tests"",
""property"":""value"",
""enumValue"":0,
""_id"":""" + model.Id + @""",
""_attachments"":{},
""_deleted"":false,
""type"":""DummyCouchModel""}";

			Assert.AreEqual(expected.Replace("\r\n", string.Empty).Replace("\n", string.Empty), stringContent, "The document content was not correctly passed to the server");

			//check that the revision and type properties were updated on the original model
			Assert.AreEqual("new rev", model.Rev, "The Revision should have been updated");
			Assert.AreEqual("DummyCouchModel", model.Type, "The Type should have been updated");
		}

		[Test]
		public async Task CreateDocumentAsync_Uses_Id_If_Specified()
		{
			var id    = Guid.NewGuid().ToString();
			var model = new DummyCouchModel { Id = id };

			//create a server response
			var couchResponse = new CouchUpdateResponse { Rev = "new rev" };
			var response      = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(JsonConvert.SerializeObject(couchResponse))
			};

			//setup a PUT call that returns a success call
			_httpClient.Setup(c => c.PutAsync(GetDocumentUri(id.ToString()), It.IsAny<HttpContent>()))
				.ReturnsAsync(response);

			//make the call
			await _couchApi.CreateDocumentAsync(model);

			//ensure the id wasn't changed
			Assert.AreEqual(id, model.Id, "The ID should not have been changed");
		}

		[Test]
		public void UpdateDocumentAsync_Throws_On_Null_Document()
		{
			Assert.ThrowsAsync<ArgumentNullException>(() => _couchApi.UpdateDocumentAsync<DummyCouchModel>(null));
		}

		[Test]
		public void UpdateDocumentAsync_Rethrows_Http_Exceptions()
		{
			var model = new DummyCouchModel { Id = Guid.NewGuid().ToString() };

			//setup a PUT call that returns a failure code
			_httpClient.Setup(c => c.PutAsync(GetDocumentUri(model.Id.ToString()), It.IsAny<HttpContent>()))
				.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

			//make the call and check the exception is thrown
			Assert.ThrowsAsync<HttpRequestException>(() =>
				_couchApi.UpdateDocumentAsync(model));
		}

		[Test]
		public void UpdateDocumentAsync_Throws_ConflictException_For_Conflict_StatusCode()
		{
			var model = new DummyCouchModel { Id = Guid.NewGuid().ToString() };

			//setup a PUT call that returns a failure code
			_httpClient.Setup(c => c.PutAsync(GetDocumentUri(model.Id.ToString()), It.IsAny<HttpContent>()))
				.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Conflict));

			//make the call and check the exception is thrown
			Assert.ThrowsAsync<ConflictException>(() =>
				_couchApi.UpdateDocumentAsync(model));
		}

		[Test]
		public async Task UpdateDocumentAsync_Sends_Correct_Content_To_Server()
		{
			var model = new DummyCouchModel { Id = Guid.NewGuid().ToString(), Rev = "123" };

			//create a server response
			var couchResponse = new CouchUpdateResponse { Rev = "new rev" };
			var response      = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(JsonConvert.SerializeObject(couchResponse))
			};

			//setup a PUT call that returns a success call and records the content
			HttpContent content = null;
			_httpClient.Setup(c => c.PutAsync(GetDocumentUri(model.Id.ToString()), It.IsAny<HttpContent>()))
				.ReturnsAsync(response)
				.Callback<string, HttpContent>((_, c) => content = c);

			//make the call and check the content passed to the server
			await _couchApi.UpdateDocumentAsync(model);
			Assert.IsNotNull(content, "Content should have been passed to the server");
			var stringContent = await content.ReadAsStringAsync();
			var expected = @"{
""$type"":""Edge10.CouchDb.Client.Tests.TestCouchApi+DummyCouchModel, Edge10.CouchDb.Client.Tests"",
""property"":""value"",
""enumValue"":0,
""_id"":""" + model.Id + @""",
""_rev"":""123"",
""_attachments"":{},
""_deleted"":false,
""type"":""DummyCouchModel""}";

			Assert.AreEqual(expected.Replace("\r\n", string.Empty).Replace("\n", string.Empty), stringContent, "The document content was not correctly passed to the server");

			//check that the revision was updated on the original
			Assert.AreEqual("new rev", model.Rev, "The Revision should have been updated");
		}

		[Test]
		public void BulkUpdateAsync_Throws_On_Null_Argument()
		{
			Assert.ThrowsAsync<ArgumentNullException>(() => _couchApi.BulkUpdateAsync<DummyCouchModel>(null));
		}

		[Test]
		public void BulkUpdateAsync_Throws_On_Not_Successful_Response()
		{
			var doc = new DummyCouchModel { Id = Guid.NewGuid().ToString() };

			//setup a POST call that returns a failure code
			_httpClient.Setup(c => c.PostAsync(GetBulkUri(), It.IsAny<HttpContent>()))
				.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

			//make the call and check the exception is thrown
			Assert.ThrowsAsync<HttpRequestException>(() => _couchApi.BulkUpdateAsync(new[] { doc }));
		}

		[Test]
		public void BulkUpdateAsync_Throws_On_Conflict_For_Some_Document()
		{
			var docs = new[] { new DummyCouchModel { Id = Guid.NewGuid().ToString() }, new DummyCouchModel { Id = Guid.NewGuid().ToString() } };
			var couchResponse = new[]
			{
				new CouchBulkUpdateResponseItem { Id = docs[0].Id, Ok = true, Rev = "222" },
				new CouchBulkUpdateResponseItem { Id = docs[1].Id, Ok = false, Error = "conflict" }
			};
			var response = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(JsonConvert.SerializeObject(couchResponse, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }))
			};

			_httpClient.Setup(c => c.PostAsync(GetBulkUri(), It.IsAny<HttpContent>()))
				.ReturnsAsync(response);

			var ex = Assert.ThrowsAsync<ConflictException>(() => _couchApi.BulkUpdateAsync(docs));
			Assert.IsTrue(ex.Message.Contains(docs[1].Id));
		}

		[Test]
		public void BulkUpdateAsync_Throws_On_Some_Other_Error_In_Result()
		{
			var docs = new[] { new DummyCouchModel { Id = Guid.NewGuid().ToString() }, new DummyCouchModel { Id = Guid.NewGuid().ToString() } };
			var couchResponse = new[]
			{
				new CouchBulkUpdateResponseItem { Id = docs[0].Id, Ok = false, Error = "unknown error", Reason = "Don't know" },
				new CouchBulkUpdateResponseItem { Id = docs[1].Id, Ok = true }
			};
			var response = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(JsonConvert.SerializeObject(couchResponse))
			};

			_httpClient.Setup(c => c.PostAsync(GetBulkUri(), It.IsAny<HttpContent>()))
				.ReturnsAsync(response);

			var ex = Assert.ThrowsAsync<HttpRequestException>(() => _couchApi.BulkUpdateAsync(docs));
			Assert.IsTrue(ex.Message.Contains(docs[0].Id));
		}

		[Test]
		public async Task BulkUpdateAsync_Doesnt_Throw_On_Error_In_Result_If_No_Error_Set()
		{
			var docs = new[] { new DummyCouchModel { Id = Guid.NewGuid().ToString() }, new DummyCouchModel { Id = Guid.NewGuid().ToString() } };
			var couchResponse = new[]
			{
				new CouchBulkUpdateResponseItem { Id = docs[0].Id, Ok = false, Error = null, Reason = "Don't know" },
				new CouchBulkUpdateResponseItem { Id = docs[1].Id, Ok = true }
			};
			var response = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(JsonConvert.SerializeObject(couchResponse))
			};

			_httpClient.Setup(c => c.PostAsync(GetBulkUri(), It.IsAny<HttpContent>()))
				.ReturnsAsync(response);

			// no errors
			await _couchApi.BulkUpdateAsync(docs);
		}

		[Test]
		public async Task BulkUpdateAsync_Sends_Correct_Content_And_Updates_Rev()
		{
			var docs          = new[] { new DummyCouchModel { Id = Guid.NewGuid().ToString() }, new DummyCouchModel { Id = Guid.NewGuid().ToString() } };
			var couchResponse = new[]
			{
				new CouchBulkUpdateResponseItem { Id = docs[0].Id, Ok = true, Rev = "111" },
				new CouchBulkUpdateResponseItem { Id = docs[1].Id, Ok = true, Rev = "222" }
			};
			var response = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(JsonConvert.SerializeObject(couchResponse))
			};

			HttpContent content = null;
			_httpClient.Setup(c => c.PostAsync(GetBulkUri(), It.IsAny<HttpContent>()))
				.ReturnsAsync(response)
				.Callback<string, HttpContent>((_, c) => content = c);

			await _couchApi.BulkUpdateAsync(docs);

			var stringContent = await content.ReadAsStringAsync();
			var expected = @"{
""$type"":""<>f__AnonymousType0`1[[Edge10.CouchDb.Client.Tests.TestCouchApi+DummyCouchModel[], Edge10.CouchDb.Client.Tests]], Edge10.CouchDb.Client"",
""docs"":[
{
""$type"":""Edge10.CouchDb.Client.Tests.TestCouchApi+DummyCouchModel, Edge10.CouchDb.Client.Tests"",
""property"":""value"",
""enumValue"":0,
""_id"":""" + docs[0].Id + @""",
""_attachments"":{},
""_deleted"":false,
""type"":""DummyCouchModel""
},
{
""$type"":""Edge10.CouchDb.Client.Tests.TestCouchApi+DummyCouchModel, Edge10.CouchDb.Client.Tests"",
""property"":""value"",
""enumValue"":0,
""_id"":""" + docs[1].Id + @""",
""_attachments"":{},
""_deleted"":false,
""type"":""DummyCouchModel""
}
]
}";

			Assert.AreEqual(expected.Replace("\r\n", string.Empty).Replace("\n", string.Empty), stringContent, "The document content was not correctly passed to the server");
			Assert.AreEqual("111", docs[0].Rev);
			Assert.AreEqual("222", docs[1].Rev);
		}

		[Test]
		public void PutAsync_Throws_Exception_When_Pass_Invalid_Parameters()
		{
			HttpContent httpContent = new StringContent("");

			Assert.ThrowsAsync<ArgumentNullException>(() => _couchApi.PutAsync(null, null, null));

			Assert.ThrowsAsync<ArgumentException>(() => _couchApi.PutAsync(string.Empty, string.Empty, httpContent));
			Assert.ThrowsAsync<ArgumentException>(() => _couchApi.PutAsync("String", string.Empty, httpContent));
			Assert.ThrowsAsync<ArgumentNullException>(() => _couchApi.PutAsync("String", "String", null));
		}

		[Test]
		public void PutAsync_Throws_Exception_When_Call_SendAsync_And_Http_Client_Fail_To_Get_Document_Header()
		{
			string documentId = "documentId";
			string attachment = "attachment";
			HttpContent httpContent = new StringContent("{}");

			SetSendHeadAsyncOnHttpClientFacade(GetDocumentUri(documentId), new HttpResponseMessage(HttpStatusCode.NotFound));

			Assert.ThrowsAsync<HttpRequestException>(() => _couchApi.PutAsync(documentId, attachment, httpContent));
		}

		private void SetupSuccessfullSendAsync(string documentId, string revision, string attachment, out string requestUri)
		{
			var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
			httpResponseMessage.Headers.ETag = new EntityTagHeaderValue(QuoteString(revision));

			SetSendHeadAsyncOnHttpClientFacade(GetDocumentUri(documentId), httpResponseMessage);

			requestUri = GetAttachmentUri(documentId, attachment, revision);
		}

		[Test]
		public void PutAsync_Throws_Exception_When_Call_PutAsync_And_Http_Client_Fail_To_Add_Attachment()
		{
			var documentId = "documentId";
			var attachment = "attachment";
			var httpContent = new StringContent("{}", Encoding.UTF8, "text/plain");
			string revision = "revision";

			string requestUri;
			SetupSuccessfullSendAsync(documentId, revision, attachment, out requestUri);

			_httpClient.Setup(c => c.PutAsync(requestUri, httpContent))
					   .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

			Assert.ThrowsAsync<HttpRequestException>(() => _couchApi.PutAsync(documentId, attachment, httpContent));
		}

		[Test]
		public async Task PutAsync_Is_Successful_When_Call_PutAsync_And_Http_Client_Returns_Success_Response()
		{
			var documentId  = "documentId";
			var attachment  = "attachment";
			var httpContent = new StringContent("{}", Encoding.UTF8, "text/plain");
			var revision    = "revision";
			string requestUri;

			SetupSuccessfullSendAsync(documentId, revision, attachment, out requestUri);

			_httpClient.Setup(c => c.PutAsync(requestUri, httpContent))
					   .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

			await _couchApi.PutAsync(documentId, attachment, httpContent);
		}

		[Test]
		public void DeleteAttachmentAsync_Throws_Exception_When_Pass_Invalid_Parameters()
		{
			Assert.ThrowsAsync<ArgumentNullException>(() => _couchApi.DeleteAttachmentAsync(null, "valid"));
			Assert.ThrowsAsync<ArgumentNullException>(() => _couchApi.DeleteAttachmentAsync("valid", null));

			Assert.ThrowsAsync<ArgumentException>(() => _couchApi.DeleteAttachmentAsync(string.Empty, "valid"));
			Assert.ThrowsAsync<ArgumentException>(() => _couchApi.DeleteAttachmentAsync("valid", string.Empty));
		}

		[Test]
		public void DeleteAttachmentAsync_Throws_Exception_When_Head_Call_Fails()
		{
			var documentId = "documentId";
			var attachment = "attachment";

			SetSendHeadAsyncOnHttpClientFacade(GetDocumentUri(documentId), new HttpResponseMessage(HttpStatusCode.NotFound));

			Assert.ThrowsAsync<HttpRequestException>(() => _couchApi.DeleteAttachmentAsync(documentId, attachment));
		}

		[Test]
		public void DeleteAttachmentAsync_Throws_Exception_When_Call_DeleteAttachmentAsync_And_Http_Client_Fail_To_Add_Attachment()
		{
			var documentId = "documentId";
			var attachment = "attachment";
			var revision   = "revision";

			string requestUri;
			SetupSuccessfullSendAsync(documentId, revision, attachment, out requestUri);

			_httpClient.Setup(c => c.DeleteAsync(requestUri))
					   .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

			Assert.ThrowsAsync<HttpRequestException>(() => _couchApi.DeleteAttachmentAsync(documentId, attachment));
		}

		[Test]
		public async Task DeleteAttachmentAsync_Is_Successful_When_Call_PutAsync_And_Http_Client_Returns_Success_Response()
		{
			var documentId = "documentId";
			var attachment = "attachment";
			var revision   = "revision";

			string requestUri;
			SetupSuccessfullSendAsync(documentId, revision, attachment, out requestUri);

			_httpClient.Setup(c => c.DeleteAsync(requestUri))
					   .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

			await _couchApi.DeleteAttachmentAsync(documentId, attachment);
		}

		[Test]
		public void GetChangesAsync_Throws_On_Null_ChangesParameters()
		{
			Assert.ThrowsAsync<ArgumentNullException>(() => _couchApi.GetChangesAsync(null));
		}

		[Test]
		public void GetChangesAsync_Throws_On_Unsuccessful_GET()
		{
			IChangesParameters changesParameters;
			string expectedUrl;
			SetupChangesParameters(out changesParameters, out expectedUrl);

			//setup an error on the HTTP call
			_httpClient.Setup(c => c.GetAsync(expectedUrl))
				.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

			//make the call and check for the exception
			Assert.ThrowsAsync<HttpRequestException>(() => _couchApi.GetChangesAsync(changesParameters));
		}

		[Test]
		public async Task GetChangesAsync_Returns_Deserialized_Results()
		{
			//setup the view parameters
			IChangesParameters changesParameters;
			string expectedUrl;
			SetupChangesParameters(out changesParameters, out expectedUrl);

			var changes = new ChangesResult
			{
				LastSequence = "123",
				Results =
				{
					new Change { Id = "1" },
					new Change { Id = "2" },
				}
			};

			//create a response
			var response = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(JsonConvert.SerializeObject(changes))
			};

			//setup a successful HTTP call
			_httpClient.Setup(c => c.GetAsync(expectedUrl))
				.ReturnsAsync(response);

			//make the call and check the result
			var result = await _couchApi.GetChangesAsync(changesParameters);
			Assert.AreEqual(result.LastSequence, "123");
			Assert.AreEqual(result.Results[0].Id, "1");
			Assert.AreEqual(result.Results[1].Id, "2");
		}

		[Test]
		public async Task GetChangesAsync_Returns_Deserialized_Results_With_Document()
		{
			//setup the view parameters
			IChangesParameters changesParameters;
			string expectedUrl;
			SetupChangesParameters(out changesParameters, out expectedUrl);

			var changes = new ChangesResult<string>
			{
				LastSequence = "123",
				Results =
				{
					new Change<string> { Id = "1", Document = "test 1" },
					new Change<string> { Id = "2", Document = "test 2" }
				}
			};

			//create a response
			var response = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(JsonConvert.SerializeObject(changes))
			};

			//setup a successful HTTP call
			_httpClient.Setup(c => c.GetAsync(expectedUrl))
				.ReturnsAsync(response);

			//make the call and check the result
			var result = await _couchApi.GetChangesAsync<string>(changesParameters);
			Assert.AreEqual(result.LastSequence, "123");
			Assert.AreEqual(result.Results[0].Id, "1");
			Assert.AreEqual(result.Results[1].Id, "2");
			Assert.AreEqual(result.Results[0].Document, "test 1");
			Assert.AreEqual(result.Results[1].Document, "test 2");
		}

		[Test]
		public void GetLatestDocumentRevision_Throws_On_Null_ChangesParameters()
		{
			Assert.ThrowsAsync<ArgumentNullException>(() => _couchApi.GetLatestDocumentRevision(null));
			Assert.ThrowsAsync<ArgumentException>(() => _couchApi.GetLatestDocumentRevision(string.Empty));
		}

		[Test]
		public void GetLatestDocumentRevision_Throws_On_Unsuccessful_GET()
		{
			var expectedUrl = "https://server:1234/database/_all_docs?keys=[%22docid%22]";

			//setup an error on the HTTP call
			_httpClient.Setup(c => c.GetAsync(expectedUrl))
				.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

			//make the call and check for the exception
			Assert.ThrowsAsync<HttpRequestException>(() => _couchApi.GetLatestDocumentRevision("docid"));
		}

		[Test]
		public async Task GetLatestDocumentRevision_Returns_Deserialized_Results()
		{
			var expectedUrl = "https://server:1234/database/_all_docs?keys=[%22docid%22]";

			var changes = new ViewResult<object, ChangeRevision>();
			((List<ViewResultRow<object, ChangeRevision>>)changes.Rows).Add(
				new ViewResultRow<object, ChangeRevision> { Value = new ChangeRevision { Revision = "2" } });
			((List<ViewResultRow<object, ChangeRevision>>)changes.Rows).Add(
				new ViewResultRow<object, ChangeRevision> { Value = new ChangeRevision { Revision = "1" } });

			//create a response
			var response = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(JsonConvert.SerializeObject(changes))
			};

			//setup a successful HTTP call
			_httpClient.Setup(c => c.GetAsync(expectedUrl))
				.ReturnsAsync(response);

			//make the call and check the result
			var result = await _couchApi.GetLatestDocumentRevision("docid");
			Assert.AreEqual(changes.Rows.ElementAt(0).Value.Revision, result.Revision);
		}

		[Test]
		public void GetLatestDocumentRevisions_Throws_On_Null_ID_Array()
		{
			Assert.ThrowsAsync<ArgumentNullException>(() => _couchApi.GetLatestDocumentRevisions(null));
		}

		[Test]
		public async Task GetLatestDocumentRevisions_Ignores_Empty_Array()
		{
			var result = await _couchApi.GetLatestDocumentRevisions(Enumerable.Empty<Guid>());
			Assert.AreEqual(0, result.Count, "No results should be returned");
			_httpClient.Verify(c => c.PostAsync(
				It.IsAny<string>(),
				It.IsAny<HttpContent>()), Times.Never(), "No calls should have been made to the server");
		}

		[Test]
		public async Task GetLatestDocumentRevisions_Returns_Documents_From_Server()
		{
			//two unique ids, another one that won't be returned from the request and one duplicate
			var duplicateId = Guid.NewGuid();
			var ids         = new[] { Guid.NewGuid(), duplicateId, Guid.NewGuid(), duplicateId };
			var expectedUrl = "https://server:1234/database/_all_docs";

			var changes = new ViewResult<object, ChangeRevision>();
			((List<ViewResultRow<object, ChangeRevision>>)changes.Rows).Add(
				new ViewResultRow<object, ChangeRevision> { Id = ids[0], Value = new ChangeRevision { Revision = "rev2" } });
			((List<ViewResultRow<object, ChangeRevision>>)changes.Rows).Add(
				new ViewResultRow<object, ChangeRevision> { Id = ids[1], Value = new ChangeRevision { Revision = "rev1" } });

			//create a response
			var response = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(JsonConvert.SerializeObject(changes))
			};

			//setup a successful HTTP call
			string actualContent = null;
			_httpClient.Setup(c => c.PostAsync(expectedUrl,
				It.IsAny<HttpContent>()))
				.ReturnsAsync(response)
				.Callback<string, HttpContent>((_, content) => actualContent = content.ReadAsStringAsync().Result);

			var revisions = await _couchApi.GetLatestDocumentRevisions(ids);

			//check the content passed to the client
			Assert.IsNotNull(actualContent, "Content should have been included in the request");
			Assert.AreEqual(
				JsonConvert.SerializeObject(new { keys = ids.Distinct() }),
				actualContent,
				"The serialized IDs should have been included in the request");

			Assert.AreEqual(3, revisions.Count, "3 revisions should be returned");

			Assert.AreEqual(ids[0], revisions.ElementAt(0).Key);
			Assert.AreEqual("rev2", revisions.ElementAt(0).Value.Revision);
			Assert.AreEqual(ids[1], revisions.ElementAt(1).Key);
			Assert.AreEqual("rev1", revisions.ElementAt(1).Value.Revision);
			Assert.AreEqual(ids[2], revisions.ElementAt(2).Key);
			Assert.IsNull(revisions.ElementAt(2).Value);
			Assert.ThrowsAsync<ObjectDisposedException>(() => response.Content.ReadAsStringAsync());
		}

		[Test]
		public async Task GetLatestDocumentRevisions_Splits_Large_Requests_Into_Packets()
		{
			//three unique ids, another one that won't be returned from the request and one duplicate
			var duplicateId = Guid.NewGuid();
			var ids         = new[] { Guid.NewGuid(), Guid.NewGuid(), duplicateId, Guid.NewGuid(), duplicateId };
			var expectedUrl = "https://server:1234/database/_all_docs";

			var packet1Changes = new ViewResult<object, ChangeRevision>();
			((List<ViewResultRow<object, ChangeRevision>>)packet1Changes.Rows).Add(
				new ViewResultRow<object, ChangeRevision> { Id = ids[0], Value = new ChangeRevision { Revision = "rev2" } });
			((List<ViewResultRow<object, ChangeRevision>>)packet1Changes.Rows).Add(
				new ViewResultRow<object, ChangeRevision> { Id = ids[1], Value = new ChangeRevision { Revision = "rev1" } });

			//create a response
			var packet1Response = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(JsonConvert.SerializeObject(packet1Changes))
			};


			var packet2Changes = new ViewResult<object, ChangeRevision>();
			((List<ViewResultRow<object, ChangeRevision>>)packet2Changes.Rows).Add(
				new ViewResultRow<object, ChangeRevision> { Id = ids[2], Value = new ChangeRevision { Revision = "rev3" } });

			//create a response
			var packet2Response = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(JsonConvert.SerializeObject(packet2Changes))
			};


			var responses       = new Queue<HttpResponseMessage>(new[] { packet1Response, packet2Response });
			var requestContents = new List<string>();
			_httpClient.Setup(c => c.PostAsync(expectedUrl, It.IsAny<HttpContent>()))
				.Returns(() => Task.FromResult(responses.Dequeue()))
				.Callback<string, HttpContent>((s, content) => requestContents.Add(content.ReadAsStringAsync().Result));

			_couchApi.MaxDocumentsPerRequest = 2;

			var revisions = await _couchApi.GetLatestDocumentRevisions(ids);

			Assert.AreEqual(2, requestContents.Count, "2 separate requests should have been made");
			Assert.AreEqual(
				JsonConvert.SerializeObject(new { keys = new[] { ids[0], ids[1] } }),
				requestContents[0],
				"The first packet of serialized IDs should have been included in the request");
			Assert.AreEqual(
				JsonConvert.SerializeObject(new { keys = new[] { ids[2], ids[3] } }),
				requestContents[1],
				"The second packet of serialized IDs should have been included in the request");

			Assert.AreEqual(4, revisions.Count, "4 revisions should be returned");

			Assert.AreEqual(ids[0], revisions.ElementAt(0).Key);
			Assert.AreEqual("rev2", revisions.ElementAt(0).Value.Revision);
			Assert.AreEqual(ids[1], revisions.ElementAt(1).Key);
			Assert.AreEqual("rev1", revisions.ElementAt(1).Value.Revision);
			Assert.AreEqual(ids[2], revisions.ElementAt(2).Key);
			Assert.AreEqual("rev3", revisions.ElementAt(2).Value.Revision);
			Assert.AreEqual(ids[3], revisions.ElementAt(3).Key);
			Assert.IsNull(revisions.ElementAt(3).Value);
		}

		[Test]
		public void GetListResult_Throws_On_Null_Parameters()
		{
			Assert.ThrowsAsync<ArgumentNullException>(() => _couchApi.GetListResult<object>(null));
		}

		[Test]
		public async Task GetListResult_Returns_Results_From_Couch()
		{
			var viewParameters = new ViewParameters("viewName", "designName") { ListName = "listName" };
			var expectedUrl    = "https://server:1234/database/_design/designName/_list/listName/viewName?include_docs=true";

			var data = new[] {
				"one", "two", "three", "with _id", "with _rev"
			};

			//create a stream
			var stream = CreateStreamWithContent(data);

			//setup a successful HTTP call
			_httpClient.Setup(c => c.GetStreamAsync(expectedUrl)).ReturnsAsync(stream);

			//make the call and check the result
			var result = await _couchApi.GetListResult<IEnumerable<string>>(viewParameters);
			Assert.AreEqual(
				new[] { "one", "two", "three", "with _id", "with _rev" },
				result,
				"Data should have been serialized");
		}

		[Test]
		public void GetListResult_Throws_Timeout_Exception_On_Timeout_Error()
		{
			TestCouchTimeoutExceptions(viewParameters => _couchApi.GetListResult<object>(viewParameters));
		}

		[Test]
		public async Task SerializerSettings_Overrides_Default_Serializer_Settings()
		{
			var model = new DummyCouchModel();

			//create a server response
			var couchResponse = new CouchUpdateResponse { Rev = "new rev" };
			var response      = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(JsonConvert.SerializeObject(couchResponse))
			};

			//setup a PUT call that returns a success call and records the content
			HttpContent content = null;
			_httpClient.Setup(c => c.PutAsync(GetDocumentUri(model.Id.ToString()), It.IsAny<HttpContent>()))
				.ReturnsAsync(response)
				.Callback<string, HttpContent>((_, c) => content = c);

			using (_couchApi.CustomSettings(x => x.TypeNameHandling = TypeNameHandling.None))
				await _couchApi.CreateDocumentAsync(model);

			Assert.IsNotNull(content, "Content should have been passed to the server");
			var stringContent = await content.ReadAsStringAsync();
			var expected = @"{
""property"":""value"",
""enumValue"":0,
""_id"":""" + model.Id + @""",
""_attachments"":{},
""_deleted"":false,
""type"":""DummyCouchModel""}";

			Assert.AreEqual(expected.Replace("\r\n", string.Empty).Replace("\n", string.Empty), stringContent,
				"The document content should have been serialized using the custom settings (i.e. no $type property)");

			//now make the same call without the settings and check that the $type property comes back
			//setup a PUT call that returns a success call and records the content
			var modelDefault           = new DummyCouchModel();
			_httpClient.Setup(c => c.PutAsync(GetDocumentUri(modelDefault.Id.ToString()), It.IsAny<HttpContent>()))
					   .ReturnsAsync(response);
			await _couchApi.CreateDocumentAsync(modelDefault);

			var stringContentDefault = await content.ReadAsStringAsync();

			Assert.AreEqual(expected.Replace("\r\n", string.Empty).Replace("\n", string.Empty), stringContentDefault,
				"The document content should have been serialized using the default settings (i.e. $type property present)");
		}


		[Test]
		public async Task JsonConverters_Are_Applied()
		{
			var model = new DummyCouchModel { EnumValue = StringComparison.InvariantCulture };

			//create a server response
			var couchResponse = new CouchUpdateResponse { Rev = "new rev" };
			var response      = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(JsonConvert.SerializeObject(couchResponse))
			};

			//setup a PUT call that returns a success call and records the content
			HttpContent content = null;
			_httpClient.Setup(c => c.PutAsync(GetDocumentUri(model.Id.ToString()), It.IsAny<HttpContent>()))
				.ReturnsAsync(response)
				.Callback<string, HttpContent>((_, c) => content = c);

			//add a converter so that the enum value is serialized to a string
			_couchApi.Converters.Add(new StringEnumConverter());
			await _couchApi.CreateDocumentAsync(model);

			Assert.IsNotNull(content, "Content should have been passed to the server");
			var stringContent = await content.ReadAsStringAsync();
			var expected = @"{
""$type"":""Edge10.CouchDb.Client.Tests.TestCouchApi+DummyCouchModel, Edge10.CouchDb.Client.Tests"",
""property"":""value"",
""enumValue"":""InvariantCulture"",
""_id"":""" + model.Id + @""",
""_attachments"":{},
""_deleted"":false,
""type"":""DummyCouchModel""}";

			Assert.AreEqual(expected.Replace("\r\n", string.Empty).Replace("\n", string.Empty), stringContent,
				"The document content should have been serialized using the custom settings (i.e. no $type property)");

			//now make the same call without the settings and check that the enum converter
			//is not applied
			_couchApi.Converters.Clear();

			var modelDefault = new DummyCouchModel();
			_httpClient.Setup(c => c.PutAsync(GetDocumentUri(modelDefault.Id.ToString()), It.IsAny<HttpContent>()))
					   .ReturnsAsync(response);
			await _couchApi.CreateDocumentAsync(modelDefault);

			var stringContentDefault = await content.ReadAsStringAsync();

			Assert.AreEqual(expected.Replace("\r\n", string.Empty).Replace("\n", string.Empty), stringContentDefault,
				"The document content should have been serialized using the default settings (i.e. $type property present)");
		}

		private string QuoteString(string str)
		{
			return $"\"{str}\"";
		}

		private void SetSendHeadAsyncOnHttpClientFacade(string requestUri, HttpResponseMessage httpResponseMessage)
		{
			_httpClient.Setup(httpcf => httpcf.SendAsync(
				It.Is<HttpRequestMessage>(httpRequestMessage =>
					  httpRequestMessage.Method == HttpMethod.Head
					  && httpRequestMessage.RequestUri.AbsoluteUri == requestUri)))
				.ReturnsAsync(httpResponseMessage);
		}

		private async Task TestUrlGenerationFromServerString(string server, string expectedUrl)
		{
			var connectionString    = CreateConnectionString();
			connectionString.Server = server;
			var api                 = new CouchApi(connectionString, _httpClient.Object);

			//setup call to the HTTP client
			_httpClient.Setup(hc => hc.GetAsync(expectedUrl, HttpCompletionOption.ResponseHeadersRead))
					   .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

			await api.AttachmentExistsAsync("document", "attachment");
		}

		private ICouchDbConnectionStringBuilder CreateConnectionString()
		{
			return new CouchDbConnectionStringBuilder
			{
				DatabaseName = "database",
				Password     = "password",
				User         = "user",
				Server       = "https://server",
				Port         = 1234
			};
		}

		private void CheckHeader(AuthenticationHeaderValue header)
		{
			Assert.AreEqual("Basic", header.Scheme, "Should be basic authentication");
			var expectedParamter = Convert.ToBase64String(Encoding.UTF8.GetBytes("user:password"));
			Assert.AreEqual(expectedParamter, header.Parameter, "Should be encoded username and password");
		}

		private ViewResult<object, TRow> CreateViewResultWithRows<TRow>(params TRow[] rows)
		{
			var result = new ViewResult<object, TRow> { TotalRows = rows.Length };

			((List<ViewResultRow<object, TRow>>)result.Rows).AddRange(rows.Select(r =>
				new ViewResultRow<object, TRow> { Value = r }));

			return result;
		}

		private ViewResult<TDocument, object> CreateViewResultWithDocuments<TDocument>(params TDocument[] documents)
		{
			var result = new ViewResult<TDocument, object> { TotalRows = documents.Length };

			((List<ViewResultRow<TDocument, object>>)result.Rows).AddRange(documents.Select(r =>
				new ViewResultRow<TDocument, object> { Document = r }));

			return result;
		}

		private Stream CreateStreamWithContent(object data)
		{
			var stream = new MemoryStream();
			var writer = new StreamWriter(stream);
			new JsonSerializer().Serialize(new JsonTextWriter(writer), data);
			writer.Flush();
			stream.Position = 0;

			return stream;
		}

		private static void SetupViewParameters(out IViewParameters viewParameters, out string expectedUrl, bool expectIncludeDocs = true, bool? expectReduce = null)
		{
			viewParameters = new ViewParameters("view", "design") { Limit = 1 };
			var reducePart = expectReduce == null ? string.Empty : $"&reduce={expectReduce.ToString().ToLower()}";
			var includeDocsPart = expectIncludeDocs ? "true" : "false";
			expectedUrl = $"https://server:1234/database/_design/design/_view/view?limit=1&include_docs={includeDocsPart}{reducePart}";
		}

		private static void SetupChangesParameters(out IChangesParameters viewParameters, out string expectedUrl)
		{
			viewParameters = new ChangesParameters() { Limit = 1, ChangesFilter = "filter/name" };
			expectedUrl = "https://server:1234/database/_changes?filter=filter/name&limit=1";
		}

		private string GetBulkUri()
		{
			return $"{_connectionString.Server}:{_connectionString.Port}/{_connectionString.DatabaseName}/_bulk_docs";
		}

		private string GetDocumentUri(string documentId)
		{
			return $"{_connectionString.Server}:{_connectionString.Port}/{_connectionString.DatabaseName}/{documentId}";
		}

		private string GetAttachmentUri(string documentId, string attachmentName, string unquotedRevision)
		{
			return $"{_connectionString.Server}:{_connectionString.Port}/{_connectionString.DatabaseName}/{documentId}/{attachmentName}?rev={unquotedRevision}";
		}

		private void TestCouchTimeoutExceptions(Func<IViewParameters, Task> makeRequest, bool expectDocs = true)
		{
			var keys = new object[] { "one", "two" };
			var viewParameters = new ViewParameters("viewName", "designName") { Keys = keys };
			var expectedUrl = $"https://server:1234/database/_design/designName/_view/viewName?include_docs={expectDocs.ToString().ToLowerInvariant()}";

			var timeoutResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError) { ReasonPhrase = "original reason" };
			var nonTimeoutResponse1 = new HttpResponseMessage(HttpStatusCode.InternalServerError) { ReasonPhrase = "original reason" };
			var nonTimeoutResponse2 = new HttpResponseMessage(HttpStatusCode.InternalServerError) { ReasonPhrase = "original reason" };
			timeoutResponse.Content = new StringContent("nonsense TiMeOUt nonsense");
			nonTimeoutResponse1.Content = new StringContent("nonsense");
			//nonTimeoutResponse2 has no content

			_httpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), HttpCompletionOption.ResponseHeadersRead))
				.ReturnsAsync(timeoutResponse)
				.Callback<HttpRequestMessage, HttpCompletionOption>((request, completionOption) =>
				{
					Assert.AreEqual(HttpMethod.Post, request.Method, "Request should be a post");
					Assert.AreEqual(expectedUrl, request.RequestUri.ToString(), "Request URI should match");

					Assert.AreEqual("application/json", request.Content.Headers.ContentType.MediaType, "application/json is required");
					var stringContent = request.Content.ReadAsStringAsync().Result;
					Assert.AreEqual(@"{""keys"":[""one"",""two""]}", stringContent, "Keys should have been serialized");
				});

			Assert.ThrowsAsync<CouchTimeoutException>(() => makeRequest(viewParameters));

			//check a response with no mention of timeouts in the content
			_httpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), HttpCompletionOption.ResponseHeadersRead))
				.ReturnsAsync(nonTimeoutResponse1);

			var requestException = Assert.ThrowsAsync<HttpRequestException>(() => makeRequest(viewParameters));
			Assert.AreEqual("500 original reason nonsense", requestException.Message);

			//check a response with no content
			_httpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), HttpCompletionOption.ResponseHeadersRead))
				.ReturnsAsync(nonTimeoutResponse2);

			requestException = Assert.ThrowsAsync<HttpRequestException>(() => makeRequest(viewParameters));
			Assert.AreEqual("500 original reason ", requestException.Message);
		}

		class DummyCouchModel : CouchModelBase
		{
			public DummyCouchModel()
			{
				Id = string.Empty;
			}

			public string Property => "value";

			public StringComparison EnumValue { get; set; }
		}
	}
}