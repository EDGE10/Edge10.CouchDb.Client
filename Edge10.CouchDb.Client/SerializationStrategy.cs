using System;
using System.IO;
using System.Text;
using Edge10.CouchDb.Client.Utils;

namespace Edge10.CouchDb.Client
{
	/// <summary>
	/// A class representing the serialization stragy.
	/// </summary>
	public class SerializationStrategy
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SerializationStrategy" /> class.
		/// </summary>
		/// <param name="readerFactory">The reader factory.</param>
		/// <param name="writerFactory">The writer factory.</param>
		public SerializationStrategy(Func<Stream, StreamReader> readerFactory, Func<StringBuilder, StringWriter> writerFactory)
		{
			readerFactory.ThrowIfNull(nameof(readerFactory));
			writerFactory.ThrowIfNull(nameof(writerFactory));

			ReaderFactory = readerFactory;
			WriterFactory = writerFactory;
		}

		/// <summary>
		/// Gets the reader factory which returns reader with some customisations.
		/// </summary>
		public Func<Stream, StreamReader> ReaderFactory { get; }

		/// <summary>
		/// Gets the writer factory which returns writer with some customisations.
		/// </summary>
		public Func<StringBuilder, StringWriter> WriterFactory { get; }
	}
}