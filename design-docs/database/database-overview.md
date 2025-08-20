# Database Overview

**Navigation**: [Documentation Index](../DocumentationIndex.md) | [Main Documentation](../Edge10.CouchDb.Client.md)

## CouchDB Integration Overview

Edge10.CouchDb.Client provides comprehensive integration with CouchDB, a NoSQL document database that stores data as JSON documents. This integration follows CouchDB's document-oriented architecture while providing .NET-specific conveniences.

## CouchDB Concepts and Client Library Mapping

### Document Storage Model

```mermaid
graph TD
    subgraph "CouchDB Database"
        DB[(Database)]
        DOC1[Document 1]
        DOC2[Document 2]
        DOC3[Document 3]
        
        DOC1 --> FIELDS1[_id, _rev, data fields]
        DOC2 --> FIELDS2[_id, _rev, data fields]
        DOC3 --> FIELDS3[_id, _rev, data fields]
        
        DOC1 --> ATTACH1[Attachments]
        DOC2 --> ATTACH2[Attachments]
    end
    
    subgraph "Client Library"
        IMODEL[ICouchModel Interface]
        BASE[CouchModelBase]
        IMPL[Document Implementation]
        
        IMODEL --> PROPS[Id, Rev, Type, Deleted, Attachments]
        BASE --> MAPPING[JSON Property Mapping]
        IMPL --> CUSTOM[Custom Document Fields]
    end
    
    DB --> IMODEL
    FIELDS1 --> PROPS
    ATTACH1 --> MAPPING
```

### Core CouchDB Properties

Every CouchDB document has standard system properties that the client library manages:

| CouchDB Field | .NET Property | Purpose | Managed By |
|---------------|---------------|---------|------------|
| `_id` | `Id` | Unique document identifier | Client/Server |
| `_rev` | `Rev` | Document revision for optimistic locking | Server |
| `_deleted` | `Deleted` | Soft deletion flag | Client |
| `_attachments` | `Attachments` | File attachment metadata | Client/Server |
| `type` | `Type` | Document type discrimination | Client |

## Document Modeling Architecture

### ICouchModel Interface

```csharp
public interface ICouchModel
{
    string Id { get; set; }                           // CouchDB _id
    string Rev { get; set; }                          // CouchDB _rev
    IList<IAttachmentMetaData> Attachments { get; }   // CouchDB _attachments
    bool Deleted { get; set; }                        // CouchDB _deleted
    bool IsNew { get; }                              // Computed: no revision
    string Type { get; set; }                         // Document type field
}
```

### CouchModelBase Implementation

```csharp
public abstract class CouchModelBase : ICouchModel
{
    [JsonProperty(PropertyName = "_id")]
    public string Id { get; set; }

    [JsonProperty(PropertyName = "_rev", NullValueHandling = NullValueHandling.Ignore)]
    public string Rev { get; set; }

    [JsonProperty(PropertyName = "_attachments")]
    [JsonConverter(typeof(AttachmentsConverter))]
    public IList<IAttachmentMetaData> Attachments { get; } = new List<IAttachmentMetaData>();

    [JsonProperty(PropertyName = "_deleted")]
    public bool Deleted { get; set; }

    [JsonIgnore]
    public bool IsNew => string.IsNullOrWhiteSpace(Rev);

    [JsonProperty("type")]
    public string Type { get; set; }
}
```

## Document Lifecycle Management

### Document State Transitions

```mermaid
stateDiagram-v2
    [*] --> New : Create Instance
    New --> Created : CreateDocumentAsync()
    Created --> Updated : UpdateDocumentAsync()
    Updated --> Updated : UpdateDocumentAsync()
    Updated --> Deleted : Set Deleted = true
    Deleted --> [*] : BulkUpdateAsync()
    
    Created --> Conflicted : Concurrent Update
    Updated --> Conflicted : Concurrent Update
    Conflicted --> Updated : Manual Resolution
    
    note right of New : IsNew = true\nRev = null
    note right of Created : IsNew = false\nRev = "1-xxxxx"
    note right of Updated : Rev incremented\nRev = "2-xxxxx"
    note right of Deleted : Deleted = true\nRev maintained
```

### Revision Management

CouchDB uses Multi-Version Concurrency Control (MVCC) through document revisions:

