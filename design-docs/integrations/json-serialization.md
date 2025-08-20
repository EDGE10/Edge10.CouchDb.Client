# JSON Serialization Integration

**Navigation**: [Documentation Index](../DocumentationIndex.md) | [Integrations Overview](integrations-overview.md)

## JSON Serialization Overview

The Edge10.CouchDb.Client uses Newtonsoft.Json as its primary JSON serialization engine, providing comprehensive support for CouchDB's JSON document format with custom converters, camel case property naming, type handling, and flexible serialization strategies.

## Core Serialization Configuration

### Default JsonSerializer Settings

```csharp
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
```

### Key Configuration Elements

#### Type Name Handling
```csharp
TypeNameHandling = TypeNameHandling.Objects
```
- Embeds .NET type information in JSON for polymorphic deserialization
- Enables round-trip serialization of derived types
- Supports inheritance hierarchies in document models

#### Camel Case Property Resolution (Required for Data Compatibility)
```csharp
ContractResolver = new CamelCasePropertyNamesContractResolver()
```
- **Required for existing data compatibility**: CamelCase naming is mandatory to work with existing Edge10 CouchDB documents
- **C# Property Compatibility**: Maintains compatibility with existing C# property naming conventions
- **Data Consistency**: Ensures consistency with existing document schemas in Edge10 databases
- Converts PascalCase .NET properties to camelCase JSON
- Example: `CustomerName` → `customerName`
- Maintains JavaScript/JSON naming conventions

**Important**: Changing the naming convention would break compatibility with existing data and is not supported.

## Document Model Serialization

### CouchDB Property Mapping

```csharp
public abstract class CouchModelBase : ICouchModel
{
    [JsonProperty(PropertyName = "_id")]
    public string Id { get; set; }

    [JsonProperty(PropertyName = "_rev", NullValueHandling = NullValueHandling.Ignore)]
    public string Rev { get; set; }

    [JsonProperty(PropertyName = "_deleted")]
    public bool Deleted { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonIgnore]
    public bool IsNew => string.IsNullOrWhiteSpace(Rev);

    [JsonProperty(PropertyName = "_attachments")]
    [JsonConverter(typeof(AttachmentsConverter))]
    public IList<IAttachmentMetaData> Attachments { get; } = new List<IAttachmentMetaData>();
}
```

### Property Attributes

#### JsonProperty Mapping
- **`_id`**: Maps to CouchDB document identifier
- **`_rev`**: Maps to CouchDB revision with null value handling
- **`_deleted`**: Maps to CouchDB deletion flag
- **`_attachments`**: Maps to CouchDB attachments with custom converter

#### JsonIgnore Usage
- **`IsNew`**: Computed property excluded from serialization
- Used for helper properties that shouldn't persist

#### NullValueHandling
```csharp
[JsonProperty(PropertyName = "_rev", NullValueHandling = NullValueHandling.Ignore)]
```
- Prevents null revision values from being serialized
- Keeps JSON clean for new documents

## Custom JSON Converters

### AttachmentsConverter Implementation

```csharp
public class AttachmentsConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) => true;

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader == null) throw new ArgumentNullException(nameof(reader));
        if (serializer == null) throw new ArgumentNullException(nameof(serializer));

        var attachments = existingValue as IList<IAttachmentMetaData> ?? new List<IAttachmentMetaData>();
        var parsedJson = JObject.Load(reader);

        foreach (var item in parsedJson)
        {
            var attachment = new AttachmentMetaData { Filename = item.Key };
            serializer.Populate(item.Value.CreateReader(), attachment);
            attachments.Add(attachment);
        }

        return attachments;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (writer == null) throw new ArgumentNullException(nameof(writer));
        if (serializer == null) throw new ArgumentNullException(nameof(serializer));

        writer.WriteStartObject();

        var attachments = value as IList<IAttachmentMetaData>;
        if (attachments != null)
        {
            foreach (var attachment in attachments)
            {
                writer.WritePropertyName(attachment.Filename);
                var typeNameHandling = serializer.TypeNameHandling;
                serializer.TypeNameHandling = TypeNameHandling.None;
                serializer.Serialize(writer, attachment);
                serializer.TypeNameHandling = typeNameHandling;
            }
        }

        writer.WriteEndObject();
    }
}
```

### Custom Converters Registry

