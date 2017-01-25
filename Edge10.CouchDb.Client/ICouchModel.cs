using System;
using System.Collections.Generic;

namespace Edge10.CouchDb.Client
{
	/// <summary>
	/// An interface for model classes stored in CouchDB.
	/// </summary>
	public interface ICouchModel
	{
		/// <summary>
		/// The CouchDB ID for this document.  This will be converted to a <see cref="string"/>
		/// for DB storage.
		/// </summary>
		Guid Id { get; set; }

		/// <summary>
		/// The CouchDB Revision number for this document.  This will be populated from the
		/// server automatically for existing documents, and must be set in order to allow
		/// deletion and saving.
		/// </summary>
		string Rev { get; set; }

		/// <summary>
		/// Gets a list of the metadata for each attachment associated with this document.
		/// </summary>
		IList<IAttachmentMetaData> Attachments { get; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="ICouchModel"/> is deleted.
		/// </summary>
		/// <value>
		///   <c>true</c> if this document has been deleted; otherwise, <c>false</c>.
		/// </value>
		bool Deleted { get; set; }

		/// <summary>
		/// Gets a value indicating whether this instance has not yet been saved to the database.
		/// </summary>
		/// <value>
		///   <c>true</c> if this instance has not yet been saved to the database; otherwise, <c>false</c>.
		/// </value>
		bool IsNew { get; }

		/// <summary>
		/// Gets or sets the type property in CouchDb.
		/// </summary>
		string Type { get; set; }
	}
}