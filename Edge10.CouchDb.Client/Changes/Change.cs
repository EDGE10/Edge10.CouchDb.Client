using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Edge10.CouchDb.Client.Changes
{
	/// <summary>
	/// A single change record from the CouchDb _changes feed.
	/// </summary>
	/// <typeparam name="TDocument">The type of the changed document.</typeparam>
	public class Change<TDocument>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Change"/> class.
		/// </summary>
		public Change()
		{
			this.Changes = new List<ChangeRevision>();
		}

		/// <summary>
		/// Gets or sets the unique identifier of the changed document.
		/// </summary>
		[JsonProperty("id")]
		public string Id { get; set; }

		/// <summary>
		/// Gets a list of changed revisions.
		/// </summary>
		[JsonProperty("changes")]
		public List<ChangeRevision> Changes { get; private set; }

		/// <summary>
		/// Indicates whether or the document was deleted as part of this change.
		/// </summary>
		[JsonProperty("deleted")]
		public bool Deleted { get; set; }

		/// <summary>
		/// Gets or sets the sequence number or ID associated with this change.
		/// </summary>
		[JsonProperty("seq")]
		public string Sequence { get; set; }

		/// <summary>
		/// Gets the first revision from the <see cref="P:Changes"/> collection.
		/// </summary>
		public string Revision
		{
			get { return this.Changes.Select(c => c.Revision).FirstOrDefault(); }
		}

		/// <summary>
		/// Gets or sets the changed document, if retrieved.
		/// </summary>
		[JsonProperty("doc")]
		public TDocument Document { get; set; }

		/// <summary>
		/// Method to prevent the <see cref="P:Changes"/> property being serialized unnecessarily by Json.Net.
		/// The <see cref="P:Revision"/> will be serialized in it's place.
		/// </summary>
		/// <returns></returns>
		public bool ShouldSerializeChanges()
		{
			return false;
		}
	}

	/// <summary>
	/// A single change record from the CouchDb _changes feed.
	/// </summary>
	public class Change : Change<dynamic>
	{ }
}