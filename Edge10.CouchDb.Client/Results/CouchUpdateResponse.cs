using Newtonsoft.Json;

namespace Edge10.CouchDb.Client.Results
{
	/// <summary>
	/// A class representing the response from CouchDb to a PUT or POST.
	/// </summary>
	internal class CouchUpdateResponse
	{
		/// <summary>
		/// Gets or sets the ID of the updated document.
		/// </summary>
		[JsonProperty("id")]
		public string Id { get; set; }

		/// <summary>
		/// Gets or sets the new revision of the updated document.
		/// </summary>
		[JsonProperty("rev")]
		public string Rev { get; set; }
	}
}