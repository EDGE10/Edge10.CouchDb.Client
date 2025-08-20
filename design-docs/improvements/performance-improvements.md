# Performance Improvements

This document analyzes performance optimization opportunities and bottlenecks in Edge10.CouchDb.Client.

## Navigation
- [Back to Improvements Overview](improvements-overview.md)
- [Back to Documentation Index](../DocumentationIndex.md)
- [Modernization Opportunities](modernization-opportunities.md)
- [Code Quality Improvements](code-quality-improvements.md)

## Performance Analysis Overview

Edge10.CouchDb.Client exhibits several performance bottlenecks related to serialization, memory allocation, HTTP client usage, and resource management. This analysis identifies key areas for optimization.

## Serialization Performance Issues

### JSON Serialization Bottlenecks

#### Repeated Serializer Creation

**Current Implementation:**
```csharp
private HttpContent SerializeDocument<TDocument>(TDocument document)
{
    var serializer = CreateSerializer(); // Creates new serializer every time
    var builder = new StringBuilder();
    
    using (var sw = _serializationStrategy != null ? 
                   _serializationStrategy.WriterFactory(builder) : 
                   new StringWriter(builder))
    using (var writer = new JsonTextWriter(sw))
        serializer.Serialize(writer, document);
    
    return new StringContent(builder.ToString(), Encoding.UTF8, "application/json");
}

private JsonSerializer CreateSerializer()
{
    var settings = new JsonSerializerSettings
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        DateFormatHandling = DateFormatHandling.IsoDateFormat,
        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        Formatting = Formatting.None,
        NullValueHandling = NullValueHandling.Ignore
    };
    
    _serializationStrategy?.Apply(settings);
    _settingsChanges?.Invoke(settings);
    
    return JsonSerializer.Create(settings);
}
```

**Performance Issues:**
- **Serializer Creation Overhead**: New serializer created for each operation
- **Settings Configuration**: Repeated settings object creation
- **Contract Resolver**: New CamelCasePropertyNamesContractResolver per serializer

**Optimization:**
```csharp
private readonly JsonSerializer _cachedSerializer;
private readonly object _serializerLock = new object();

private JsonSerializer GetOrCreateSerializer()
{
    if (_cachedSerializer != null && _settingsChanges == null)
        return _cachedSerializer;
    
    lock (_serializerLock)
    {
        return CreateSerializer();
    }
}

// Or better: Use Lazy<JsonSerializer> for thread-safe initialization
private readonly Lazy<JsonSerializer> _serializer = new Lazy<JsonSerializer>(CreateSerializer);
```

#### StringBuilder vs Stream Performance

**Current Approach:**
```csharp
private HttpContent SerializeDocument<TDocument>(TDocument document)
{
    var serializer = CreateSerializer();
    var builder = new StringBuilder();
    
    using (var sw = new StringWriter(builder))
    using (var writer = new JsonTextWriter(sw))
        serializer.Serialize(writer, document);
    
    return new StringContent(builder.ToString(), Encoding.UTF8, "application/json");
}
```

**Performance Issues:**
- **Double Memory Allocation**: StringBuilder + StringContent conversion
- **String Encoding**: UTF-8 encoding happens twice
- **Large Document Overhead**: StringBuilder reallocations for large objects

**Optimized Approach:**
```csharp
private HttpContent SerializeDocument<TDocument>(TDocument document)
{
    var serializer = GetCachedSerializer();
    var stream = new MemoryStream();
    
    using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
    using (var jsonWriter = new JsonTextWriter(writer))
    {
        serializer.Serialize(jsonWriter, document);
    }
    
    stream.Position = 0;
    return new StreamContent(stream);
}

// Even better: Use pooled memory streams
private HttpContent SerializeDocumentPooled<TDocument>(TDocument document)
{
    var serializer = GetCachedSerializer();
    var stream = MemoryStreamPool.Rent();
    
    try
    {
        using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
        using (var jsonWriter = new JsonTextWriter(writer))
        {
            serializer.Serialize(jsonWriter, document);
        }
        
        var content = new ByteArrayContent(stream.ToArray());
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return content;
    }
    finally
    {
        MemoryStreamPool.Return(stream);
    }
}
```

