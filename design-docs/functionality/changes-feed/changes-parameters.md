# Changes Feed Parameters

**Navigation**: [Documentation Index](../../DocumentationIndex.md) | [Changes Feed Overview](changes-feed-overview.md)

## Changes Parameters Overview

The Edge10.CouchDb.Client provides comprehensive control over CouchDB's changes feed through the `ChangesParameters` class, which implements the `IChangesParameters` interface. This system enables real-time monitoring of database changes with flexible filtering, pagination, and document inclusion options.

## ChangesParameters Class

### Class Structure

```csharp
public class ChangesParameters : CouchQueryParametersBase, IChangesParameters
{
    // Changes-specific parameters
    public string ChangesFilter { get; set; }
    public int? ChangesSince { get; set; }
    public IDictionary<string, object> AdditionalParameters { get; } = new Dictionary<string, object>();
    
    // Inherited from CouchQueryParametersBase
    public bool? Descending { get; set; }
    public int? Limit { get; set; }
    public bool? IncludeDocs { get; set; }
}
```

### Interface Definition

```csharp
public interface IChangesParameters : ICouchQueryParameters
{
    string ChangesFilter { get; set; }
    int? ChangesSince { get; set; }
    IDictionary<string, object> AdditionalParameters { get; }
}
```

## Core Parameters

### Changes Feed Filtering

#### ChangesFilter
Specifies a filter function to apply to the changes feed:

```csharp
var parameters = new ChangesParameters
{
    ChangesFilter = "myapp/active_docs"
};
```

**Filter Function Format**: `design_document/filter_name`

**Common Filter Patterns:**
- Document type filtering: `app/by_type`
- User-specific changes: `app/by_user` 
- Status-based filtering: `app/by_status`
- Date range filtering: `app/by_date_range`

#### ChangesSince
Specifies the sequence number from which to retrieve changes:

```csharp
var parameters = new ChangesParameters
{
    ChangesSince = 12345  // Start from sequence 12345
};
```

**Use Cases:**
- Incremental synchronization
- Resume interrupted change processing
- Skip old changes on startup
- Implement checkpoint-based resumption

### Document Inclusion

#### IncludeDocs
Controls whether full documents are included in change notifications:

```csharp
// Include full documents (more data, slower)
parameters.IncludeDocs = true;

// Only change metadata (less data, faster)
parameters.IncludeDocs = false;
```

**Performance Impact:**
- `true`: Full document content, larger response, immediate access to data
- `false`: Only change metadata, smaller response, requires separate document fetches

### Pagination and Ordering

#### Limit
Restricts the number of changes returned:

```csharp
parameters.Limit = 100;  // Return maximum 100 changes
```

#### Descending
Controls the order of returned changes:

```csharp
parameters.Descending = true;   // Most recent changes first
parameters.Descending = false;  // Oldest changes first (default)
```

### Additional Filter Parameters

#### AdditionalParameters Dictionary
Provides custom parameters to filter functions:

```csharp
parameters.AdditionalParameters["doc_type"] = "customer";
parameters.AdditionalParameters["status"] = "active";
parameters.AdditionalParameters["created_after"] = "2023-01-01";
```

## Query String Generation

### Automatic Query String Creation

```csharp
public override string CreateQueryString()
{
    var builder = new StringBuilder();

    AppendQueryStringElement(builder, "filter", ChangesFilter);
    AppendQueryStringElement(builder, "since", ChangesSince);
    AppendQueryStringElement(builder, "limit", Limit);
    AppendQueryStringElement(builder, "descending", Descending);
    AppendQueryStringElement(builder, "include_docs", IncludeDocs);

    foreach (var item in AdditionalParameters)
        AppendQueryStringElement(builder, item.Key, item.Value);

    return builder.ToString();
}

private static void AppendQueryStringElement<T>(StringBuilder builder, string name, T value)
{
    if (value != null)
    {
        builder.Append(builder.Length == 0 ? "?" : "&");
        builder.AppendFormat(CultureInfo.InvariantCulture, "{0}={1}", name, value.ToString().ToLowerInvariant());
    }
}
```

## Filter Functions

### Design Document Filter Examples

#### JavaScript Filter Function (in design document)

```javascript
// Design document: _design/myapp
{
  "filters": {
    "by_type": function(doc, req) {
      return doc.type === req.query.doc_type;
    },
    
    "active_docs": function(doc, req) {
      return !doc._deleted && doc.status === "active";
    },
    
    "by_user": function(doc, req) {
      return doc.owner === req.query.user_id;
    },
    
    "recent_changes": function(doc, req) {
      var cutoff = new Date(req.query.since_date);
      var docDate = new Date(doc.modified_date);
      return docDate >= cutoff;
    }
  }
}
```

