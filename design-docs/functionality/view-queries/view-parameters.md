# View Query Parameters

**Navigation**: [Documentation Index](../../DocumentationIndex.md) | [View Queries Overview](view-queries-overview.md)

## View Parameters Overview

The Edge10.CouchDb.Client provides comprehensive view query parameter configuration through the `ViewParameters` class, which implements the `IViewParameters` interface. This system allows for precise control over CouchDB view queries with type-safe parameter handling and automatic query string generation.

## ViewParameters Class

### Class Structure

```csharp
public class ViewParameters : CouchQueryParametersBase, IViewParameters
{
    public ViewParameters(string viewName, string designDocument)
    {
        ViewName = viewName;
        DesignDocument = designDocument;
        IncludeDocs = true; // Default to include documents
    }
    
    // Core view identification
    public string ViewName { get; set; }
    public string DesignDocument { get; set; }
    public string ListName { get; set; }
    
    // Query control parameters
    public bool? Group { get; set; }
    public int? GroupLevel { get; set; }
    public bool? Stale { get; set; }
    public bool? Reduce { get; set; }
    public bool? InclusiveEnd { get; set; }
    
    // Key filtering
    public KeyParameter Key { get; set; }
    public IEnumerable<object> Keys { get; set; }
    public IEnumerable<object> StartKey { get; set; }
    public IEnumerable<object> EndKey { get; set; }
    
    // Pagination
    public int? Skip { get; set; }
    
    // Additional parameters
    public IDictionary<string, string> QueryStringParameters { get; }
}
```

### Base Class Properties (CouchQueryParametersBase)

```csharp
public abstract class CouchQueryParametersBase
{
    public bool? Descending { get; set; }
    public int? Limit { get; set; }
    public bool? IncludeDocs { get; set; }
    
    public abstract string CreateQueryString();
}
```

## Core Parameters

### View Identification Parameters

#### ViewName and DesignDocument
```csharp
var parameters = new ViewParameters("customers-by-city", "customers");
// Results in: /db/_design/customers/_view/customers-by-city
```

#### ListName (Optional)
```csharp
var parameters = new ViewParameters("customers-by-city", "customers")
{
    ListName = "formatted-customers"
};
// Results in: /db/_design/customers/_list/formatted-customers/customers-by-city
```

### Document Inclusion

#### IncludeDocs
Controls whether full documents are returned with view results:

```csharp
// Include full documents (default: true)
parameters.IncludeDocs = true;

// Only return view keys and values
parameters.IncludeDocs = false;
```

**Impact on Performance:**
- `true`: Larger response payload, full document access
- `false`: Smaller response, only view key/value data

## Key-Based Filtering

### Single Key Filtering

```csharp
// Simple key
parameters.Key = new KeyParameter("Chicago");

// Complex key (array)
parameters.Key = new KeyParameter(new object[] { "Chicago", "Premium" });

// Typed key
parameters.Key = new KeyParameter(customerId);
```

### Multiple Keys (POST to View)

```csharp
// Query for specific keys
parameters.Keys = new object[] 
{ 
    "Chicago", 
    "New York", 
    "Los Angeles" 
};

// Complex keys
parameters.Keys = new object[]
{
    new object[] { "Chicago", "Premium" },
    new object[] { "New York", "Standard" },
    new object[] { "Los Angeles", "Premium" }
};
```

**Note**: When `Keys` is specified, it overrides `Key`, `StartKey`, and `EndKey` parameters.

### Range Filtering

```csharp
// Simple range
parameters.StartKey = new object[] { "A" };
parameters.EndKey = new object[] { "M" };

// Date range
parameters.StartKey = new object[] { DateTime.Parse("2023-01-01") };
parameters.EndKey = new object[] { DateTime.Parse("2023-12-31") };

// Complex range with multiple fields
parameters.StartKey = new object[] { "Chicago", DateTime.Parse("2023-01-01") };
parameters.EndKey = new object[] { "Chicago", DateTime.Parse("2023-12-31") };
```

