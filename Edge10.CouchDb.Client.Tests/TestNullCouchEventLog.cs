using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Edge10.CouchDb.Client.Utils;
using NUnit.Framework;

namespace Edge10.CouchDb.Client.Tests
{
	[TestFixture]
	public class TestNullCouchEventLog
	{
		[Test]
		public void LogViewEvent_Returns_NoOp_DisposableWrapper()
		{
			Assert.That(NullCouchEventLog.Instance.LogViewEvent(null), Is.EqualTo(DisposableWrapper<object>.NoOp));
		}

		[Test]
		public void LogDocumentEvent_Returns_NoOp_DisposableWrapper()
		{
			Assert.That(NullCouchEventLog.Instance.LogDocumentEvent(null, null), Is.EqualTo(DisposableWrapper<object>.NoOp));
		}
	}
}
