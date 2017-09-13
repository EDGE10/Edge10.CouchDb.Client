using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Edge10.CouchDb.Client.Utils;
using NUnit.Framework;

namespace Edge10.CouchDb.Client.Tests.Utils
{
	[TestFixture]
	public class TestDisposableWrapper
	{
		private string _value;
		private int _disposeActionCount;
		private DisposableWrapper<string> _wrapper;

		[SetUp]
		public void Init()
		{
			_value   = "new value";
			_wrapper = new DisposableWrapper<string>(_value, () => _disposeActionCount++);
		}

		[Test]
		public void Constructor_Throws_Exception_On_Null_Parameters()
		{
			Assert.That(() => new DisposableWrapper<string>(null, null), Throws.ArgumentNullException);

			//ensure a null value is permitted by creating an instance without getting an exceptioon
			new DisposableWrapper<string>(null, () => { });
		}

		[Test]
		public void Value_Returns_Value_Passed_To_Constructor()
		{
			Assert.That(_wrapper.Value, Is.EqualTo(_value));
		}

		[Test]
		public void Dispose_Calls_Action_Passed_To_Constructor()
		{
			Assert.That(_disposeActionCount, Is.Zero, "The Dispose action should not have been called yet");
			_wrapper.Dispose();
			Assert.That(_disposeActionCount, Is.EqualTo(1), "The Dispose action should have been called once");
			_wrapper.Dispose();
			Assert.That(_disposeActionCount, Is.EqualTo(2), "The Dispose action should have been called again");
		}

		[Test]
		public void NoOp_Returns_A_Do_Nothing_Disposable()
		{
			Assert.That(DisposableWrapper<object>.NoOp, Is.Not.Null);
			DisposableWrapper<object>.NoOp.Dispose();
			DisposableWrapper<object>.NoOp.Dispose();
			DisposableWrapper<object>.NoOp.Dispose();

			//no exceptions so pass
		}
	}
}
