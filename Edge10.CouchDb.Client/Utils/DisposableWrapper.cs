using System;

namespace Edge10.CouchDb.Client.Utils
{
	internal class DisposableWrapper<T> : IDisposable
	{
		private readonly Action _dispose;

		public DisposableWrapper(T value, Action dispose)
		{
			dispose.ThrowIfNull("dispose");

			_dispose = dispose;
			Value    = value;
		}

		public T Value { get; private set; }

		/// <summary>
		/// A "do nothing" instance.
		/// </summary>
		public static IDisposable NoOp { get; } = new DisposableWrapper<T>(default(T), () => { });

		public void Dispose()
		{
			_dispose();
		}
	}
}