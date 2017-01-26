namespace Edge10.CouchDb.Client
{
	/// <summary>
	/// An interface for objects that contain metadata about an attachment on a
	/// CouchDb document.
	/// </summary>
	public interface IAttachmentMetaData
	{
		/// <summary>
		/// Gets or sets the content mime type.
		/// </summary>
		/// <value>
		/// The mime type of the content.
		/// </value>
		string ContentType { get; set; }

		/// <summary>
		/// Gets or sets the filename of the attachment.
		/// </summary>
		/// <value>
		/// The attachment filename.
		/// </value>
		string Filename { get; set; }

		/// <summary>
		/// Gets a value indicating whether this <see cref="IAttachmentMetaData" /> is stub.
		/// </summary>
		/// <value>
		///   <c>true</c> if stub; otherwise, <c>false</c>.
		/// </value>
		bool Stub { get; }
	}
}