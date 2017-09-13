using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Edge10.CouchDb.Client.Tests
{
	[TestFixture]
	public class TestCouchModelBase
	{
		private TestCouchModel _model;

		[SetUp]
		public void Init()
		{
			_model = new TestCouchModel();
		}

		[Test]
		public void IsNew_Returns_True_If_No_Rev()
		{
			Assert.That(_model.IsNew);

			_model.Rev = string.Empty;
			Assert.That(_model.IsNew);

			_model.Rev = " ";
			Assert.That(_model.IsNew);

			_model.Rev = "Rev1";
			Assert.That(_model.IsNew, Is.False);
		}

		private class TestCouchModel : CouchModelBase { }
	}
}
