using System;
using System.Data.Common;

namespace Edge10.CouchDb.Client
{
	/// <summary>
	/// An implementation of <see cref="ICouchDbConnectionStringBuilder" /> that provides string typed access to connection string properties.
	/// </summary>
	public class CouchDbConnectionStringBuilder : DbConnectionStringBuilder, ICouchDbConnectionStringBuilder
	{
		/// <summary>
		/// Gets or sets a server.
		/// </summary>
		public string Server
		{
			get { return ContainsKey("Server") ? (string)base["Server"] : null; }
			set { base["Server"] = value; }
		}

		/// <summary>
		/// Gets or sets a server port.
		/// </summary>
		public int? Port
		{
			get
			{
				int value;
				if (!ContainsKey("Port") ||
					!Int32.TryParse((string)base["Port"], out value))
				{
					return null;
				}
				return value;
			}
			set { base["Port"] = value; }
		}

		/// <summary>
		/// Gets or sets an user name.
		/// </summary>
		public string User
		{
			get { return ContainsKey("User") ? (string)base["User"] : null; }
			set { base["User"] = value; }
		}

		/// <summary>
		/// Gets or sets a password.
		/// </summary>
		public string Password
		{
			get { return ContainsKey("Password") ? (string)base["Password"] : null; ; }
			set { base["Password"] = value; }
		}

		/// <summary>
		/// Gets or sets a database name.
		/// </summary>
		public string DatabaseName
		{
			get { return ContainsKey("DatabaseName") ? ((string)base["DatabaseName"]).ToLower() : null; ; }
			set { base["DatabaseName"] = value; }
		}
	}
}