using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Edge10.CouchDb.Client.Changes;
using NUnit.Framework;

namespace Edge10.CouchDb.Client.Tests.Changes
{
	[TestFixture]
	public class TestChange
	{
		private Change _change;

		[SetUp]
		public void Init()
		{
			_change = new Change();
		}

		[Test]
		public void Changes_Is_Initially_Empty()
		{
			Assert.That(_change.Changes, Is.Empty);
		}

		[Test]
		public void Revision_Returns_Null_If_No_Changes()
		{
			Assert.That(_change.Revision, Is.Null);
		}

		[Test]
		public void Revision_Returns_First_Change_Revision()
		{
			_change.Changes.Add(new ChangeRevision { Revision = "rev1" });
			_change.Changes.Add(new ChangeRevision { Revision = "rev2" });

			Assert.That(_change.Revision, Is.EqualTo("rev1"));
		}

		[Test]
		public void Should_Not_Serialise_Changes()
		{
			Assert.That(_change.ShouldSerializeChanges(), Is.False);
		}
	}
}
