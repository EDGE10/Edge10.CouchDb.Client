using System.Collections.Generic;

namespace Edge10.CouchDb.Client.Changes
{
	/// <summary>
	/// Interface describing the methods/properties to be used for parameter class
	/// </summary>
	public interface IChangesParameters : ICouchQueryParameters
	{
		/// <summary>
		/// Gets or sets the name of the filter to be used
		/// </summary>
		string ChangesFilter { get; set; }

		/// <summary>
		/// Gets or sets the sequence number from which to get changes.
		/// </summary>
		int? ChangesSince { get; set; }

		/// <summary>
		/// Gets the additional request parameters to be supplied to the filter.
		/// </summary>
		IDictionary<string, object> AdditionalParameters { get; }
	}
}