using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Edge10.CouchDb.Client.Utils
{
	/// <summary>
	/// A class containing extension methods to help in throwing exceptions.
	/// </summary>
	[ExcludeFromCodeCoverage]
	internal static class ThrowExtensions
	{
		/// <summary>
		/// Throws an <see cref="ArgumentNullException"/> if <paramref name="target"/> is <c>null</c>.
		/// </summary>
		/// <param name="target">The target object being null checked.</param>
		/// <param name="parameterName">The name of the parameter to be included in the <see cref="ArgumentNullException"/>.</param>
		[DebuggerStepThrough]
		internal static void ThrowIfNull(this object target, string parameterName)
		{
			if (parameterName        == null) throw new ArgumentNullException(nameof(parameterName));
			if (parameterName.Length == 0) throw new ArgumentException("The 'parameterName' parameter cannot be empty");
			if (target               == null) throw new ArgumentNullException(parameterName);
		}

		/// <summary>
		/// Throws an <see cref="ArgumentNullException"/> if <paramref name="target"/> is <c>null</c>,
		/// or throws an <see cref="ArgumentException"/> if <paramref name="target"/> is a zero-length
		/// string.
		/// </summary>
		/// <param name="target">The target string being tested.</param>
		/// <param name="parameterName">The name of the parameter to be included in the <see cref="ArgumentNullException"/> or
		/// <see cref="ArgumentException"/>.</param>
		[DebuggerStepThrough]
		internal static void ThrowIfNullOrEmpty(this string target, string parameterName)
		{
			target.ThrowIfNull(parameterName);

			if (target.Length == 0) throw new ArgumentException($"The '{parameterName}' parameter cannot be empty", parameterName);
		}
	}
}