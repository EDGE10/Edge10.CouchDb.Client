using System.Collections.Generic;
using Newtonsoft.Json;

namespace Edge10.CouchDb.Client.Results
{
	/// <summary>
	/// A class representing a deserialized view result from CouchDb
	/// </summary>
	/// <typeparam name="TDocument">The type of the document (only applies when IncludeDocs is true).</typeparam>
	/// <typeparam name="TValue">The type of the value in each row.</typeparam>
	internal class ViewResult<TDocument, TValue>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ViewResult{TValue}" /> class.
		/// </summary>
		public ViewResult()
		{
			this.Rows = new List<ViewResultRow<TDocument, TValue>>();
		}

		/// <summary>
		/// Gets or sets the total rows.
		/// </summary>
		/// <value>
		/// The total rows.
		/// </value>
		[JsonProperty("total_rows")]
		public long TotalRows { get; set; }

		/// <summary>
		/// Gets or sets the offset.
		/// </summary>
		/// <value>
		/// The offset.
		/// </value>
		[JsonProperty("offset")]
		public long Offset { get; set; }

		/// <summary>
		/// Gets the rows of data.
		/// </summary>
		[JsonProperty("rows")]
		public IEnumerable<ViewResultRow<TDocument, TValue>> Rows { get; private set; }
	}
}