#### InclusiveEnd
Controls whether the end key is included in results:

```csharp
parameters.InclusiveEnd = true;  // Include documents matching EndKey (default)
parameters.InclusiveEnd = false; // Exclude documents matching EndKey exactly
```

## Sorting and Ordering

### Descending Order

```csharp
parameters.Descending = true;  // Reverse the natural sort order
parameters.Descending = false; // Natural sort order (default)
```

**Effect on Key Ranges:**
When using `Descending = true`, you may need to swap `StartKey` and `EndKey`:

```csharp
// Ascending: A to Z
parameters.StartKey = new object[] { "A" };
parameters.EndKey = new object[] { "Z" };

// Descending: Z to A
parameters.Descending = true;
parameters.StartKey = new object[] { "Z" };
parameters.EndKey = new object[] { "A" };
```

## Pagination Parameters

### Limit
Restricts the number of results returned:

```csharp
parameters.Limit = 50; // Return maximum 50 results
```

### Skip
Skips the specified number of results (for pagination):

```csharp
// Page 1: First 50 results
parameters.Limit = 50;
parameters.Skip = 0;

// Page 2: Next 50 results  
parameters.Limit = 50;
parameters.Skip = 50;

// Page 3: Next 50 results
parameters.Limit = 50;
parameters.Skip = 100;
```

**Performance Note**: Large `Skip` values can be inefficient. Consider using key-based pagination for better performance.

## Reduce and Grouping Parameters

### Reduce
Controls whether the view's reduce function is executed:

```csharp
parameters.Reduce = true;  // Execute reduce function (default for reduce views)
parameters.Reduce = false; // Skip reduce, return individual results
```

### Group
Groups reduce results by the complete key:

```csharp
parameters.Reduce = true;
parameters.Group = true; // Group by complete key
```

### GroupLevel
Groups reduce results by partial keys:

```csharp
parameters.Reduce = true;
parameters.GroupLevel = 1; // Group by first element of key
parameters.GroupLevel = 2; // Group by first two elements of key
```

**Example with Complex Keys:**
```javascript
// View emits: [country, state, city] as key
// GroupLevel = 1: Groups by country
// GroupLevel = 2: Groups by country + state  
// GroupLevel = 3: Groups by country + state + city (full key)
```

## Performance Parameters

### Stale
Allows querying potentially stale views for better performance:

```csharp
parameters.Stale = true;  // Allow stale results (faster)
parameters.Stale = false; // Force view update (slower, current data)
```

**Use Cases:**
- `true`: Acceptable for analytics, reporting, non-critical real-time data
- `false`: Required for transactional operations, critical real-time data

## Query String Generation

### Automatic Query String Creation

The `ViewParameters` class automatically generates properly formatted query strings:

```csharp
public override string CreateQueryString()
{
    var parameters = new Dictionary<string, string>();
    
    if (Descending.HasValue)
        parameters.Add("descending", Descending.Value.ToString().ToLower());
    if (Limit.HasValue)
        parameters.Add("limit", Limit.Value.ToString());
    if (IncludeDocs.HasValue)
        parameters.Add("include_docs", IncludeDocs.Value.ToString().ToLower());
    if (Group.HasValue)
        parameters.Add("group", Group.Value.ToString().ToLower());
    if (GroupLevel.HasValue)
        parameters.Add("group_level", GroupLevel.Value.ToString());
    if (Stale.GetValueOrDefault())
        parameters.Add("stale", "ok");
    if (Reduce.HasValue)
        parameters.Add("reduce", Reduce.Value.ToString().ToLower());
    if (InclusiveEnd.HasValue)
        parameters.Add("inclusive_end", InclusiveEnd.Value.ToString().ToLower());
    if (Skip.HasValue)
        parameters.Add("skip", Skip.Value.ToString());
    if (Key != null)
        parameters.Add("key", Key.FormattedValue);
    if (StartKey != null && StartKey.Any())
        parameters.Add("startkey", PrepareArrayValue(StartKey));
    if (EndKey != null && EndKey.Any())
        parameters.Add("endkey", PrepareArrayValue(EndKey));

    // Add custom parameters
    foreach (var additionalParameter in QueryStringParameters)
        parameters[additionalParameter.Key] = additionalParameter.Value;

    // Build query string
    var builder = new StringBuilder();
    foreach (var param in parameters)
    {
        builder.Append(builder.Length == 0 ? "?" : "&");
        builder.Append(param.Key);
        builder.Append("=");
        builder.Append(param.Value);
    }

    return builder.ToString();
}
```

