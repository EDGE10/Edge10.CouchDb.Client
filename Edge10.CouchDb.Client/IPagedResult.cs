using System.Collections.Generic;

namespace Edge10.CouchDb.Client
{
	/// <summary>
	/// An interface representing a paged result of Couch data.
	/// </summary>
	public interface IPagedResult<out T>
	{
		/// <summary>
		/// Gets result rows.
		/// </summary>
		IEnumerable<T> Rows { get; }

		/// <summary>
		/// Gets total number of rows.
		/// </summary>
		long TotalRows { get; }
	}
}