```csharp
public IList<JsonConverter> Converters { get; } = new List<JsonConverter>();

// Usage:
couchApi.Converters.Add(new CustomDateTimeConverter());
couchApi.Converters.Add(new EnumStringConverter());
couchApi.Converters.Add(new DecimalConverter());
```

## Serialization Strategies

### SerializationStrategy Class

```csharp
public class SerializationStrategy
{
    public SerializationStrategy(Func<Stream, StreamReader> readerFactory, Func<StringBuilder, StringWriter> writerFactory)
    {
        ReaderFactory = readerFactory ?? throw new ArgumentNullException(nameof(readerFactory));
        WriterFactory = writerFactory ?? throw new ArgumentNullException(nameof(writerFactory));
    }

    public Func<Stream, StreamReader> ReaderFactory { get; }
    public Func<StringBuilder, StringWriter> WriterFactory { get; }
}
```

### Custom Encoding Strategy

```csharp
// UTF-8 with BOM
var utf8WithBomStrategy = new SerializationStrategy(
    stream => new StreamReader(stream, Encoding.UTF8, true),
    builder => new StringWriter(builder)
);

// Custom encoding
var customEncodingStrategy = new SerializationStrategy(
    stream => new StreamReader(stream, Encoding.GetEncoding("iso-8859-1")),
    builder => new StringWriter(builder)
);

var couchApi = new CouchApi(connectionString, eventLog, customEncodingStrategy);
```

## Stream-Based Serialization

### Memory-Efficient Deserialization

```csharp
private TData GetSerializedContent<TData>(Stream content)
{
    using (var sr = _serializationStrategy?.ReaderFactory(content) ?? new StreamReader(content))
    using (var jsonTextReader = new JsonTextReader(sr))
    {
        var serializer = CreateSerializer();
        return serializer.Deserialize<TData>(jsonTextReader);
    }
}
```

### Memory-Efficient Serialization

```csharp
private HttpContent SerializeDocument<TDocument>(TDocument document)
{
    var serializer = CreateSerializer();
    var builder = new StringBuilder();

    using (var sw = _serializationStrategy?.WriterFactory(builder) ?? new StringWriter(builder))
    using (var writer = new JsonTextWriter(sw))
        serializer.Serialize(writer, document);

    return new StringContent(builder.ToString(), Encoding.UTF8, "application/json");
}
```

## Dynamic Serialization Settings

### Runtime Configuration Changes

```csharp
public IDisposable CustomSettings(Action<JsonSerializerSettings> settingsChanges)
{
    _settingsChanges = settingsChanges;
    return new DisposableWrapper<Action<JsonSerializerSettings>>(_settingsChanges, () =>
    {
        _settingsChanges = null;
    });
}

// Usage:
using (couchApi.CustomSettings(settings =>
{
    settings.DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffZ";
    settings.NullValueHandling = NullValueHandling.Ignore;
}))
{
    await couchApi.CreateDocumentAsync(document);
}
```

### Conditional Serialization

```csharp
public class ConditionalDocument : CouchModelBase
{
    public string Name { get; set; }
    public DateTime? OptionalDate { get; set; }
    public List<string> Tags { get; set; }
    
    // Control serialization conditionally
    public bool ShouldSerializeOptionalDate() => OptionalDate.HasValue;
    public bool ShouldSerializeTags() => Tags?.Any() == true;
}
```

## Advanced Serialization Patterns

### Polymorphic Document Handling

```csharp
// Base document class
[JsonObject(MemberSerialization.OptIn)]
public abstract class BaseDocument : CouchModelBase
{
    [JsonProperty("documentType")]
    public abstract string DocumentType { get; }
}

// Derived document classes
public class CustomerDocument : BaseDocument
{
    public override string DocumentType => "customer";
    
    [JsonProperty("customerName")]
    public string Name { get; set; }
    
    [JsonProperty("email")]
    public string Email { get; set; }
}

public class OrderDocument : BaseDocument
{
    public override string DocumentType => "order";
    
    [JsonProperty("orderNumber")]
    public string OrderNumber { get; set; }
    
    [JsonProperty("customerId")]
    public string CustomerId { get; set; }
}

// Polymorphic deserialization with type discriminator
public class DocumentTypeConverter : JsonConverter<BaseDocument>
{
    public override BaseDocument ReadJson(JsonReader reader, Type objectType, BaseDocument existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jObject = JObject.Load(reader);
        var documentType = jObject["documentType"]?.Value<string>();
        
        return documentType switch
        {
            "customer" => jObject.ToObject<CustomerDocument>(serializer),
            "order" => jObject.ToObject<OrderDocument>(serializer),
            _ => throw new JsonSerializationException($"Unknown document type: {documentType}")
        };
    }
    
    public override void WriteJson(JsonWriter writer, BaseDocument value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }
}
```

