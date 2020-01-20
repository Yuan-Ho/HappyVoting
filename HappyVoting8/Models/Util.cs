using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.Security.Cryptography;

namespace HappyVoting8
{
	public static class Consts
	{
		public const string VALUE_PROP_NAME = "value";
		public const string WHO_PROP_NAME = "who";
		public const string TIME_PROP_NAME = "time";
		public const string MAT_PROP_NAME = "mat";		// Short for material.
		public const string TYPE_PROP_NAME = "type";
		public const string PARE_PROP_NAME = "pare";		// Short for parent.
		public const string TPT_PROP_NAME = "tpt";		// Short for tally point.

		public const char ACTION_ID_CHAR = 'a';
		public const int ACTION_ID_DIGITS = 4;		// 4 digits for 0000..9999.

		public const int SHORT_CACHE_SECONDS = 10;
#if DEBUG
		public const int DEFAULT_CACHE_SECONDS = 15;
#else
		public const int DEFAULT_CACHE_SECONDS = 15 * 60;
#endif

		public const int TALLY_POINT_PRECISION = 100;

		public const char SPECIAL_KEY_PREFIX = '!';
		public const char NAME_CONCAT = '@';
		public const char NAME_CONCAT_LIMIT = (char)(NAME_CONCAT + 1);

		public const char TAG_NAME_KEY_PREFIX = 'T';

		public const int NUM_VOTES_IN_A_PAGE = 15;
		public const char PAGE_KEY_PREFIX = 'P';
		public const char INNER_KEY_PREFIX = 'I';
		public const int TAGSCROLL_INNER_KEY_DIGITS = 2;		// 2 digits for 00..99.

		public const string VOTE_PAGE_PATH = "/v/";
	}
	[Serializable]
	public class ProgramLogicException : ApplicationException
	{
		public ProgramLogicException() : base() { }
		public ProgramLogicException(string message) : base(message) { }
	}
	public static class Util
	{
		public static string DateTimeToString(DateTime dt, int type)
		{
			if (type == 1)
				return dt.ToString("yyyy/MM/dd HH:mm:ss");
			else if (type == 2)
				return dt.ToString("yyyy-MM-dd HH:mm:ss");
			else if (type == 3)
				return dt.ToString("yyyy-MM-dd");
			else if (type == 4)
				return dt.ToString("yyyyMMdd");
			else if (type == 5)
				return dt.ToString("yyyyMM");
			else if (type == 6)
				return dt.ToString("yyyyMMdd_HHmmss");
			else
				throw new ProgramLogicException();
		}
		public static string ExceptionDescription(Exception ex)
		{
			string str = string.Format("例外：{0}，訊息：{1}，呼叫堆疊：{2}。", ex.GetType().ToString(), ex.Message, ex.StackTrace);

			if (ex.InnerException != null)
				str = string.Format("{0}Inner例外：{1}，訊息：{2}，呼叫堆疊：{3}。", str, ex.InnerException.GetType().ToString(), ex.InnerException.Message, ex.InnerException.StackTrace);

			return str;
		}
		public static string ClientIp()
		{
			string ip_addr = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

			if (!string.IsNullOrEmpty(ip_addr))
			{
				ip_addr = ip_addr.Split(',')[0].Trim();
				ip_addr = ip_addr.Split(':')[0];
			}
			else
				ip_addr = HttpContext.Current.Request.UserHostAddress;

			return ip_addr;
		}
		public static string RandomAlphaNumericString(int len)
		{
			string text = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
			StringBuilder builder = new StringBuilder(len);
			//Random random = new Random();		// sometimes cause same random number sequence.

			for (int i = 0; i < len; i++)
				builder.Append(text[Warehouse.Random.Next(text.Length)]);
			return builder.ToString();
		}
		public static bool WithinCharSetUserName(string text)
		{
			for (int i = 0; i < text.Length; i++)
			{
				char code = text[i];

				if (code >= 0x80) continue;

				if (code >= 0x61 && code <= 0x7A) continue;		// a..z
				if (code >= 0x41 && code <= 0x5A) continue;		// A..Z
				if (code >= 0x30 && code <= 0x39) continue;		// 0..9

				if (i == 0 || i == text.Length - 1) return false;

				if (" .-_".IndexOf(code) == -1) return false;
			}
			return true;
		}
		public static byte[] ToMd5(byte[] data)
		{
			MD5 md5 = new MD5CryptoServiceProvider();
			return md5.ComputeHash(data);
		}
		public static byte[] ToMd5(string text)
		{
			byte[] data = Encoding.UTF8.GetBytes(text);
			return ToMd5(data);
		}
		public static string ToDumpStringCompact(byte[] data)
		{
			StringBuilder builder = new StringBuilder();

			for (int i = 0; i < data.Length; i++)
			{
				builder.Append(data[i].ToString("X2"));
			}
			return builder.ToString();
		}

	}
	public static class Convertor
	{
		public static string ToString(DateTimeOffset? dto)
		{
#if NOT_USED
			return dto.Value.ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyy/MM/dd(ddd) HH:mm:ss.ff");
#else
			// return dto.Value.ToOffset(new TimeSpan(8, 0, 0)).ToString("o");
			return dto.Value.ToString("o");
#endif
		}
	}
}