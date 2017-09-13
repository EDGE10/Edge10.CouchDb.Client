using System.Collections.Generic;

namespace Edge10.CouchDb.Client
{
	/// <summary>
	/// An interface for classes representing the parameters for a CouchDb view.
	/// </summary>
	public interface IViewParameters : ICouchQueryParameters
	{
		/// <summary>
		/// Gets the view name.
		/// </summary>
		string ViewName { get; }

		/// <summary>
		/// Gets the design document.
		/// </summary>
		string DesignDocument { get; }

		/// <summary>
		/// Optionally sets a list name to use when querying view results.
		/// </summary>
		string ListName { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether result should be grouped or not.
		/// </summary>
		bool? Group { get; set; }

		/// <summary>
		/// Gets or sets a group level.
		/// </summary>
		int? GroupLevel { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether results from stale view allowed or not.
		/// </summary>
		bool? Stale { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether results should be reduced or not.
		/// </summary>
		bool? Reduce { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether end key should be included or not.
		/// </summary>
		bool? InclusiveEnd { get; set; }

		/// <summary>
		/// Gets or sets a key.
		/// </summary>
		KeyParameter Key { get; set; }

		/// <summary>
		/// Gets or sets a start key.
		/// </summary>
		IEnumerable<object> StartKey { get; set; }

		/// <summary>
		/// Gets or sets a end key.
		/// </summary>
		IEnumerable<object> EndKey { get; set; }

		/// <summary>
		/// Gets or sets the keys to be posted to the view.  If specified, these
		/// will override anything specified in the <see cref="P:Key"/>, <see cref="P:StartKey"/>
		/// or <see cref="P:EndKey"/> properties.
		/// </summary>
		/// <value>
		/// The keys to be posted.
		/// </value>
		IEnumerable<object> Keys { get; set; }

		/// <summary>
		/// Sets the number of results to skip.
		/// </summary>
		int? Skip { get; set; }

		/// <summary>
		/// Gets any non-default parameters that should be added to the query string
		/// </summary>
		IDictionary<string, string> QueryStringParameters { get; }
	}
}