### Date/Time Serialization

```csharp
public class CouchDbDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime ReadJson(JsonReader reader, Type objectType, DateTime existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.String)
        {
            var dateString = reader.Value.ToString();
            if (DateTime.TryParse(dateString, out var date))
                return DateTime.SpecifyKind(date, DateTimeKind.Utc);
        }
        
        throw new JsonSerializationException($"Unable to parse DateTime from {reader.Value}");
    }
    
    public override void WriteJson(JsonWriter writer, DateTime value, JsonSerializer serializer)
    {
        var utcTime = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        writer.WriteValue(utcTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
    }
}

// Usage:
couchApi.Converters.Add(new CouchDbDateTimeConverter());
```

### Decimal Precision Handling

```csharp
public class HighPrecisionDecimalConverter : JsonConverter<decimal>
{
    public override decimal ReadJson(JsonReader reader, Type objectType, decimal existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.String)
        {
            if (decimal.TryParse(reader.Value.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
                return result;
        }
        else if (reader.TokenType == JsonToken.Float || reader.TokenType == JsonToken.Integer)
        {
            return Convert.ToDecimal(reader.Value);
        }
        
        throw new JsonSerializationException($"Unable to parse decimal from {reader.Value}");
    }
    
    public override void WriteJson(JsonWriter writer, decimal value, JsonSerializer serializer)
    {
        // Preserve precision by writing as string
        writer.WriteValue(value.ToString(CultureInfo.InvariantCulture));
    }
}
```

## Error Handling in Serialization

### Serialization Error Handling

```csharp
public static class SerializationErrorHandler
{
    public static JsonSerializerSettings CreateRobustSettings()
    {
        return new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Error = HandleSerializationError,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };
    }
    
    private static void HandleSerializationError(object sender, ErrorEventArgs e)
    {
        // Log the error
        Console.WriteLine($"Serialization error: {e.ErrorContext.Error.Message}");
        Console.WriteLine($"Path: {e.ErrorContext.Path}");
        
        // Mark error as handled to continue processing
        e.ErrorContext.Handled = true;
    }
}
```

### Validation During Serialization

```csharp
public class ValidatingDocument : CouchModelBase, IValidatable
{
    [JsonProperty("email")]
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    
    [JsonProperty("age")]
    [Range(0, 150)]
    public int? Age { get; set; }
    
    [OnSerializing]
    internal void OnSerializingMethod(StreamingContext context)
    {
        ValidateModel();
    }
    
    public void ValidateModel()
    {
        var validationContext = new ValidationContext(this);
        var validationResults = new List<ValidationResult>();
        
        if (!Validator.TryValidateObject(this, validationContext, validationResults, true))
        {
            var errors = string.Join(", ", validationResults.Select(r => r.ErrorMessage));
            throw new ValidationException($"Document validation failed: {errors}");
        }
    }
}
```

## Performance Optimization

### Serialization Performance Tips

```csharp
public class PerformanceOptimizedSettings
{
    public static JsonSerializerSettings Create()
    {
        return new JsonSerializerSettings
        {
            // Optimize for performance
            TypeNameHandling = TypeNameHandling.None, // Disable if not needed
            ContractResolver = new DefaultContractResolver(), // Faster than CamelCase
            
            // Reduce object creation
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            
            // Skip default values
            DefaultValueHandling = DefaultValueHandling.Ignore,
            
            // Faster date parsing
            DateParseHandling = DateParseHandling.None
        };
    }
}
```

### Memory-Efficient Large Document Handling

