using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Edge10.CouchDb.Client.Exceptions;
using NUnit.Framework;

namespace Edge10.CouchDb.Client.Tests.Exceptions
{
	[TestFixture]
	public class TestConflictException
	{
		[Test]
		public void Constructor_Passes_Message_Down()
		{
			Assert.That(new ConflictException("Oh no!").Message, Is.EqualTo("Oh no!"));
		}

		[Test]
		public void Constructor_Passes_Message_And_Inner_Exception_Down()
		{
			var inner     = new Exception();
			var exception = new ConflictException("Oh no!", inner);

			Assert.That(exception.Message, Is.EqualTo("Oh no!"));
			Assert.That(exception.InnerException, Is.EqualTo(inner));
		}
	}
}
