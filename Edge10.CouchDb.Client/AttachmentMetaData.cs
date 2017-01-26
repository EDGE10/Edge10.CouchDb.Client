using Newtonsoft.Json;

namespace Edge10.CouchDb.Client
{
	/// <summary>
	/// A class representing the metadata for a CouchDb attachment.
	/// </summary>
	public class AttachmentMetaData : IAttachmentMetaData
	{
		/// <summary>
		/// Gets or sets the content mime type.
		/// </summary>
		/// <value>
		/// The mime type of the content.
		/// </value>
		[JsonProperty(PropertyName = "content_type")]
		public string ContentType { get; set; }

		/// <summary>
		/// Gets or sets the filename of the attachment.
		/// </summary>
		/// <value>
		/// The attachment filename.
		/// </value>
		[JsonIgnore]
		public string Filename { get; set; }

		/// <summary>
		/// Gets a value indicating whether this <see cref="AttachmentMetaData"/> is a stub.
		/// </summary>
		/// <remarks>
		/// This should always return true, but needs to be included in serialization.
		/// </remarks>
		/// <value>
		///   <c>true</c> if stub; otherwise, <c>false</c>.
		/// </value>
		[JsonProperty("stub")]
		public bool Stub { get; private set; } = true;
	}
}