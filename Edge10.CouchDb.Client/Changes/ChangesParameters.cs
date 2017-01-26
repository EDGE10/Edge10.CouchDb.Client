using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Edge10.CouchDb.Client.Changes
{
	/// <summary>
	/// Implementaion of <see cref="IChangesParameters"/>, parameters for use with the CouchDB Changes API
	/// </summary>
	public class ChangesParameters : CouchQueryParametersBase, IChangesParameters
	{
		/// <summary>
		/// Gets or sets the include changes filter.
		/// </summary>
		/// <value>
		/// The include changes filter.
		/// </value>
		public string ChangesFilter { get; set; }

		/// <summary>
		/// Gets or sets the changes since filter on the changes api.
		/// </summary>
		/// <value>
		/// The changes since.
		/// </value>
		public int? ChangesSince { get; set; }

		/// <summary>
		/// Gets the additional request parameters to be supplied to the filter.
		/// </summary>
		public IDictionary<string, object> AdditionalParameters { get; } = new Dictionary<string, object>();

		/// <summary>
		/// Creates a query string representing these parameters.
		/// </summary>
		/// <returns>
		/// The generated query string.
		/// </returns>
		public override string CreateQueryString()
		{
			var builder = new StringBuilder();

			AppendQueryStringElement(builder, "filter", ChangesFilter);
			AppendQueryStringElement(builder, "since", ChangesSince);
			AppendQueryStringElement(builder, "limit", Limit);
			AppendQueryStringElement(builder, "descending", Descending);
			AppendQueryStringElement(builder, "include_docs", IncludeDocs);

			foreach (var item in AdditionalParameters)
				AppendQueryStringElement(builder, item.Key, item.Value);

			return builder.ToString();
		}

		private static void AppendQueryStringElement<T>(StringBuilder builder, string name, T value)
		{
			if (value != null)
			{
				builder.Append(builder.Length == 0 ? "?" : "&");
				builder.AppendFormat(CultureInfo.InvariantCulture, "{0}={1}", name, value.ToString().ToLowerInvariant());
			}
		}
	}
}