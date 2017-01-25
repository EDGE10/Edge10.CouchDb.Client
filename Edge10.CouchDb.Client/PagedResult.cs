using System.Collections.Generic;
using Edge10.CouchDb.Client.Utils;

namespace Edge10.CouchDb.Client
{
	/// <summary>
	/// A class representing a paged result of Couch data.
	/// </summary>
	public class PagedResult<T> : IPagedResult<T>
	{
		/// <summary>
		/// Initializes new instance of <see cref="PagedResult{T}"/>
		/// </summary>
		/// <param name="rows">The rows.</param>
		/// <param name="totalRows">Total rows count.</param>
		public PagedResult(IEnumerable<T> rows, long totalRows)
		{
			rows.ThrowIfNull(nameof(rows));

			Rows = rows;
			TotalRows = totalRows;
		}

		/// <summary>
		/// Gets result rows.
		/// </summary>
		public IEnumerable<T> Rows { get; }

		/// <summary>
		/// Gets total number of rows.
		/// </summary>
		public long TotalRows { get; }
	}
}