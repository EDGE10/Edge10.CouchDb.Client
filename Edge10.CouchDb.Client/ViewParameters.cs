using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Edge10.CouchDb.Client.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Edge10.CouchDb.Client
{
	/// <summary>
	/// A class representing the parameters for a CouchDb view.
	/// </summary>
	public class ViewParameters : CouchQueryParametersBase, IViewParameters
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ViewParameters" /> class.
		/// </summary>
		/// <param name="viewName">Name of the view.</param>
		/// <param name="designDocument">The design document.</param>
		public ViewParameters(string viewName, string designDocument)
		{
			viewName.ThrowIfNullOrEmpty(nameof(viewName));
			designDocument.ThrowIfNullOrEmpty(nameof(designDocument));

			ViewName       = viewName;
			DesignDocument = designDocument;
			IncludeDocs    = true;
		}

		/// <summary>
		/// Gets the view name.
		/// </summary>
		public string ViewName { get; set; }

		/// <summary>
		/// Gets the design document.
		/// </summary>
		public string DesignDocument { get; set; }

		/// <summary>
		/// Optionally sets a list name to use when querying view results.
		/// </summary>
		public string ListName { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether result should be grouped or not.
		/// </summary>
		public bool? Group { get; set; }

		/// <summary>
		/// Gets or sets a group level.
		/// </summary>
		public int? GroupLevel { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether results from stale view allowed or not.
		/// </summary>
		public bool? Stale { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether results should be reduced or not.
		/// </summary>
		public bool? Reduce { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether end key should be included or not.
		/// </summary>
		public bool? InclusiveEnd { get; set; }

		/// <summary>
		/// Gets or sets a key.
		/// </summary>
		public KeyParameter Key { get; set; }

		/// <summary>
		/// Gets or sets the keys to be posted to the view.  If specified, these
		/// will override anything specified in the <see cref="P:Key" />, <see cref="P:StartKey" />
		/// or <see cref="P:EndKey" /> properties.
		/// </summary>
		/// <value>
		/// The keys to be posted.
		/// </value>
		public IEnumerable<object> Keys { get; set; }

		/// <summary>
		/// Gets or sets a start key.
		/// </summary>
		public IEnumerable<object> StartKey { get; set; }

		/// <summary>
		/// Gets or sets a end key.
		/// </summary>
		public IEnumerable<object> EndKey { get; set; }

		/// <summary>
		/// Sets the number of results to skip.
		/// </summary>
		public int? Skip { get; set; }

		/// <summary>
		/// Gets any non-default parameters that should be added to the query string
		/// </summary>
		public IDictionary<string, string> QueryStringParameters { get; } = new Dictionary<string, string>();

		/// <summary>
		/// Creates a query string representing these parameters that can be appended
		/// to the view url.
		/// </summary>
		/// <returns>
		/// A query string representing the view parameters.
		/// </returns>
		public override string CreateQueryString()
		{
			var parameters = new Dictionary<string, string>();
			if (Descending.HasValue)
				parameters.Add("descending", Descending.Value.ToString().ToLower());
			if (Limit.HasValue)
				parameters.Add("limit", Limit.Value.ToString());
			if (IncludeDocs.HasValue)
				parameters.Add("include_docs", IncludeDocs.Value.ToString().ToLower());
			if (Group.HasValue)
				parameters.Add("group", Group.Value.ToString().ToLower());
			if (GroupLevel.HasValue)
				parameters.Add("group_level", GroupLevel.Value.ToString());
			if (Stale.GetValueOrDefault())
				parameters.Add("stale", "ok");
			if (Reduce.HasValue)
				parameters.Add("reduce", Reduce.Value.ToString().ToLower());
			if (InclusiveEnd.HasValue)
				parameters.Add("inclusive_end", InclusiveEnd.Value.ToString().ToLower());
			if (Skip.HasValue)
				parameters.Add("skip", Skip.Value.ToString());
			if (Key != null)
				parameters.Add("key", Key.FormattedValue);
			if (StartKey != null && StartKey.Any())
				parameters.Add("startkey", PrepareArrayValue(StartKey));
			if (EndKey != null && EndKey.Any())
				parameters.Add("endkey", PrepareArrayValue(EndKey));

			foreach (var additionalParameter in QueryStringParameters)
				parameters[additionalParameter.Key] = additionalParameter.Value;

			var builder = new StringBuilder();
			foreach (var extraParam in parameters)
			{
				builder.Append(builder.Length == 0 ? "?" : "&");
				builder.Append(extraParam.Key);
				builder.Append("=");
				builder.Append(extraParam.Value);
			}

			return builder.ToString();
		}

		private string PrepareArrayValue(IEnumerable<object> values)
		{
			var valuesArray = values as object[] ?? values.ToArray();
			var value       = JsonConvert.SerializeObject(valuesArray, Formatting.None, new IsoDateTimeConverter());
			return WebUtility.UrlEncode(value);
		}
	}
}