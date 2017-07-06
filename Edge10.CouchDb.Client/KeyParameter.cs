using System;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Edge10.CouchDb.Client
{
	public class KeyParameter
	{
		public string FormattedValue { get; private set; }

		public static implicit operator KeyParameter(string key)
		{
			return new KeyParameter
			{
				FormattedValue = WebUtility.UrlEncode(JsonConvert.SerializeObject(key, Formatting.None, new IsoDateTimeConverter()))
			};
		}

		public static implicit operator KeyParameter(Array key)
		{
			var value = JsonConvert.SerializeObject(key, Formatting.None, new IsoDateTimeConverter());
			return new KeyParameter
			{
				FormattedValue = WebUtility.UrlEncode(value)
			};
		}
	}
}