```csharp
// Automatic revision handling in updates
private async Task UpdateOrCreateDocumentAsync<TDocument>(TDocument document, bool isNew)
    where TDocument : ICouchModel
{
    var documentUri = GetDocumentUrl(document.Id);
    
    // Resolve current revision for updates
    if (!isNew && string.IsNullOrWhiteSpace(document.Rev))
    {
        var revision = await GetDocumentRevision(documentUri);
        document.Rev = revision;
    }

    document.Type = typeof(TDocument).Name;

    var content = SerializeDocument(document);
    var response = await _client.PutAsync(documentUri, content);

    // Handle revision conflicts
    if (response.StatusCode == HttpStatusCode.Conflict)
        throw new ConflictException();

    await response.ThrowErrorIfNotSuccess();
    await UpdateDocumentRevision(document, response);
}
```

## Attachment Management

### Attachment Metadata Structure

```mermaid
classDiagram
    class IAttachmentMetaData {
        <<interface>>
        +string Name
        +string ContentType
        +int Length
        +string Digest
        +bool Stub
    }
    
    class AttachmentMetaData {
        +string Name
        +string ContentType
        +int Length
        +string Digest
        +bool Stub
    }
    
    class AttachmentsConverter {
        +WriteJson(writer, value, serializer)
        +ReadJson(reader, objectType, existingValue, serializer)
    }
    
    IAttachmentMetaData <|-- AttachmentMetaData
    AttachmentsConverter --> IAttachmentMetaData
```

### Attachment Operations

```csharp
// Attachment existence check
public async Task<bool> AttachmentExistsAsync(string documentId, string attachmentName)
{
    var result = await _client.GetAsync(
        GetAttachmentUrl(documentId, attachmentName),
        HttpCompletionOption.ResponseHeadersRead);

    using (result)
        return result.IsSuccessStatusCode;
}

// Stream-based attachment retrieval
public async Task<Stream> GetAttachmentStreamAsync(string documentId, string attachmentName)
{
    var content = await GetAttachment(documentId, attachmentName, throwOnError: true);
    return await content.ReadAsStreamAsync();
}

// Attachment upload with revision management
public async Task PutAsync(string documentId, string attachmentName, HttpContent httpContent)
{
    var requestUri = await GetAttachmentUrlWithRevision(documentId, attachmentName);
    var response = await _client.PutAsync(requestUri, httpContent);
    await response.ThrowErrorIfNotSuccess();
}
```

## View Query Architecture

### CouchDB Views Integration

```mermaid
graph LR
    subgraph "CouchDB Server"
        DD[Design Document]
        VIEW[Map/Reduce Views]
        IDX[View Indexes]
    end
    
    subgraph "Client Library"
        VP[ViewParameters]
        QS[Query String Builder]
        RES[ViewResult Processing]
    end
    
    subgraph "Query Types"
        SIMPLE[Simple Queries]
        KEYED[Key-based Queries]
        PAGED[Paged Queries]
    end
    
    VP --> QS
    QS --> SIMPLE
    QS --> KEYED
    QS --> PAGED
    
    SIMPLE --> DD
    KEYED --> DD
    PAGED --> DD
    
    DD --> VIEW
    VIEW --> IDX
    IDX --> RES
```

### View Parameter Construction

```csharp
public class ViewParameters : CouchQueryParametersBase, IViewParameters
{
    public string ViewName { get; set; }              // View name in design document
    public string DesignDocument { get; set; }        // Design document name
    public string ListName { get; set; }              // Optional list function
    
    // Query parameters
    public bool? Group { get; set; }                  // Group results
    public int? GroupLevel { get; set; }              // Group level for complex keys
    public bool? Reduce { get; set; }                 // Enable/disable reduce
    public KeyParameter Key { get; set; }             // Single key query
    public IEnumerable<object> Keys { get; set; }     // Multiple keys (POST)
    public IEnumerable<object> StartKey { get; set; } // Range start
    public IEnumerable<object> EndKey { get; set; }   // Range end
    public int? Skip { get; set; }                    // Pagination offset
}
```

### View Result Processing

```csharp
// View result structure mapping
public class ViewResult<TDocument, TValue>
{
    [JsonProperty("rows")]
    public IList<ViewResultRow<TDocument, TValue>> Rows { get; set; }
    
    [JsonProperty("total_rows")]
    public int TotalRows { get; set; }
    
    [JsonProperty("offset")]
    public int Offset { get; set; }
}

// Paged result wrapper
public async Task<IPagedResult<TDocument>> GetPagedViewDocumentsAsync<TDocument>(IViewParameters viewParameters)
{
    // Get documents with reduce disabled
    viewParameters.Reduce = false;
    var viewResult = await GetListResult<ViewResult<TDocument, object>>(viewParameters);
    
    // Get count with reduce enabled
    viewParameters.Reduce = true;
    viewParameters.Skip = null;
    viewParameters.Limit = null;
    viewParameters.IncludeDocs = false;
    var countResult = await GetListResult<ViewResult<object, long>>(viewParameters);
    
    var count = countResult.Rows.FirstOrDefault()?.Value ?? 0;
    return new PagedResult<TDocument>(viewResult.Rows.Select(r => r.Document).ToArray(), count);
}
```

