using NUnit.Framework;

namespace Edge10.CouchDb.Client.Tests
{
	[TestFixture]
	public class TestViewParameters
	{
		public void CreateQuerySting_Check_Http_Params_Formatting()
		{
			var parameters = new ViewParameters("view", "designdoc")
			{
				Descending = true,
				Limit      = 123
			};

			parameters.QueryStringParameters.Add("custom", "123");

			var queryString = parameters.CreateQueryString();
			Assert.AreEqual("?descending=true&limit=123", queryString);
		}

		[Test]
		public void CreateQueryString_Includes_All_Settings()
		{
			var parameters = new ViewParameters("view", "designdoc")
			{
				Descending   = true,
				EndKey       = new[] { "one", "two" },
				Group        = true,
				GroupLevel   = 10,
				IncludeDocs  = false,
				InclusiveEnd = false,
				Key          = new[] { "ten", "eleven" },
				Limit        = 99,
				Reduce       = false,
				Skip         = 123,
				Stale        = true,
				StartKey     = new[] { "three", "four" },
				Keys         = new object[] { "one", 2, "three" }
			};

			parameters.QueryStringParameters.Add("custom", "123");

			var queryString = parameters.CreateQueryString();
			Assert.IsTrue(queryString.Contains("descending=true"), "Descending was not set");
			Assert.IsTrue(queryString.Contains("endkey=%5B%22one%22%2C%22two%22%5D"), "EndKey was not set");
			Assert.IsTrue(queryString.Contains("group=true"), "Group was not set");
			Assert.IsTrue(queryString.Contains("group_level=10"), "GroupLevel was not set");
			Assert.IsTrue(queryString.Contains("include_docs=false"), "IncludeDocs was not set");
			Assert.IsTrue(queryString.Contains("inclusive_end=false"), "InclusiveEnd was not set");
			Assert.IsTrue(queryString.Contains("key=%5B%22ten%22%2C%22eleven%22%5D"), "Key was not set");
			Assert.IsTrue(queryString.Contains("limit=99"), "Limit was not set");
			Assert.IsTrue(queryString.Contains("reduce=false"), "Reduce was not set");
			Assert.IsTrue(queryString.Contains("skip=123"), "Skip was not set");
			Assert.IsTrue(queryString.Contains("stale=ok"), "Stale was not set");
			Assert.IsTrue(queryString.Contains("startkey=%5B%22three%22%2C%22four%22%5D"), "StartKey was not set");
			Assert.IsTrue(queryString.Contains("custom=123"), "Custom parameters were not inserted");
			Assert.IsFalse(queryString.Contains("keys"), "Keys property should not be included in the query string");
		}

		[Test]
		public void CreateQueryString_Supports_Single_Key_Item()
		{
			var parameters = new ViewParameters("view", "designdoc")
			{
				Key = "ten"
			};

			parameters.QueryStringParameters.Add("custom", "123");

			var queryString = parameters.CreateQueryString();
			Assert.That(queryString, Does.Contain("key=%22ten%22"), "Key was not set");
		}

		[Test]
		public void CreateQueryString_Ignores_Empty_Array_For_Key()
		{
			var parameters = new ViewParameters("view", "designdoc")
			{
				Key = new object[0]
			};

			parameters.QueryStringParameters.Add("custom", "123");

			var queryString = parameters.CreateQueryString();
			Assert.That(queryString, Does.Not.Contain("key"), "Key was set");
		}

		[Test]
		public void CreateQueryString_Builds_QueryString_From_Only_Custom_Parameters()
		{
			var parameters = new ViewParameters("view", "design");
			parameters.QueryStringParameters.Add("custom1", "one");
			parameters.QueryStringParameters.Add("custom2", "two");
			parameters.IncludeDocs = null; //remove default value

			Assert.AreEqual("?custom1=one&custom2=two", parameters.CreateQueryString());
		}
	}
}