using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Edge10.CouchDb.Client.Tests
{
	[TestFixture]
	public class TestAttachmentMetaData
	{
		private AttachmentMetaData _metaData;

		[SetUp]
		public void Init()
		{
			_metaData = new AttachmentMetaData();
		}

		[Test]
		public void Stub_Returns_True()
		{
			Assert.That(_metaData.Stub);
		}
	}
}