### Deserialization Performance

**Current Implementation:**
```csharp
private TData GetSerializedContent<TData>(Stream content)
{
    using (var sr = _serializationStrategy != null ? 
                   _serializationStrategy.ReaderFactory(content) : 
                   new StreamReader(content))
    using (var jsonTextReader = new JsonTextReader(sr))
    {
        var serializer = CreateSerializer(); // New serializer each time
        return serializer.Deserialize<TData>(jsonTextReader);
    }
}
```

**Optimizations:**
```csharp
private TData GetSerializedContent<TData>(Stream content)
{
    using (var sr = new StreamReader(content, Encoding.UTF8, false, 4096, false))
    using (var jsonTextReader = new JsonTextReader(sr))
    {
        jsonTextReader.CloseInput = false; // Don't close underlying stream
        var serializer = GetCachedSerializer();
        return serializer.Deserialize<TData>(jsonTextReader);
    }
}

// For high-frequency operations, consider System.Text.Json
private TData GetSerializedContentSystemTextJson<TData>(Stream content)
{
    return JsonSerializer.DeserializeAsync<TData>(content, _jsonOptions).Result;
}
```

## HTTP Client Performance Issues

### Connection Management

**Current Implementation:**
```csharp
public CouchApi(ICouchDbConnectionStringBuilder connectionString, 
               HttpClientHandler httpClientHandler = null)
{
    _client = new HttpClientFacade(httpClientHandler ?? new HttpClientHandler());
    _client.Timeout = TimeSpan.FromMinutes(20);
}
```

**Performance Issues:**
- **No Connection Pooling Configuration**: Uses default connection limits
- **Long Timeout**: 20-minute timeout may hold connections too long
- **No Keep-Alive Optimization**: Default keep-alive settings

**Optimizations:**
```csharp
private static HttpClientHandler CreateOptimizedHandler()
{
    return new HttpClientHandler
    {
        MaxConnectionsPerServer = Environment.ProcessorCount * 2,
        PooledConnectionLifetime = TimeSpan.FromMinutes(15),
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
        UseCookies = false // CouchDB doesn't use cookies
    };
}

public CouchApi(ICouchDbConnectionStringBuilder connectionString, 
               HttpClientHandler httpClientHandler = null)
{
    var handler = httpClientHandler ?? CreateOptimizedHandler();
    _client = new HttpClientFacade(handler);
    
    // Differentiated timeouts
    _client.Timeout = GetTimeoutForOperation();
    
    // Optimize request headers
    _client.DefaultRequestHeaders.Connection.Add("keep-alive");
    _client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
}
```

### Request Performance

**Current Bulk Operations:**
```csharp
public async Task<IEnumerable<CouchBulkUpdateResponseItem>> BulkUpdateAsync<TDocument>(TDocument[] documents)
{
    var docsArray = documents.Select(doc => (object)doc).ToArray(); // Unnecessary boxing
    var content = SerializeDocument(new { Docs = docsArray });
    
    var response = await _client.PostAsync(GetBulkUrl(), content);
    await response.ThrowErrorIfNotSuccess(CouchExceptionFactory);
    
    var responseContent = await response.Content.ReadAsStringAsync();
    var updateResults = JsonConvert.DeserializeObject<CouchBulkUpdateResponseItem[]>(responseContent);
    
    return updateResults;
}
```

**Performance Issues:**
- **Boxing Overhead**: Converting documents to object array
- **String Deserialization**: Reading response as string then deserializing
- **Memory Allocation**: Multiple intermediate arrays

