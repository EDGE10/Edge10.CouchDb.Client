using System;
using System.Diagnostics.CodeAnalysis;

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

		public void Dispose()
		{
			_dispose();
		}
	}
}