#### Using Filters with Parameters

```csharp
// Filter by document type
var typeFilterParams = new ChangesParameters
{
    ChangesFilter = "myapp/by_type",
    IncludeDocs = true
};
typeFilterParams.AdditionalParameters["doc_type"] = "customer";

// Filter by user
var userFilterParams = new ChangesParameters
{
    ChangesFilter = "myapp/by_user",
    IncludeDocs = false
};
userFilterParams.AdditionalParameters["user_id"] = "user-12345";

// Filter by date range
var dateFilterParams = new ChangesParameters
{
    ChangesFilter = "myapp/recent_changes",
    IncludeDocs = true
};
dateFilterParams.AdditionalParameters["since_date"] = "2023-01-01T00:00:00Z";
```

## Advanced Parameter Patterns

### Incremental Synchronization

```csharp
public class ChangesSynchronizer
{
    private int? _lastSequence;
    
    public async Task<ChangesResult<TDocument>> GetIncrementalChanges<TDocument>(
        ICouchApi couchApi,
        string filterName = null,
        int limit = 100)
    {
        var parameters = new ChangesParameters
        {
            ChangesSince = _lastSequence,
            Limit = limit,
            IncludeDocs = true,
            ChangesFilter = filterName
        };
        
        var result = await couchApi.GetChangesAsync<TDocument>(parameters);
        
        // Update checkpoint for next call
        if (result.Results.Any())
        {
            if (int.TryParse(result.LastSequence, out var lastSeq))
            {
                _lastSequence = lastSeq;
            }
        }
        
        return result;
    }
    
    public void SetCheckpoint(int sequenceNumber)
    {
        _lastSequence = sequenceNumber;
    }
}
```

### Filtered Change Monitoring

```csharp
public class DocumentTypeChangeMonitor
{
    private readonly ICouchApi _couchApi;
    private readonly string _documentType;
    
    public DocumentTypeChangeMonitor(ICouchApi couchApi, string documentType)
    {
        _couchApi = couchApi;
        _documentType = documentType;
    }
    
    public async Task<ChangesResult<TDocument>> GetTypeSpecificChanges<TDocument>(
        int? sinceSequence = null,
        bool includeDeleted = false)
    {
        var parameters = new ChangesParameters
        {
            ChangesFilter = "app/by_type",  
            ChangesSince = sinceSequence,
            IncludeDocs = true,
            Limit = 1000
        };
        
        parameters.AdditionalParameters["doc_type"] = _documentType;
        
        if (!includeDeleted)
        {
            parameters.AdditionalParameters["exclude_deleted"] = "true";
        }
        
        return await _couchApi.GetChangesAsync<TDocument>(parameters);
    }
}
```

### Batch Processing with Checkpoints

```csharp
public class BatchChangeProcessor
{
    private readonly ICouchApi _couchApi;
    private readonly int _batchSize;
    
    public BatchChangeProcessor(ICouchApi couchApi, int batchSize = 500)
    {
        _couchApi = couchApi;
        _batchSize = batchSize;
    }
    
    public async Task ProcessAllChanges<TDocument>(
        Func<List<Change<TDocument>>, Task> processor,
        int? startFromSequence = null,
        string filterName = null)
    {
        var currentSequence = startFromSequence;
        bool hasMoreChanges = true;
        
        while (hasMoreChanges)
        {
            var parameters = new ChangesParameters
            {
                ChangesSince = currentSequence,
                Limit = _batchSize,
                IncludeDocs = true,
                ChangesFilter = filterName
            };
            
            var result = await _couchApi.GetChangesAsync<TDocument>(parameters);
            
            if (result.Results.Any())
            {
                await processor(result.Results);
                
                // Update sequence for next batch
                if (int.TryParse(result.LastSequence, out var lastSeq))
                {
                    currentSequence = lastSeq;
                }
                
                hasMoreChanges = result.Results.Count == _batchSize;
            }
            else
            {
                hasMoreChanges = false;
            }
        }
    }
}
```

## Parameter Validation

### Runtime Parameter Validation

