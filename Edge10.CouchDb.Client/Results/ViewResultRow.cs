using System;
using Newtonsoft.Json;

namespace Edge10.CouchDb.Client.Results
{
	/// <summary>
	/// A class representing a row of data in a view result.
	/// </summary>
	/// <typeparam name="TDocument">The type of the document (only applies when IncludeDocs is true).</typeparam>
	/// <typeparam name="TValue">The type of the value in each row.</typeparam>
	internal class ViewResultRow<TDocument, TValue>
	{
		/// <summary>
		/// Gets or sets the ID.
		/// </summary>
		/// <value>
		/// The ID.
		/// </value>
		[JsonProperty("id")]
		public Guid Id { get; set; }

		/// <summary>
		/// Gets or sets the key that identifies this row.
		/// </summary>
		/// <value>
		/// The key.
		/// </value>
		[JsonProperty("key")]
		public object Key { get; set; }

		/// <summary>
		/// Gets or sets the value.
		/// </summary>
		/// <value>
		/// The value.
		/// </value>
		[JsonProperty("value")]
		public TValue Value { get; set; }

		/// <summary>
		/// Gets or sets the document included in this row (if IncludeDocs is true).
		/// </summary>
		/// <value>
		/// The document.
		/// </value>
		[JsonProperty("doc")]
		public TDocument Document { get; set; }
	}
}