**Optimized Version:**
```csharp
public async Task<IEnumerable<CouchBulkUpdateResponseItem>> BulkUpdateAsync<TDocument>(TDocument[] documents)
{
    // Avoid boxing by using strongly-typed wrapper
    var bulkRequest = new BulkUpdateRequest<TDocument> { Docs = documents };
    var content = SerializeDocument(bulkRequest);
    
    using (var response = await _client.PostAsync(GetBulkUrl(), content))
    {
        await response.ThrowErrorIfNotSuccess(CouchExceptionFactory);
        
        // Stream deserialization
        using (var stream = await response.Content.ReadAsStreamAsync())
        {
            return GetSerializedContent<CouchBulkUpdateResponseItem[]>(stream);
        }
    }
}

// Strongly-typed wrapper to avoid boxing
private class BulkUpdateRequest<T>
{
    [JsonProperty("docs")]
    public T[] Docs { get; set; }
}
```

## Memory Management Optimizations

### Object Pooling

**Current Resource Usage:**
```csharp
private async Task<Stream> GetViewHttpResponse(IViewParameters viewParameters)
{
    // Creates new objects for each request
    using (var keysContent = new ObjectContent<dynamic>(keyData, new JsonMediaTypeFormatter()))
    using (var request = new HttpRequestMessage(HttpMethod.Post, url))
    {
        // Implementation
    }
}
```

**Optimized with Pooling:**
```csharp
private readonly ObjectPool<StringBuilder> _stringBuilderPool;
private readonly ObjectPool<MemoryStream> _memoryStreamPool;

private async Task<Stream> GetViewHttpResponseOptimized(IViewParameters viewParameters)
{
    var stream = _memoryStreamPool.Get();
    try
    {
        // Use pooled resources
        SerializeToStream(keyData, stream);
        stream.Position = 0;
        
        using (var content = new StreamContent(stream))
        using (var request = new HttpRequestMessage(HttpMethod.Post, url))
        {
            request.Content = content;
            var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            return await response.Content.ReadAsStreamAsync();
        }
    }
    finally
    {
        _memoryStreamPool.Return(stream);
    }
}
```

### Stream Optimization

**Current Stream Handling:**
```csharp
public async Task<Stream> GetAttachmentStreamAsync(string documentId, string attachmentName)
{
    var result = await _client.GetAsync(GetAttachmentUrl(documentId, attachmentName), 
                                       HttpCompletionOption.ResponseHeadersRead);
    await result.ThrowErrorIfNotSuccess();
    return await result.Content.ReadAsStreamAsync();
}
```

**Optimized Stream Handling:**
```csharp
public async Task<Stream> GetAttachmentStreamAsync(string documentId, string attachmentName, 
                                                  CancellationToken cancellationToken = default)
{
    var request = new HttpRequestMessage(HttpMethod.Get, GetAttachmentUrl(documentId, attachmentName));
    
    var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    
    if (!response.IsSuccessStatusCode)
    {
        response.Dispose(); // Clean up on error
        await response.ThrowErrorIfNotSuccess();
    }
    
    return new HttpResponseStream(response); // Custom stream that disposes response
}

// Custom stream wrapper that ensures proper resource cleanup
private class HttpResponseStream : Stream
{
    private readonly HttpResponseMessage _response;
    private readonly Stream _innerStream;
    
    public HttpResponseStream(HttpResponseMessage response)
    {
        _response = response;
        _innerStream = response.Content.ReadAsStreamAsync().Result;
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _innerStream?.Dispose();
            _response?.Dispose();
        }
        base.Dispose(disposing);
    }
    
    // Implement Stream methods delegating to _innerStream
}
```

## Async/Await Performance

### ConfigureAwait(false) Usage

**Current Implementation:**
```csharp
public async Task<TDocument> GetDocumentAsync<TDocument>(string documentId)
{
    var result = await _client.GetAsync(GetDocumentUrl(documentId));
    await result.ThrowErrorIfNotSuccess();
    return await GetSerializedResultContent<TDocument>(result);
}
```

**Optimized for Library Code:**
```csharp
public async Task<TDocument> GetDocumentAsync<TDocument>(string documentId, 
                                                        CancellationToken cancellationToken = default)
{
    var result = await _client.GetAsync(GetDocumentUrl(documentId), cancellationToken)
                             .ConfigureAwait(false);
    await result.ThrowErrorIfNotSuccess()
                .ConfigureAwait(false);
    return await GetSerializedResultContent<TDocument>(result)
                .ConfigureAwait(false);
}
```