### Custom Query Parameters

```csharp
// Add custom parameters not covered by standard properties
parameters.QueryStringParameters["update_seq"] = "true";
parameters.QueryStringParameters["conflicts"] = "true";
```

## Advanced Parameter Patterns

### Key-Based Pagination

More efficient than Skip-based pagination for large datasets:

```csharp
public async Task<IEnumerable<CustomerDocument>> GetCustomersPagedByKey(
    string lastCustomerName = null, 
    int pageSize = 50)
{
    var parameters = new ViewParameters("customers-by-name", "customers")
    {
        Limit = pageSize + 1, // Get one extra to determine if more pages exist
        IncludeDocs = true
    };
    
    if (!string.IsNullOrEmpty(lastCustomerName))
    {
        parameters.StartKey = new object[] { lastCustomerName };
        parameters.Skip = 1; // Skip the last item from previous page
    }
    
    var results = await couchApi.GetViewDocumentsAsync<CustomerDocument>(parameters);
    var resultsList = results.ToList();
    
    // Remove the extra item if we got more than requested
    if (resultsList.Count > pageSize)
    {
        resultsList.RemoveAt(resultsList.Count - 1);
        // The removed item's key can be used for the next page
    }
    
    return resultsList;
}
```

### Complex Key Queries

```csharp
public async Task<IEnumerable<OrderDocument>> GetOrdersByCustomerAndDateRange(
    string customerId, 
    DateTime startDate, 
    DateTime endDate)
{
    var parameters = new ViewParameters("orders-by-customer-and-date", "orders")
    {
        StartKey = new object[] { customerId, startDate },
        EndKey = new object[] { customerId, endDate },
        IncludeDocs = true,
        InclusiveEnd = true
    };
    
    return await couchApi.GetViewDocumentsAsync<OrderDocument>(parameters);
}
```

### Grouped Aggregations

```csharp
public async Task<IDictionary<string, int>> GetOrderCountsByCustomer()
{
    var parameters = new ViewParameters("orders-count-by-customer", "orders")
    {
        Reduce = true,
        Group = true,
        IncludeDocs = false // Not needed for reduce results
    };
    
    var results = await couchApi.GetViewRowsAsync<int>(parameters);
    
    // Convert view results to dictionary
    // Note: This would need the full ViewResult to get keys
    // See result-processing.md for complete examples
}
```

## Parameter Validation

### Runtime Validation

```csharp
public static class ViewParametersValidator
{
    public static void Validate(IViewParameters parameters)
    {
        if (string.IsNullOrWhiteSpace(parameters.ViewName))
            throw new ArgumentException("ViewName is required");
            
        if (string.IsNullOrWhiteSpace(parameters.DesignDocument))
            throw new ArgumentException("DesignDocument is required");
            
        if (parameters.Keys != null && parameters.Keys.Any())
        {
            if (parameters.Key != null)
                throw new ArgumentException("Cannot specify both Key and Keys");
            if (parameters.StartKey != null || parameters.EndKey != null)
                throw new ArgumentException("Cannot specify Keys with StartKey/EndKey");
        }
        
        if (parameters.GroupLevel.HasValue && !parameters.Group.GetValueOrDefault())
            throw new ArgumentException("GroupLevel requires Group to be true");
            
        if (parameters.Skip.HasValue && parameters.Skip.Value < 0)
            throw new ArgumentException("Skip cannot be negative");
            
        if (parameters.Limit.HasValue && parameters.Limit.Value <= 0)
            throw new ArgumentException("Limit must be positive");
    }
}
```

