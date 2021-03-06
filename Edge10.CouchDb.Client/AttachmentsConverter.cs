using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Edge10.CouchDb.Client
{
	/// <summary>
	/// A <see cref="JsonConverter"/> extension used to convert the attachments object on a CouchDb document
	/// to a list of <see cref="IAttachmentMetaData"/> instances.
	/// </summary>
	public class AttachmentsConverter : JsonConverter
	{
		/// <summary>
		/// Determines whether this instance can convert the specified object type.
		/// </summary>
		/// <param name="objectType">Type of the object.</param>
		/// <returns>
		///   <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
		/// </returns>
		public override bool CanConvert(Type objectType)
		{
			return true;
		}

		/// <summary>
		/// Reads the JSON representation of the object.
		/// </summary>
		/// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader"/> to read from.</param>
		/// <param name="objectType">Type of the object.</param>
		/// <param name="existingValue">The existing value of object being read.</param>
		/// <param name="serializer">The calling serializer.</param>
		/// <returns>
		/// The object value.
		/// </returns>
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader     == null) throw new ArgumentNullException(nameof(reader));
			if (serializer == null) throw new ArgumentNullException(nameof(serializer));

			var attachments = existingValue as IList<IAttachmentMetaData> ?? new List<IAttachmentMetaData>();
			var parsedJson = JObject.Load(reader);

			foreach (var item in parsedJson)
			{
				var attachment = new AttachmentMetaData { Filename = item.Key };
				serializer.Populate(item.Value.CreateReader(), attachment);
				attachments.Add(attachment);
			}

			return attachments;
		}

		/// <summary>
		/// Writes the JSON representation of the object.
		/// </summary>
		/// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter"/> to write to.</param>
		/// <param name="value">The value.</param>
		/// <param name="serializer">The calling serializer.</param>
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (writer     == null) throw new ArgumentNullException(nameof(writer));
			if (serializer == null) throw new ArgumentNullException(nameof(serializer));

			writer.WriteStartObject();

			var attachments = value as IList<IAttachmentMetaData>;
			if (attachments != null)
			{
				foreach (var attachment in attachments)
				{
					writer.WritePropertyName(attachment.Filename);
					var typeNameHandling = serializer.TypeNameHandling;
					serializer.TypeNameHandling = TypeNameHandling.None;
					serializer.Serialize(writer, attachment);
					serializer.TypeNameHandling = typeNameHandling;
				}
			}

			writer.WriteEndObject();
		}
	}
}