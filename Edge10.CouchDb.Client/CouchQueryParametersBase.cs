namespace Edge10.CouchDb.Client
{
	/// <summary>
	/// Represents the parameters shared between view and _changes parameters.
	/// </summary>
	public abstract class CouchQueryParametersBase : ICouchQueryParameters
	{
		/// <summary>
		/// Determines whether or not the results should be descending.
		/// </summary>
		public bool? Descending { get; set; }

		/// <summary>
		/// Sets the maximum number of results.
		/// </summary>
		public int? Limit { get; set; }

		/// <summary>
		/// Determines whether or not the results should include the full document content.
		/// </summary>
		public bool? IncludeDocs { get; set; }

		/// <summary>
		/// Creates a query string representing these parameters.
		/// </summary>
		/// <returns>
		/// The generated query string.
		/// </returns>
		public abstract string CreateQueryString();
	}
}