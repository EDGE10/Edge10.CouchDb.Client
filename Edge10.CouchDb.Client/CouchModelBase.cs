using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Edge10.CouchDb.Client
{
	/// <summary>
	/// A base class for <see cref="ICouchModel"/> instances.
	/// </summary>
	public abstract class CouchModelBase : ICouchModel
	{
		/// <summary>
		/// The CouchDB ID for this document.
		/// </summary>
		[JsonProperty(PropertyName = "_id")]
		public string Id { get; set; }

		/// <summary>
		/// The CouchDB Revision number for this document.  This will be populated from the
		/// server automatically for existing documents, and must be set in order to allow
		/// deletion and saving.TestCouchRepository
		/// </summary>
		[JsonProperty(PropertyName = "_rev", NullValueHandling = NullValueHandling.Ignore)]
		public string Rev { get; set; }

		/// <summary>
		/// Gets a list of the metadata for each attachment associated with this document.
		/// </summary>
		[JsonProperty(PropertyName = "_attachments")]
		[JsonConverter(typeof(AttachmentsConverter))]
		public IList<IAttachmentMetaData> Attachments { get; } = new List<IAttachmentMetaData>();

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="ICouchModel"/> is deleted.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this document has been deleted; otherwise, <c>false</c>.
		/// </value>
		[JsonProperty(PropertyName = "_deleted")]
		public bool Deleted { get; set; }

		/// <summary>
		/// Gets a value indicating whether this instance has not yet been saved to the database.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance has not yet been saved to the database; otherwise, <c>false</c>.
		/// </value>
		[JsonIgnore]
		public bool IsNew => string.IsNullOrWhiteSpace(Rev);

		/// <summary>
		/// Gets or sets the type property in CouchDb.
		/// </summary>
		[JsonProperty("type")]
		public string Type { get; set; }

		/// <summary>
		/// Creates a new object that is a copy of the current instance.
		/// </summary>
		/// <returns>
		/// A new object that is a copy of this instance.
		/// </returns>
		public object Clone()
		{
			var serializer = new JsonSerializer
			{
				TypeNameHandling = TypeNameHandling.Objects
			};

			var newObject = (ICouchModel)JToken.FromObject(this, serializer).ToObject(this.GetType(), serializer);
			newObject.Id  = Guid.NewGuid().ToString();
			newObject.Rev = null;

			//remove any stub attachments - we can't get the content from the server so we can't clone them.
			//note: AttachmentMetaData only supports Stub=true attachments so there is no need to filter
			//the attachments collection until inline attachments are supported
			newObject.Attachments.Clear();

			return newObject;
		}
	}
}