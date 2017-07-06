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
			if (key == null)
				return null;

			return ToKeyParameter(key);
		}

		public static implicit operator KeyParameter(Array key)
		{
			if (key == null || key.Length == 0)
				return null;

			return ToKeyParameter(key);
		}

		private static KeyParameter ToKeyParameter(object key)
		{
			return new KeyParameter
			{
				FormattedValue = WebUtility.UrlEncode(JsonConvert.SerializeObject(key, Formatting.None, new IsoDateTimeConverter()))
			};
		}
	}
}