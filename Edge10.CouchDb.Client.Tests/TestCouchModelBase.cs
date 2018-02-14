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

		[Test]
		public void Clone()
		{
			var testSubject = new TestCouchModel
			{
				Id = "1",
				Rev = "1",
				Field1 = "Field1 value",
				Field2 = new TestCouchModel
				{
					Id = "2",
					Rev = "2",
					Field1 = "Field1 on inner class"
				},
				Field3 = new DateTime(2000, 1, 2, 3, 4, 5, DateTimeKind.Local),
				Deleted = true,
				Attachments =
				{
					new AttachmentMetaData { ContentType = "Content 1", Filename = "File 1" },
					new AttachmentMetaData { ContentType = "Content 2", Filename = "File 2" }
				}
			};

			var cloned = (TestCouchModel)testSubject.Clone();

			Assert.AreNotEqual(testSubject, cloned, "Clone() should have returned a new object");

			Assert.AreNotEqual(testSubject.Id, cloned.Id, "The Id field on the clone should be different to the original");
			Assert.AreNotEqual(testSubject.Rev, cloned.Rev, "The Rev field on the clone should be different to the original");
			Assert.AreEqual(testSubject.Field1, cloned.Field1, "Field1 on the clone should be the same as the original");
			Assert.AreEqual(testSubject.Field3, cloned.Field3, "Field3 on the clone should be the same as the original");
			Assert.IsTrue(cloned.Deleted, "The Deleted flag should have been set");

			var inner1 = testSubject.Field2 as TestCouchModel;
			var inner2 = cloned.Field2 as TestCouchModel;

			Assert.AreEqual(inner1.Id, inner2.Id, "The Id field on the inner object of the clone should be the same as the original");
			Assert.AreEqual(inner1.Rev, inner2.Rev, "The Rev field on the inner object of the clone should be the same as the original");
			Assert.AreEqual(inner1.Field1, inner2.Field1, "Field1 on the inner object of the clone should be the same as the original");

			Assert.AreEqual(0, cloned.Attachments.Count, message: "The attachments should have been removed (they cannot *actually* be cloned as it would require another call to the server to download them)");
			//note: there is no way to test non-stub attachments as AttachmentMetaData only supports Stub=true
		}

		private class TestCouchModel : CouchModelBase
		{
			public string Field1;
			public object Field2;
			public DateTime Field3;
		}
	}
}