## Usage Examples

### Basic View Query

```csharp
var parameters = new ViewParameters("customers-by-city", "customers")
{
    Key = new KeyParameter("Chicago"),
    IncludeDocs = true,
    Limit = 100
};

var customers = await couchApi.GetViewDocumentsAsync<CustomerDocument>(parameters);
```

### Range Query with Pagination

```csharp
var parameters = new ViewParameters("products-by-price", "products")
{
    StartKey = new object[] { 10.00 },
    EndKey = new object[] { 100.00 },
    Limit = 50,
    Skip = 0,
    Descending = false,
    IncludeDocs = true
};

var products = await couchApi.GetViewDocumentsAsync<ProductDocument>(parameters);
```

### Multiple Key Query

```csharp
var parameters = new ViewParameters("customers-by-id", "customers")
{
    Keys = new object[] { "customer-1", "customer-2", "customer-3" },
    IncludeDocs = true
};

var customers = await couchApi.GetViewDocumentsAsync<CustomerDocument>(parameters);
```

### Grouped Reduce Query

```csharp
var parameters = new ViewParameters("sales-by-region-and-month", "sales")
{
    Reduce = true,
    GroupLevel = 2, // Group by region and month
    StartKey = new object[] { "North", "2023-01" },
    EndKey = new object[] { "North", "2023-12" }
};

var salesTotals = await couchApi.GetViewRowsAsync<decimal>(parameters);
```

### Stale View Query for Analytics

```csharp
var parameters = new ViewParameters("analytics-summary", "reports")
{
    Stale = true, // Allow stale data for better performance
    Reduce = true,
    Group = true,
    IncludeDocs = false
};

var analyticsData = await couchApi.GetViewRowsAsync<AnalyticsSummary>(parameters);
```

## Performance Optimization

### Query Optimization Guidelines

1. **Use specific keys when possible**: More efficient than ranges
2. **Limit result sets**: Use `Limit` to avoid large responses
3. **Consider stale queries**: For non-critical data, `Stale = true` improves performance
4. **Use key-based pagination**: More efficient than large `Skip` values
5. **Avoid `IncludeDocs` when not needed**: Reduces response size
6. **Group at appropriate levels**: Balance between granularity and performance

### Parameter Tuning

```csharp
public static class ViewParameterOptimizer
{
    public static ViewParameters OptimizeForPerformance(ViewParameters parameters)
    {
        // Enable stale queries for better performance if acceptable
        if (parameters.Stale == null)
            parameters.Stale = true;
            
        // Limit large result sets
        if (!parameters.Limit.HasValue || parameters.Limit.Value > 1000)
            parameters.Limit = 1000;
            
        // Optimize document inclusion
        if (parameters.IncludeDocs == null)
            parameters.IncludeDocs = false; // Only if document data not needed
            
        return parameters;
    }
}
```

## Common Patterns and Best Practices

1. **Always specify view and design document names**: Required for all queries
2. **Use typed key parameters**: Leverage `KeyParameter` for type safety
3. **Validate parameters**: Check for conflicting parameter combinations
4. **Consider query performance**: Balance between freshness and speed
5. **Use appropriate pagination**: Key-based for large datasets, Skip for small ones
6. **Test with realistic data**: Ensure queries perform well with production data
7. **Document view dependencies**: Clearly specify which views your code depends on
8. **Handle parameter conflicts**: Validate mutually exclusive parameters
9. **Monitor query performance**: Track slow queries and optimize parameters
10. **Use reduce functions appropriately**: Understand when to enable/disable reduce

---

*For more information about view query parameters, see [View Queries Overview](view-queries-overview.md) and [Result Processing](result-processing.md).*