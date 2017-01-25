using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Edge10.CouchDb.Client.Utils
{
	/// <summary>
	/// Contains extensions for the <see cref="HttpResponseMessage"/> type.
	/// </summary>
	internal static class HttpResponseMessageExtensions
	{
		/// <summary>
		/// Throws a more useful error than the built in EnsureSuccessStatusCode method by
		/// including the message content.
		/// </summary>
		/// <param name="message">The message to be checked.</param>
		/// <returns></returns>
		/// <exception cref="System.Net.Http.HttpRequestException">If the message has a non-success status code</exception>
		public static Task ThrowErrorIfNotSuccess(this HttpResponseMessage message)
		{
			return message.ThrowErrorIfNotSuccess((_, content) => new HttpRequestException($"{(int)message.StatusCode} {message.ReasonPhrase} {content}"));
		}

		/// <summary>
		/// Throws a more useful error than the built in EnsureSuccessStatusCode method by
		/// including the message content.
		/// </summary>
		/// <param name="message">The message to be checked.</param>
		/// <param name="exceptionFactory">Creates the exception to be thrown.</param>
		/// <returns></returns>
		/// <exception cref="Exception">The exception created by <paramref name="exceptionFactory" />, thrown if the message has a non-success status code</exception>
		public static async Task ThrowErrorIfNotSuccess(this HttpResponseMessage message, Func<HttpResponseMessage, string, Exception> exceptionFactory)
		{
			message.ThrowIfNull(nameof(message));
			exceptionFactory.ThrowIfNull(nameof(exceptionFactory));

			if (message.IsSuccessStatusCode) return;

			var content = string.Empty;

			if (message.Content != null)
			{
				content = await message.Content.ReadAsStringAsync();
				message.Content.Dispose();
			}

			throw exceptionFactory(message, content);
		}
	}
}