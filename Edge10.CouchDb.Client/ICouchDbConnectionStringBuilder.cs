namespace Edge10.CouchDb.Client
{
	/// <summary>
	/// An interface for couch db connection string builder.
	/// </summary>
	public interface ICouchDbConnectionStringBuilder
	{
		/// <summary>
		/// Gets or sets a raw connection string.
		/// </summary>
		string ConnectionString { get; set; }

		/// <summary>
		/// Gets or sets a database name.
		/// </summary>
		string DatabaseName { get; set; }

		/// <summary>
		/// Gets or sets a server.
		/// </summary>
		string Server { get; set; }

		/// <summary>
		/// Gets or sets a server port.
		/// </summary>
		int? Port { get; set; }

		/// <summary>
		/// Gets or sets an user name.
		/// </summary>
		string User { get; set; }

		/// <summary>
		/// Gets or sets a password.
		/// </summary>
		string Password { get; set; }
	}
}