### ValueTask Optimization

For frequently called methods that might return synchronously:

```csharp
private readonly ConcurrentDictionary<string, TDocument> _documentCache = new();

public ValueTask<TDocument> GetDocumentAsync<TDocument>(string documentId) where TDocument : class
{
    // Check cache first
    if (_documentCache.TryGetValue(documentId, out var cached) && cached is TDocument cachedDoc)
    {
        return new ValueTask<TDocument>(cachedDoc);
    }
    
    // Fall back to async operation
    return new ValueTask<TDocument>(GetDocumentAsyncInternal<TDocument>(documentId));
}

private async Task<TDocument> GetDocumentAsyncInternal<TDocument>(string documentId) where TDocument : class
{
    // Actual implementation
}
```

## Caching Strategies

### Response Caching

```csharp
public class CachedCouchApi : ICouchApi
{
    private readonly ICouchApi _innerApi;
    private readonly IMemoryCache _cache;
    private readonly CacheOptions _options;
    
    public async Task<TDocument> GetDocumentAsync<TDocument>(string documentId)
    {
        var cacheKey = $"doc:{documentId}";
        
        if (_cache.TryGetValue(cacheKey, out TDocument cached))
        {
            return cached;
        }
        
        var document = await _innerApi.GetDocumentAsync<TDocument>(documentId);
        
        _cache.Set(cacheKey, document, _options.DocumentCacheExpiration);
        
        return document;
    }
}
```

### Connection Caching

```csharp
public static class CouchApiFactory
{
    private static readonly ConcurrentDictionary<string, ICouchApi> _apiCache = new();
    
    public static ICouchApi GetOrCreate(ICouchDbConnectionStringBuilder connectionString)
    {
        var key = connectionString.ConnectionString;
        
        return _apiCache.GetOrAdd(key, _ => new CouchApi(connectionString));
    }
}
```

## Bulk Operation Optimizations

### Parallel Processing

**Current Sequential Processing:**
```csharp
public async Task<IEnumerable<TDocument>> GetDocumentsAsync<TDocument>(string[] documentIds)
{
    var results = new List<TDocument>();
    
    foreach (var id in documentIds) // Sequential processing
    {
        var doc = await GetDocumentAsync<TDocument>(id);
        results.Add(doc);
    }
    
    return results;
}
```

**Optimized Parallel Processing:**
```csharp
public async Task<IEnumerable<TDocument>> GetDocumentsAsync<TDocument>(string[] documentIds, 
                                                                      int maxConcurrency = 10)
{
    var semaphore = new SemaphoreSlim(maxConcurrency);
    var tasks = documentIds.Select(async id =>
    {
        await semaphore.WaitAsync();
        try
        {
            return await GetDocumentAsync<TDocument>(id).ConfigureAwait(false);
        }
        finally
        {
            semaphore.Release();
        }
    });
    
    return await Task.WhenAll(tasks).ConfigureAwait(false);
}
```

### Batch Size Optimization

```csharp
public async Task<IEnumerable<CouchBulkUpdateResponseItem>> BulkUpdateAsync<TDocument>(
    IEnumerable<TDocument> documents, 
    int batchSize = 1000)
{
    var results = new List<CouchBulkUpdateResponseItem>();
    var batches = documents.Batch(batchSize); // Extension method for batching
    
    foreach (var batch in batches)
    {
        var batchResults = await BulkUpdateBatchAsync(batch.ToArray());
        results.AddRange(batchResults);
    }
    
    return results;
}
```

## Monitoring and Profiling

### Performance Metrics