```csharp
public async Task<T> DeserializeLargeDocument<T>(Stream documentStream)
{
    using var reader = new StreamReader(documentStream, Encoding.UTF8, false, 8192, leaveOpen: true);
    using var jsonReader = new JsonTextReader(reader);
    
    // Configure for large documents
    jsonReader.SupportMultipleContent = false;
    jsonReader.CloseInput = false;
    
    var serializer = CreateSerializer();
    return serializer.Deserialize<T>(jsonReader);
}

public async Task SerializeLargeDocument<T>(T document, Stream outputStream)
{
    using var writer = new StreamWriter(outputStream, Encoding.UTF8, 8192, leaveOpen: true);
    using var jsonWriter = new JsonTextWriter(writer);
    
    // Configure for large documents
    jsonWriter.CloseOutput = false;
    jsonWriter.Formatting = Formatting.None; // Minimize size
    
    var serializer = CreateSerializer();
    serializer.Serialize(jsonWriter, document);
    
    await writer.FlushAsync();
}
```

## Testing JSON Serialization

### Round-Trip Serialization Tests

```csharp
[Test]
public void JsonSerialization_Should_HandleRoundTrip()
{
    // Arrange
    var original = new CustomerDocument
    {
        Id = "customer-123",
        Rev = "1-abc",
        Name = "John Doe",
        Email = "john@example.com",
        CreatedDate = DateTime.UtcNow
    };
    
    var serializer = CreateTestSerializer();
    
    // Act - Serialize
    var json = JsonConvert.SerializeObject(original, serializer.Converters.ToArray());
    
    // Act - Deserialize
    var deserialized = JsonConvert.DeserializeObject<CustomerDocument>(json, serializer.Converters.ToArray());
    
    // Assert
    Assert.That(deserialized.Id, Is.EqualTo(original.Id));
    Assert.That(deserialized.Rev, Is.EqualTo(original.Rev));
    Assert.That(deserialized.Name, Is.EqualTo(original.Name));
    Assert.That(deserialized.Email, Is.EqualTo(original.Email));
}

[Test]
public void CamelCaseConversion_Should_ConvertPropertyNames()
{
    // Arrange
    var document = new CustomerDocument { Name = "Test Customer" };
    var settings = new JsonSerializerSettings
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };
    
    // Act
    var json = JsonConvert.SerializeObject(document, settings);
    
    // Assert
    Assert.That(json, Contains.Substring("\"name\":\"Test Customer\""));
    Assert.That(json, Does.Not.Contain("\"Name\":")); // PascalCase should not appear
}
```

### Custom Converter Testing

```csharp
[Test]
public void AttachmentsConverter_Should_HandleCouchDbFormat()
{
    // Arrange
    var couchDbAttachmentsJson = @"{
        ""photo.jpg"": {
            ""content_type"": ""image/jpeg"",
            ""stub"": true
        },
        ""document.pdf"": {
            ""content_type"": ""application/pdf"", 
            ""stub"": true
        }
    }";
    
    var converter = new AttachmentsConverter();
    var serializer = JsonSerializer.Create();
    
    // Act
    using var reader = new JsonTextReader(new StringReader(couchDbAttachmentsJson));
    var attachments = (IList<IAttachmentMetaData>)converter.ReadJson(reader, typeof(IList<IAttachmentMetaData>), null, serializer);
    
    // Assert
    Assert.That(attachments.Count, Is.EqualTo(2));
    Assert.That(attachments[0].Filename, Is.EqualTo("photo.jpg"));
    Assert.That(attachments[0].ContentType, Is.EqualTo("image/jpeg"));
    Assert.That(attachments[1].Filename, Is.EqualTo("document.pdf"));
    Assert.That(attachments[1].ContentType, Is.EqualTo("application/pdf"));
}
```

## Best Practices

1. **Use camel case naming**: Follow JSON/JavaScript conventions
2. **Handle null values appropriately**: Use `NullValueHandling.Ignore` where suitable
3. **Implement custom converters carefully**: Ensure bidirectional conversion works
4. **Use stream-based processing**: For large documents to minimize memory usage
5. **Test serialization round-trips**: Ensure data integrity through serialize/deserialize cycles
6. **Configure type handling**: Use `TypeNameHandling.Objects` for polymorphic scenarios
7. **Validate during serialization**: Catch data issues early in the process
8. **Optimize for performance**: Consider disabling unnecessary features for high-throughput scenarios
9. **Handle serialization errors gracefully**: Implement error handlers for robust processing
10. **Document custom converters**: Provide clear documentation for custom serialization logic

---

*For more information about JSON serialization, see [Integrations Overview](integrations-overview.md), [CouchDB Integration](couchdb-integration.md), and [HTTP Client Integration](http-client-integration.md).*