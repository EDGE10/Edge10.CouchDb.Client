using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edge10.CouchDb.Client.Configuration
{
	/// <summary>
	/// An interface for API objects that provide access to CouchDB configuration.
	/// </summary>
	public interface ICouchConfigurationApi : IDisposable
	{
		/// <summary>
		/// Determines whether the specified user exists.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <returns><c>true</c> if the user exists; otherwise, <c>false</c>.</returns>
		Task<bool> UserExists(string username);


	}
}
