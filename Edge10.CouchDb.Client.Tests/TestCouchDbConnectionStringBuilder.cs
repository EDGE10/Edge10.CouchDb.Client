using NUnit.Framework;

namespace Edge10.CouchDb.Client.Tests
{
	[TestFixture]
	public class TestCouchDbConnectionStringBuilder
	{
		private CouchDbConnectionStringBuilder _connectionStringBuilder;

		[SetUp]
		public void Init()
		{
			_connectionStringBuilder = new CouchDbConnectionStringBuilder();
		}

		[Test]
		public void Test_DatabaseName_SetsLowerCase()
		{
			_connectionStringBuilder.DatabaseName = "SomethingWithCapitals";

			Assert.AreEqual("somethingwithcapitals", _connectionStringBuilder.DatabaseName, "Database name should be lower case");
		}

		[Test]
		public void Test_DatabaseName_Null()
		{
			_connectionStringBuilder.DatabaseName = null;
			Assert.IsNull(_connectionStringBuilder.DatabaseName, "Should be able to return null from database name");
		}

		[Test]
		public void Test_Port_Not_Numeric()
		{
			_connectionStringBuilder.ConnectionString = "port=ten";
			Assert.That(_connectionStringBuilder.Port, Is.Null, "Port should be null when not numeric");
		}

		[Test]
		public void Test_Properties_SetFromConnectionString()
		{
			_connectionStringBuilder.ConnectionString = "seRvEr=servername;pOrt=456;User=my user;Password=A Password;DatabaseName=db;Unknown=whatever";

			Assert.AreEqual("servername", _connectionStringBuilder.Server);
			Assert.AreEqual(456, _connectionStringBuilder.Port);
			Assert.AreEqual("my user", _connectionStringBuilder.User);
			Assert.AreEqual("A Password", _connectionStringBuilder.Password);
			Assert.AreEqual("db", _connectionStringBuilder.DatabaseName);
		}

		[Test]
		public void Test_Properties_BuildConnectionString()
		{
			_connectionStringBuilder.Server = "my server";
			_connectionStringBuilder.Port = 789;
			_connectionStringBuilder.User = "user123";
			_connectionStringBuilder.Password = "123abc";
			_connectionStringBuilder.DatabaseName = "db";

			var expected = "Server=\"my server\";Port=789;User=user123;Password=123abc;DatabaseName=db";
			Assert.AreEqual(expected, _connectionStringBuilder.ConnectionString);
		}
	}
}
