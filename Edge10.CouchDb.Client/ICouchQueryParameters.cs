namespace Edge10.CouchDb.Client
{
	/// <summary>
	/// Represents the parameters shared between view and _changes parameters.
	/// </summary>
	public interface ICouchQueryParameters
	{
		/// <summary>
		/// Determines whether or not the results should be descending.
		/// </summary>
		bool? Descending { get; set; }

		/// <summary>
		/// Sets the maximum number of results.
		/// </summary>
		int? Limit { get; set; }

		/// <summary>
		/// Determines whether or not the results should include the full document content.
		/// </summary>
		bool? IncludeDocs { get; set; }

		/// <summary>
		/// Creates a query string representing these parameters.
		/// </summary>
		/// <returns>The generated query string.</returns>
		string CreateQueryString();
	}
}