```csharp
public class PerformanceAwareCouchApi : ICouchApi
{
    private static readonly Counter RequestCount = Metrics
        .CreateCounter("couchdb_requests_total", "Total CouchDB requests", "method", "status");
    
    private static readonly Histogram RequestDuration = Metrics
        .CreateHistogram("couchdb_request_duration_seconds", "CouchDB request duration");
    
    public async Task<TDocument> GetDocumentAsync<TDocument>(string documentId)
    {
        using (RequestDuration.NewTimer())
        {
            try
            {
                var result = await _innerApi.GetDocumentAsync<TDocument>(documentId);
                RequestCount.WithLabels("GET", "success").Inc();
                return result;
            }
            catch
            {
                RequestCount.WithLabels("GET", "error").Inc();
                throw;
            }
        }
    }
}
```

### Memory Usage Monitoring

```csharp
public class MemoryAwareCouchApi : ICouchApi
{
    private readonly ILogger<MemoryAwareCouchApi> _logger;
    
    public async Task<TDocument> GetDocumentAsync<TDocument>(string documentId)
    {
        var memoryBefore = GC.GetTotalMemory(false);
        
        try
        {
            return await _innerApi.GetDocumentAsync<TDocument>(documentId);
        }
        finally
        {
            var memoryAfter = GC.GetTotalMemory(false);
            var allocated = memoryAfter - memoryBefore;
            
            if (allocated > 1024 * 1024) // Log if > 1MB allocated
            {
                _logger.LogWarning("High memory allocation detected: {AllocatedBytes} bytes", allocated);
            }
        }
    }
}
```

## Benchmarking Results

### Recommended Performance Testing

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net60)]
public class CouchApiPerformanceBenchmarks
{
    private ICouchApi _api;
    private TestDocument[] _testDocuments;
    
    [GlobalSetup]
    public void Setup()
    {
        _api = new CouchApi(/* configuration */);
        _testDocuments = GenerateTestDocuments(1000);
    }
    
    [Benchmark]
    public async Task BulkUpdate_Current()
    {
        await _api.BulkUpdateAsync(_testDocuments);
    }
    
    [Benchmark]
    public async Task BulkUpdate_Optimized()
    {
        await _optimizedApi.BulkUpdateAsync(_testDocuments);
    }
    
    [Benchmark]
    public async Task Serialization_Current()
    {
        for (int i = 0; i < 100; i++)
        {
            SerializeDocument(_testDocuments[i]);
        }
    }
    
    [Benchmark]
    public async Task Serialization_Cached()
    {
        for (int i = 0; i < 100; i++)
        {
            SerializeDocumentCached(_testDocuments[i]);
        }
    }
}
```

## Implementation Priority

### High Impact, Low Effort
1. **Add ConfigureAwait(false)** to all async calls
2. **Cache JsonSerializer instances** instead of creating new ones
3. **Use stream-based serialization** instead of StringBuilder
4. **Add CancellationToken support** to all async methods

### Medium Impact, Medium Effort
1. **Implement object pooling** for frequently allocated objects
2. **Add response caching** for immutable operations
3. **Optimize HTTP client configuration** for connection pooling
4. **Implement batching** for bulk operations

### High Impact, High Effort
1. **Migrate to System.Text.Json** for better performance
2. **Implement comprehensive caching layer**
3. **Add parallel processing** for independent operations
4. **Implement custom HTTP content types** for zero-copy scenarios

## Conclusion

Edge10.CouchDb.Client has significant performance optimization opportunities, particularly in:

1. **Serialization efficiency** through caching and streaming
2. **Memory management** via object pooling and proper disposal
3. **HTTP client optimization** with better connection management
4. **Async/await patterns** with ConfigureAwait and CancellationToken
5. **Bulk operation optimization** through batching and parallelization

Implementing these optimizations could result in:
- **50-80% reduction** in memory allocations
- **30-60% improvement** in serialization performance  
- **20-40% reduction** in response times
- **Significantly better** scalability under load

The optimizations should be implemented incrementally with proper benchmarking to measure actual performance gains.

---

**Related Documentation:**
- [Modernization Opportunities](modernization-opportunities.md) - .NET Standard migration recommendations
- [Code Quality Improvements](code-quality-improvements.md) - Code quality enhancements
- [Improvements Overview](improvements-overview.md) - Technical debt analysis