```csharp
public static class ChangesParametersValidator
{
    public static void Validate(IChangesParameters parameters)
    {
        if (parameters.ChangesSince.HasValue && parameters.ChangesSince.Value < 0)
            throw new ArgumentException("ChangesSince must be non-negative");
            
        if (parameters.Limit.HasValue && parameters.Limit.Value <= 0)
            throw new ArgumentException("Limit must be positive");
            
        if (!string.IsNullOrEmpty(parameters.ChangesFilter))
        {
            if (!parameters.ChangesFilter.Contains("/"))
                throw new ArgumentException("ChangesFilter must be in format 'design_doc/filter_name'");
        }
        
        // Validate additional parameters
        foreach (var param in parameters.AdditionalParameters)
        {
            if (string.IsNullOrWhiteSpace(param.Key))
                throw new ArgumentException("Additional parameter keys cannot be empty");
        }
    }
    
    public static void ValidateForContinuousMonitoring(IChangesParameters parameters)
    {
        Validate(parameters);
        
        if (!parameters.Limit.HasValue || parameters.Limit.Value > 10000)
            throw new ArgumentException("Limit should be specified and reasonable for continuous monitoring");
    }
}
```

## Usage Examples

### Basic Changes Monitoring

```csharp
public async Task MonitorBasicChanges()
{
    var parameters = new ChangesParameters
    {
        IncludeDocs = true,
        Limit = 100
    };
    
    var changes = await couchApi.GetChangesAsync<CustomerDocument>(parameters);
    
    foreach (var change in changes.Results)
    {
        if (change.Deleted)
        {
            Console.WriteLine($"Document {change.Id} was deleted");
        }
        else
        {
            Console.WriteLine($"Document {change.Id} was modified (rev: {change.Revision})");
            if (change.Document != null)
            {
                Console.WriteLine($"  Name: {change.Document.Name}");
            }
        }
    }
    
    Console.WriteLine($"Last sequence: {changes.LastSequence}");
}
```

### Filtered Changes by Document Type

```csharp
public async Task MonitorCustomerChanges(int? sinceSequence = null)
{
    var parameters = new ChangesParameters
    {
        ChangesFilter = "myapp/by_type",
        ChangesSince = sinceSequence,
        IncludeDocs = true,
        Limit = 50
    };
    
    parameters.AdditionalParameters["doc_type"] = "customer";
    
    var changes = await couchApi.GetChangesAsync<CustomerDocument>(parameters);
    
    foreach (var change in changes.Results)
    {
        await ProcessCustomerChange(change);
    }
}

private async Task ProcessCustomerChange(Change<CustomerDocument> change)
{
    if (change.Deleted)
    {
        await HandleCustomerDeleted(change.Id);
    }
    else if (change.Document != null)
    {
        await HandleCustomerModified(change.Document);
    }
}
```

### Continuous Monitoring Pattern

```csharp
public class ContinuousChangeMonitor
{
    private readonly ICouchApi _couchApi;
    private readonly Timer _timer;
    private int? _lastSequence;
    
    public ContinuousChangeMonitor(ICouchApi couchApi, TimeSpan interval)
    {
        _couchApi = couchApi;
        _timer = new Timer(CheckForChanges, null, TimeSpan.Zero, interval);
    }
    
    private async void CheckForChanges(object state)
    {
        try
        {
            var parameters = new ChangesParameters
            {
                ChangesSince = _lastSequence,
                IncludeDocs = true,
                Limit = 100,
                ChangesFilter = "myapp/active_docs"
            };
            
            var result = await _couchApi.GetChangesAsync<DocumentBase>(parameters);
            
            if (result.Results.Any())
            {
                await ProcessChanges(result.Results);
                
                if (int.TryParse(result.LastSequence, out var lastSeq))
                {
                    _lastSequence = lastSeq;
                }
            }
        }
        catch (Exception ex)
        {
            // Log error but continue monitoring
            Console.WriteLine($"Error checking for changes: {ex.Message}");
        }
    }
    
    private async Task ProcessChanges(List<Change<DocumentBase>> changes)
    {
        foreach (var change in changes)
        {
            // Process each change
            await ProcessSingleChange(change);
        }
    }
}
```

### Multi-Filter Change Processing

```csharp
public async Task ProcessMultipleFilters()
{
    var filterConfigs = new[]
    {
        new { Filter = "myapp/customers", Type = "customer" },
        new { Filter = "myapp/orders", Type = "order" },
        new { Filter = "myapp/products", Type = "product" }
    };
    
    var tasks = filterConfigs.Select(async config =>
    {
        var parameters = new ChangesParameters
        {
            ChangesFilter = config.Filter,
            IncludeDocs = true,
            Limit = 200
        };
        
        parameters.AdditionalParameters["doc_type"] = config.Type;
        
        var changes = await couchApi.GetChangesAsync<dynamic>(parameters);
        return new { Config = config, Changes = changes };
    });
    
    var results = await Task.WhenAll(tasks);
    
    foreach (var result in results)
    {
        Console.WriteLine($"Filter {result.Config.Filter}: {result.Changes.Results.Count} changes");
    }
}
```

