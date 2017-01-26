using Edge10.CouchDb.Client.Changes;
using NUnit.Framework;

namespace Edge10.CouchDb.Client.Tests.Changes
{
	[TestFixture]
	public class TestChangesParameters
	{
		[Test]
		public void CreateQueryString_Includes_All_Settings()
		{
			var parameters = new ChangesParameters()
			{
				Descending    = true,
				IncludeDocs   = false,
				Limit         = 99,
				ChangesFilter = "filter/name",
				ChangesSince  = 456
			};

			parameters.AdditionalParameters.Add("add1", "value1");
			parameters.AdditionalParameters.Add("add2", 123);
			parameters.AdditionalParameters.Add("add3", false);
			parameters.AdditionalParameters.Add("add4", null);

			var queryString = parameters.CreateQueryString();
			Assert.IsTrue(queryString.Contains("descending=true"), "Descending was not set");
			Assert.IsTrue(queryString.Contains("include_docs=false"), "IncludeDocs was not set");
			Assert.IsTrue(queryString.Contains("limit=99"), "Limit was not set");
			Assert.IsTrue(queryString.Contains("since=456"), "ChangesSince was not set");
			Assert.IsTrue(queryString.Contains("filter=filter/name"), "ChangesFilter was not set");

			Assert.IsTrue(queryString.Contains("add1=value1"), "AdditionalParameters were not set");
			Assert.IsTrue(queryString.Contains("add2=123"), "AdditionalParameters were not set");
			Assert.IsTrue(queryString.Contains("add3=false"), "AdditionalParameters were not set");
			Assert.IsFalse(queryString.Contains("add4"), "Null AdditionalParameters should be omitted");
		}
	}
}