## Changes Feed Integration

### Changes Feed Architecture

```mermaid
sequenceDiagram
    participant Client as Client Application
    participant API as CouchApi
    participant Params as ChangesParameters
    participant HTTP as HttpClientFacade
    participant DB as CouchDB
    
    Client->>Params: Configure filters, since, limit
    Client->>API: GetChangesAsync(parameters)
    API->>Params: CreateQueryString()
    Params-->>API: Query string
    API->>HTTP: GET /db/_changes?params
    HTTP->>DB: HTTP Request
    DB-->>HTTP: Changes JSON
    HTTP-->>API: HttpResponseMessage
    API->>API: Deserialize ChangesResult
    API-->>Client: ChangesResult<T>
```

### Changes Parameters

```csharp
public class ChangesParameters : CouchQueryParametersBase, IChangesParameters
{
    public string ChangesFilter { get; set; }                      // Server-side filter
    public int? ChangesSince { get; set; }                        // Sequence number to start from
    public IDictionary<string, object> AdditionalParameters { get; } // Filter parameters
}
```

### Changes Result Processing

```csharp
public class ChangesResult<TDocument>
{
    [JsonProperty("results")]
    public List<Change<TDocument>> Results { get; private set; }
    
    [JsonProperty("last_seq")]
    public string LastSequence { get; set; }
}

public class Change<TDocument>
{
    [JsonProperty("id")]
    public string Id { get; set; }
    
    [JsonProperty("changes")]
    public List<ChangeRevision> Changes { get; set; }
    
    [JsonProperty("doc")]
    public TDocument Document { get; set; }
    
    [JsonProperty("deleted")]
    public bool? Deleted { get; set; }
}
```

## Bulk Operations Architecture

### Bulk Update Implementation

```mermaid
graph TD
    DOCS[Document Collection] --> VALIDATE[Validate & Type Assignment]
    VALIDATE --> SERIALIZE[Serialize to Bulk Format]
    SERIALIZE --> POST[POST to /_bulk_docs]
    POST --> RESPONSE[Process Response Array]
    RESPONSE --> CONFLICTS{Check Conflicts}
    CONFLICTS -->|Has Conflicts| THROW[Throw ConflictException]
    CONFLICTS -->|No Conflicts| UPDATE[Update Revisions]
    UPDATE --> COMPLETE[Complete]
```

### Bulk Request/Response Format

```csharp
// Bulk request format
var bulkRequest = new { Docs = documents };

// Bulk response processing
var updateResults = JsonConvert.DeserializeObject<CouchBulkUpdateResponseItem[]>(responseContent);

// Conflict detection
var conflictIds = updateResults.Where(x => x.Error == "conflict").Select(x => x.Id.ToString()).ToArray();
if (conflictIds.Any())
    throw new ConflictException(string.Join(", ", conflictIds));

// Revision updates
var revisionsById = updateResults.ToDictionary(x => x.Id, x => x.Rev);
foreach (var document in docsArray)
    document.Rev = revisionsById[document.Id];
```

## Connection Management

### Connection String Architecture

```csharp
public interface ICouchDbConnectionStringBuilder
{
    string Server { get; set; }        // Server hostname/IP
    int Port { get; set; }             // Server port (default: 5984)
    string DatabaseName { get; set; }  // Target database name
    string User { get; set; }          // Authentication username
    string Password { get; set; }      // Authentication password
}
```

### URL Construction

```csharp
private static string GetServerUrl(ICouchDbConnectionStringBuilder connectionString)
{
    return $"{((connectionString.Server.StartsWith("http://") || connectionString.Server.StartsWith("https://")) ? string.Empty : "http://")}{connectionString.Server}:{connectionString.Port}";
}
```

## Database Performance Considerations

### Batch Size Management
```csharp
public int MaxDocumentsPerRequest { get; set; } = 500;

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
```

### Memory Management
- Stream-based operations for large attachments
- Configurable batch sizes for bulk operations
- Proper disposal of HTTP resources

### Concurrency Considerations
- No document-level locking (CouchDB MVCC)
- Conflict detection and handling
- Thread-safe HTTP client usage

---

*This database overview provides comprehensive coverage of how the client library integrates with CouchDB's document-oriented architecture while providing .NET-specific conveniences and type safety.*