### Error Handling and Resilience

```csharp
public async Task<ChangesResult<TDocument>> GetChangesWithRetry<TDocument>(
    ChangesParameters parameters,
    int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            return await couchApi.GetChangesAsync<TDocument>(parameters);
        }
        catch (HttpRequestException ex) when (attempt < maxRetries)
        {
            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // Exponential backoff
            Console.WriteLine($"Attempt {attempt} failed: {ex.Message}. Retrying in {delay}...");
            await Task.Delay(delay);
        }
    }
    
    // Final attempt without catching exceptions
    return await couchApi.GetChangesAsync<TDocument>(parameters);
}
```

## Performance Optimization

### Parameter Tuning for Performance

```csharp
public static class ChangesParameterOptimizer
{
    public static ChangesParameters OptimizeForThroughput(ChangesParameters parameters)
    {
        // Larger batch sizes for throughput
        parameters.Limit = parameters.Limit ?? 1000;
        
        // Don't include docs if not needed
        if (!parameters.IncludeDocs.HasValue)
            parameters.IncludeDocs = false;
            
        return parameters;
    }
    
    public static ChangesParameters OptimizeForRealtime(ChangesParameters parameters)
    {
        // Smaller batch sizes for lower latency
        parameters.Limit = Math.Min(parameters.Limit ?? 100, 100);
        
        // Include docs for immediate processing
        parameters.IncludeDocs = true;
        
        return parameters;
    }
    
    public static ChangesParameters OptimizeForResumability(ChangesParameters parameters, int checkpointSequence)
    {
        // Always start from known checkpoint
        parameters.ChangesSince = checkpointSequence;
        
        // Reasonable batch size for checkpoint management
        parameters.Limit = parameters.Limit ?? 500;
        
        return parameters;
    }
}
```

## Testing Changes Parameters

### Unit Testing Parameters

```csharp
[Test]
public void ChangesParameters_Should_GenerateCorrectQueryString()
{
    // Arrange
    var parameters = new ChangesParameters
    {
        ChangesFilter = "myapp/by_type",
        ChangesSince = 12345,
        Limit = 100,
        IncludeDocs = true,
        Descending = false
    };
    
    parameters.AdditionalParameters["doc_type"] = "customer";
    parameters.AdditionalParameters["status"] = "active";
    
    // Act
    var queryString = parameters.CreateQueryString();
    
    // Assert
    Assert.That(queryString, Contains.Substring("filter=myapp/by_type"));
    Assert.That(queryString, Contains.Substring("since=12345"));
    Assert.That(queryString, Contains.Substring("limit=100"));
    Assert.That(queryString, Contains.Substring("include_docs=true"));
    Assert.That(queryString, Contains.Substring("doc_type=customer"));
    Assert.That(queryString, Contains.Substring("status=active"));
}

[Test]
public void ChangesParametersValidator_Should_ThrowOnInvalidParameters()
{
    // Arrange
    var invalidParameters = new ChangesParameters
    {
        ChangesSince = -1,  // Invalid: negative sequence
        Limit = 0,          // Invalid: zero limit
        ChangesFilter = "invalid_format"  // Invalid: missing slash
    };
    
    // Act & Assert
    Assert.Throws<ArgumentException>(() => ChangesParametersValidator.Validate(invalidParameters));
}
```

## Best Practices

1. **Use appropriate batch sizes**: Balance between memory usage and network efficiency
2. **Implement checkpointing**: Store and resume from last processed sequence
3. **Filter at the database level**: Use filter functions to reduce network traffic
4. **Handle missing sequences**: Account for database compaction and sequence gaps
5. **Validate parameters**: Check parameter combinations and values
6. **Monitor filter performance**: Ensure filter functions are efficient
7. **Implement retry logic**: Handle transient network failures gracefully
8. **Use include_docs judiciously**: Only include documents when necessary
9. **Plan for continuous monitoring**: Design for long-running change monitoring
10. **Test with realistic data**: Validate performance with production-like datasets

---

*For more information about changes feed parameters, see [Changes Feed Overview](changes-feed-overview.md) and [Change Processing](change-processing.md).*