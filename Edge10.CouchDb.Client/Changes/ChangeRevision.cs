using Newtonsoft.Json;

namespace Edge10.CouchDb.Client.Changes
{
	/// <summary>
	/// A revision record from the CouchDb _changes feed.
	/// </summary>
	public class ChangeRevision
	{
		/// <summary>
		/// Gets or sets the new revision of the changed document.
		/// </summary>
		[JsonProperty("rev")]
		public string Revision { get; set; }

		/// <summary>
		/// Indicates whether or the document was deleted as part of this change.
		/// </summary>
		[JsonProperty("deleted")]
		public bool Deleted { get; set; }
	}
}