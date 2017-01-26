using System.Collections.Generic;
using Newtonsoft.Json;

namespace Edge10.CouchDb.Client.Changes
{
	/// <summary>
	/// The results of a query to the CouchDb _changes feed.
	/// </summary>
	/// <typeparam name="TDocument">The type of the changed document.</typeparam>
	public class ChangesResult<TDocument>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ChangesResult"/> class.
		/// </summary>
		public ChangesResult()
		{
			Results = new List<Change<TDocument>>();
		}

		/// <summary>
		/// Gets the results of the _changes query.
		/// </summary>
		[JsonProperty("results")]
		public List<Change<TDocument>> Results { get; private set; }

		/// <summary>
		/// Gets or sets the last sequence number or ID in the database.
		/// </summary>
		[JsonProperty("last_seq")]
		public string LastSequence { get; set; }
	}

	/// <summary>
	/// The results of a query to the CouchDb _changes feed.
	/// </summary>
	public class ChangesResult : ChangesResult<dynamic>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ChangesResult"/> class.
		/// </summary>
		public ChangesResult()
		{
			this.Results = new List<Change>();
		}

		/// <summary>
		/// Gets the results of the _changes query.
		/// </summary>
		[JsonProperty("results")]
		public new List<Change> Results { get; private set; }
	}
}