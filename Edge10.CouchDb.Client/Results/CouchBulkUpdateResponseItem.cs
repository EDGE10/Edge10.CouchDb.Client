using System;

namespace Edge10.CouchDb.Client.Results
{
	/// <summary>
	/// A class representing the response item from CouchDb to the bulk update.
	/// </summary>
	internal class CouchBulkUpdateResponseItem
	{
		/// <summary>
		/// Gets or sets the id of the updated document.
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// Gets or sets whether the update was successful or not.
		/// </summary>
		public bool Ok { get; set; }

		/// <summary>
		/// Gets or sets the new revision of the updated document.
		/// </summary>
		public string Rev { get; set; }

		/// <summary>
		/// Gets or sets the error type.
		/// </summary>
		public string Error { get; set; }

		/// <summary>
		/// Gets or sets the reason for the error.
		/// </summary>
		public string Reason { get; set; }
	}
}