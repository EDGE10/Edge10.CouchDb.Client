using System;
using System.IO;
using NUnit.Framework;

namespace Edge10.CouchDb.Client.Tests
{
	[TestFixture]
	public class TestSerializationStrategy
	{
		[Test]
		public void Constructor_Throws_On_Wrong_Arguments()
		{
			Assert.Throws<ArgumentNullException>(() => new SerializationStrategy(null, x => new StringWriter(x)));
			Assert.Throws<ArgumentNullException>(() => new SerializationStrategy(x => new StreamReader(x), null));
		}
	}
}