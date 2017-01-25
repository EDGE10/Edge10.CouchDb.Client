using System;
using System.Collections.Generic;
using System.IO;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Edge10.CouchDb.Client.Tests
{
	[TestFixture]
	public class TestAttachmentsConverter
	{
		private AttachmentsConverter _converter;

		[SetUp]
		public void Init()
		{
			_converter = new AttachmentsConverter();
		}

		[Test]
		public void CanConvert_Always_Returns_True()
		{
			Assert.IsTrue(_converter.CanConvert(null), "CanConvert should always return true");
			Assert.IsTrue(_converter.CanConvert(typeof(object)), "CanConvert should always return true");
			Assert.IsTrue(_converter.CanConvert(typeof(IList<IAttachmentMetaData>)), "CanConvert should always return true");
		}

		[Test]
		public void ReadJson_Throws_Exceptions_On_Null_Reader_Or_Serializer()
		{
			var reader = new Mock<JsonReader>();
			var serializer = new Mock<JsonSerializer>();
			Assert.Throws<ArgumentNullException>(() => _converter.ReadJson(null, null, null, serializer.Object));
			Assert.Throws<ArgumentNullException>(() => _converter.ReadJson(reader.Object, null, null, null));
		}

		[Test]
		public void ReadJson_Creates_Attachments_List()
		{
			//create a dummy attachments object
			var reader = JObject.Parse(@"{
	""file1"" : { ""content_type"" : ""text/plain"" },
	""file2"" : { ""content_type"" : ""text/html"" }
}").CreateReader();

			//deserialize the attachments
			var serializer = new JsonSerializer();
			var converted = _converter.ReadJson(reader, null, null, serializer) as IList<IAttachmentMetaData>;

			//check the deserialized structure
			Assert.IsNotNull(converted, "The result should be a list of attachment metadata");
			Assert.AreEqual(2, converted.Count, "2 attachments should have been added");
			Assert.AreEqual("file1", converted[0].Filename, "The attachment data was not parsed correctly");
			Assert.AreEqual("text/plain", converted[0].ContentType, "The attachment data was not parsed correctly");
			Assert.AreEqual("file2", converted[1].Filename, "The attachment data was not parsed correctly");
			Assert.AreEqual("text/html", converted[1].ContentType, "The attachment data was not parsed correctly");
		}

		[Test]
		public void ReadJson_Updates_Existing_List_If_Specified()
		{
			//create an existing list object
			var existingList = new List<IAttachmentMetaData>();

			//create a dummy attachments object
			var reader = JObject.Parse(@"{
	""file1"" : { ""content_type"" : ""text/plain"" },
	""file2"" : { ""content_type"" : ""text/html"" }
}").CreateReader();

			//deserialize the attachments
			var serializer = new JsonSerializer();
			var converted = _converter.ReadJson(reader, null, existingList, serializer) as IList<IAttachmentMetaData>;

			//check the deserialized structure
			Assert.AreEqual(existingList, converted, "ReadJson should return the existing list, if specified");
			Assert.AreEqual(2, existingList.Count, "2 attachments should have been added");
			Assert.AreEqual("file1", existingList[0].Filename, "The attachment data was not parsed correctly");
			Assert.AreEqual("text/plain", existingList[0].ContentType, "The attachment data was not parsed correctly");
			Assert.AreEqual("file2", existingList[1].Filename, "The attachment data was not parsed correctly");
			Assert.AreEqual("text/html", existingList[1].ContentType, "The attachment data was not parsed correctly");
		}

		[Test]
		public void WriteJson_Throws_Exception_On_Null_Writer_Or_Serializer()
		{
			var reader = new Mock<JsonWriter>();
			var serializer = new Mock<JsonSerializer>();
			Assert.Throws<ArgumentNullException>(() => _converter.WriteJson(null, null, serializer.Object));
			Assert.Throws<ArgumentNullException>(() => _converter.WriteJson(reader.Object, null, null));
		}

		[Test]
		[TestCase(null)]
		[TestCase("not a list")]
		[TestCase(123)]
		public void WriteJson_Returns_Empty_Object_When_Passed_Null_Or_Invalid_Value(object value)
		{
			//create a writer and serializer
			var textWriter = new StringWriter();
			var writer = new JsonTextWriter(textWriter);
			var serializer = new JsonSerializer();

			//serialize
			_converter.WriteJson(writer, value, serializer);

			//check the output
			Assert.AreEqual("{}", textWriter.ToString(), "The output should have been an empty JSON object");
		}

		[Test]
		public void WriteJson_Writes_Correct_JSON()
		{
			//create a writer and serializer
			var textWriter = new StringWriter();
			var writer = new JsonTextWriter(textWriter);
			var serializer = new JsonSerializer();

			//create an object to serialize
			var toSerialize = new List<IAttachmentMetaData>
			{
				new AttachmentMetaData { Filename = "File 1", ContentType = "text/1" },
				new AttachmentMetaData { Filename = "File 2", ContentType = "text/2" }
			};

			//serialize
			_converter.WriteJson(writer, toSerialize, serializer);

			//check the output
			var expected = @"{""File 1"":{""content_type"":""text/1"",""stub"":true},""File 2"":{""content_type"":""text/2"",""stub"":true}}";
			var actual = textWriter.ToString();
			Assert.AreEqual(expected, actual, "The output should have been a populated JSON object");
		}

	}
}