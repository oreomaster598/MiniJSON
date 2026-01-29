using System.Text;

namespace MiniJSON
{
	public enum JSONTokenType
	{
		Number,
		Colon,
		Terminator,
		Equals,
		LCurl,
		RCurl,
		Comma,
		LBracket,
		RBacket,
		String,
		EoF,
		Unknown,
		Identifier,
		Comment,
	}
	public enum JSONValueType
	{
		None,
		Float,
		Double,
		Int,
		UInt,
		Long,
		ULong,
		Short,
		UShort,
		String,
		Boolean,
	}
	public class JSONToken
	{
		public string value;
		public JSONTokenType type;
		public JSONValueType valuetype = JSONValueType.None;
		public int position;

		public override string ToString()
		{
			return $"Type: {type}, Value: {value}, Value Type: {valuetype}, Position: {position}";
		}
	}
	public class JSONTokenizer
	{
		private StringBuilder value = new StringBuilder();
		private string data;
		private int Position = 0;
		private bool EoF = false;
		private int TokenStart = 0;
		private char c => data[Position];
		private char next => Position + 1 < data.Length ? data[Position + 1] : '\0';
		private char prev => Position - 1 >= 0 ? data[Position - 1] : '\0';

		private void Advance()
		{
			if (Position + 1 == data.Length)
			{
				EoF = true;
				return;
			}
			Position++;
		}
		private void Consume()
		{
			if (!"\r".Contains(c))
				value.Append(c);
			Advance();
		}
		public JSONTokenizer(string data)
		{
			this.data = data;
		}
		public List<JSONToken> Tokenize(bool include_all = false)
		{
			Position = 0;
			EoF = false;
			List<JSONToken> tokens = new List<JSONToken>();
			while (!EoF)
			{
				JSONTokenType type = GetTokenType();
				switch (type)
				{
					case JSONTokenType.Number:
						tokens.Add(ConsumeNumber());
						continue;
					case JSONTokenType.String:
						tokens.Add(ConsumeString());
						continue;
					case JSONTokenType.Identifier:
						tokens.Add(ConsumeIdentifier());
						continue;
					case JSONTokenType.Comment:
						if (include_all)
							tokens.Add(ConsumeComment());
						else
							ConsumeComment();
						continue;
				}
				if (type != JSONTokenType.Unknown) // All single character tokens
				{
					TokenStart = Position;
					Consume();
					tokens.Add(CreateToken(type));
					continue;
				}
				Advance();
			}
			return tokens;
		}
		private bool IsDigit()
		{
			return (c >= '0' && c <= '9') || c == '.';
		}
		private bool IsString()
		{
			return c == '"' || c == '\'';
		}
		private bool IsIdentifier()
		{
			return "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_-1234567890".Contains(c);
		}
		private bool IsComment()
		{
			return c == '/' && (next == '/' || next == '*');
		}
		private JSONTokenType GetTokenType()
		{
			if (IsDigit())
				return JSONTokenType.Number;
			if (IsString())
				return JSONTokenType.String;
			if (IsIdentifier())
				return JSONTokenType.Identifier;
			if (IsComment())
				return JSONTokenType.Comment;
			if (c == '{')
				return JSONTokenType.LCurl;
			if (c == '}')
				return JSONTokenType.RCurl;
			if (c == '[')
				return JSONTokenType.LBracket;
			if (c == ']')
				return JSONTokenType.RBacket;
			if (c == ',')
				return JSONTokenType.Comma;
			if (c == ':')
				return JSONTokenType.Colon;

			return JSONTokenType.Unknown;
		}
		private JSONToken ConsumeNumber()
		{
			TokenStart = Position;
			JSONValueType valuetype = JSONValueType.Int;
			while (IsDigit())
			{
				Consume();
				if (c == '.')
					valuetype = JSONValueType.Float;
			}
			switch (c)
			{
				case 'f':
				case 'F':
					valuetype = JSONValueType.Float;
					break;
				case 'd':
				case 'D':
					valuetype = JSONValueType.Double;
					break;
				case 'u':
				case 'U':
					valuetype = JSONValueType.UInt;
					break;
				case 'l':
				case 'L':
					valuetype = JSONValueType.Long;
					break;
				case 's':
				case 'S':
					valuetype = JSONValueType.Short;
					break;
			}
			if ("fFdDuUlLsS".Contains(c))
			{
				Advance();
				if (valuetype == JSONValueType.UInt)
				{
					switch (c)
					{
						case 'l':
						case 'L':
							valuetype = JSONValueType.ULong;
							Advance();
							break;
						case 's':
						case 'S':
							valuetype = JSONValueType.UShort;
							Advance();
							break;
					}
				}
			}
			return CreateToken(JSONTokenType.Number, valuetype);
		}
		private JSONToken ConsumeComment()
		{
			TokenStart = Position;
			Advance();
			if (c == '*')
			{
				Advance(); // *
				while (!(c == '*' && next == '/'))
				{
					Consume();
				}
				Advance(); // *
				Advance(); // /
			}
			else if (c == '/')
			{
				Advance(); // /
				while (c != '\n')
				{
					Consume();
				}
				Advance(); // \n
			}
			return CreateToken(JSONTokenType.Comment);
		}
		private JSONToken ConsumeString()
		{
			TokenStart = Position;
			Advance(); // "
			while (!IsString())
			{
				if (c == '\\')
				{
					Advance();
					Consume();
				}
				else
					Consume();
			}
			Advance(); // "
			return CreateToken(JSONTokenType.String, JSONValueType.String);
		}
		private JSONToken ConsumeIdentifier()
		{
			while (IsIdentifier())
				Consume();
			JSONValueType valuetype = JSONValueType.None;
			string v = value.ToString().ToLower();
			if (v == "true" || v == "false")
				valuetype = JSONValueType.Boolean;
			return CreateToken(JSONTokenType.Identifier, valuetype);
		}
		private JSONToken CreateToken(JSONTokenType type)
		{
			JSONToken token = new JSONToken();
			token.value = value.ToString();
			value.Clear();
			token.type = type;
			token.position = TokenStart;
			return token;
		}
		private JSONToken CreateToken(JSONTokenType type, JSONValueType valuetype)
		{
			JSONToken token = new JSONToken();
			token.value = value.ToString();
			value.Clear();
			token.type = type;
			token.valuetype = valuetype;
			token.position = TokenStart;
			return token;
		}
	}

	public class JSONParser
	{
		private List<JSONToken> tokens;
		private int Position = 0;
		private JSONNode root;
		private JSONToken token => tokens[Position];
		private JSONToken took;
		private void Advance()
		{
			Position++;
		}

		public JSONParser(List<JSONToken> tokens)
		{
			this.tokens = tokens;
		}

		public JSONNode Parse()
		{
			Position = 0;
			return TakeNode();
		}
		private JSONNode TakeNode()
		{
			Take(JSONTokenType.LCurl);
			if (token.type == JSONTokenType.RCurl)
			{
				Take();
				return new JSONNode();
			}
			List<JSONValue> values = [TakeValue()];
			while (token.type == JSONTokenType.Comma)
			{
				Take();
				values.Add(TakeValue());
			}
			Take(JSONTokenType.RCurl);
			JSONNode node = new JSONNode();
			node.values = values.ToArray();
			return node;
		}
		private JSONValue TakeValue()
		{
			string name = Take(JSONTokenType.String).value;
			Take(JSONTokenType.Colon);
			object value = null;
			switch (token.type)
			{
				case JSONTokenType.Number:
					value = ParseNumber(Take());
					break;
				case JSONTokenType.Identifier:
					value = ParseIdentifier(Take());
					break;
				case JSONTokenType.String:
					value = Take().value;
					break;
				case JSONTokenType.LBracket:
					value = TakeArray();
					break;
				case JSONTokenType.LCurl:
					value = TakeNode();
					break;
			}

			return new JSONValue(name, value);
		}
		private object ParseNumber(JSONToken token)
		{
			switch (token.valuetype)
			{
				case JSONValueType.Int:
					return int.Parse(token.value);
				case JSONValueType.UInt:
					return uint.Parse(token.value);
				case JSONValueType.Short:
					return short.Parse(token.value);
				case JSONValueType.UShort:
					return ushort.Parse(token.value);
				case JSONValueType.Long:
					return long.Parse(token.value);
				case JSONValueType.ULong:
					return ulong.Parse(token.value);
				case JSONValueType.Float:
					return float.Parse(token.value);
				case JSONValueType.Double:
					return double.Parse(token.value);
			}
			return 0;
		}
		private object ParseIdentifier(JSONToken token)
		{
			switch (token.valuetype)
			{
				case JSONValueType.Boolean:
					return bool.Parse(token.value);
			}
			return null;
		}
		private object[] TakeArray()
		{
			Take(JSONTokenType.LBracket);
			if (token.type == JSONTokenType.RBacket)
				return new object[0];
			List<object> values = new List<object>();
			do
			{
				switch (token.type)
				{
					case JSONTokenType.Number:
						values.Add(ParseNumber(Take()));
						break;
					case JSONTokenType.Identifier:
						values.Add(ParseIdentifier(Take()));
						break;
					case JSONTokenType.String:
						values.Add(Take().value);
						break;
					case JSONTokenType.LBracket:
						values.Add(TakeArray());
						break;
					case JSONTokenType.LCurl:
						values.Add(TakeNode());
						break;
				}
			} while (Take(JSONTokenType.Comma, JSONTokenType.RBacket).type == JSONTokenType.Comma);
			return values.ToArray();
		}
		private JSONToken Take()
		{
			took = token;
			Advance();
			return took;
		}
		private JSONToken Take(JSONTokenType type)
		{
			if (token.type != type)
				throw new Exception($"Expected {type} but got \"{token.value}\"");
			return Take();
		}
		private JSONToken Take(JSONTokenType type, JSONTokenType type2)
		{
			if (token.type != type && token.type != type2)
				throw new Exception($"Expected {type} or {type2} but got \"{token.value}\"");
			return Take();
		}
	}

	public class JSONValue
	{
		public string name;
		public object value;
		public JSONValue(string name, object value)
		{
			this.name = name;
			this.value = value;
		}
		private static string Format(object obj)
		{
			if (obj is string)
				return $"\"{obj}\"";
			if (JSONNode.CompatMode)
			{
				if (obj is bool)
					return (bool)obj ? "true" : "false";

				goto END;
			}
			if (obj is float)
				return obj.ToString() + "f";
			if (obj is double)
				return obj.ToString() + "d";
			if (obj is uint)
				return obj.ToString() + "u";
			if (obj is short)
				return obj.ToString() + "s";
			if (obj is ushort)
				return obj.ToString() + "us";
			if (obj is long)
				return obj.ToString() + "l";
			if (obj is ulong)
				return obj.ToString() + "ul";

			END:
			return obj.ToString() ?? string.Empty;
		}
		private void PrintObject(StringBuilder sb, object obj, int indent = 0, bool id = false, bool comma = true)
		{
			if (obj is JSONNode)
			{
				sb.Append('\n');
				((JSONNode)obj).Print(sb, indent);

			}
			else if (obj is object[])
			{
				object[] objects = (object[])obj;

				sb.Append($"\n{new string(' ', indent * 2)}[\n");
				for (int i = 0; i < objects.Length; i++)
				{
					bool b = i != objects.Length - 1;
					PrintObject(sb, objects[i], indent + 1, true, b);
					if (b)
						sb.Append("\n");

				}
				sb.Append($"\n{new string(' ', indent * 2)}]");

			}
			else
			{
				string s = id ? new string(' ', indent * 2) : string.Empty;
				sb.Append($"{s}{Format(obj)}");
			}
			if (comma)
				sb.Append(',');
		}
		public void Print(StringBuilder sb, int indent = 0, bool comma = true)
		{
			sb.Append($"\n{new string(' ', indent * 2)}\"{name}\": ");

			PrintObject(sb, value, indent + 1, false, comma);
		}
	}
	public class JSONNode
	{
		internal JSONValue[] values;
		public static bool CompatMode = false;
		public static JSONNode Parse(string str)
		{
			return new JSONParser(new JSONTokenizer(str).Tokenize()).Parse();
		}
		public object[] GetArray(string name)
		{
			for (int i = 0; i < values.Length; i++)
			{
				if (values[i].name == name && values[i].value is object[])
					return (object[])values[i].value;
			}
			return new object[0];
		}
		public T? GetValue<T>(string name)
		{
			for (int i = 0; i < values.Length; i++)
			{
				if (values[i].name == name && values[i].value is T)
					return (T)values[i].value;
			}
			return default(T);
		}
		public void SetValue<T>(string name, T value)
		{
			for (int i = 0; i < values.Length; i++)
			{
				if (values[i].name == name && values[i].value is T)
				{
					values[i].value = value;
					return;
				}
			}
		}
		public int GetInt(string name) => GetValue<int>(name);
		public uint GetUInt(string name) => GetValue<uint>(name);
		public short GetShort(string name) => GetValue<short>(name);
		public ushort GetUShort(string name) => GetValue<ushort>(name);
		public long GetLong(string name) => GetValue<long>(name);
		public ulong GetULong(string name) => GetValue<ulong>(name);
		public float GetFloat(string name) => GetValue<float>(name);
		public double GetDouble(string name) => GetValue<double>(name);
		public string GetString(string name) => GetValue<string>(name) ?? string.Empty;
		public bool GetBool(string name) => GetValue<bool>(name);
		public void SetInt(string name, int value) => SetValue<int>(name, value);
		public void SetUInt(string name, uint value) => SetValue<uint>(name, value);
		public void SetShort(string name, short value) => SetValue<short>(name, value);
		public void SetUShort(string name, ushort value) => SetValue<ushort>(name, value);
		public void SetLong(string name, long value) => SetValue<long>(name, value);
		public void SetULong(string name, ulong value) => SetValue<ulong>(name, value);
		public void SetFloat(string name, float value) => SetValue<float>(name, value);
		public void SetDouble(string name, double value) => SetValue<double>(name, value);
		public void SetString(string name, string value) => SetValue<string>(name, value);
		public void SetBool(string name, bool value) => SetValue<bool>(name, value);

		public JSONNode? GetNode(string name)
		{
			for (int i = 0; i < values.Length; i++)
			{
				if (values[i].name == name && values[i].value is JSONNode)
					return (JSONNode)values[i].value;
			}
			return null;
		}
		public void Print(StringBuilder sb, int indent = 0)
		{
			sb.Append($"{new string(' ', indent * 2)}{{");
			for (int i = 0; i < values.Length; i++)
			{
				values[i].Print(sb, indent + 1, i + 1 != values.Length);
			}
			sb.Append($"\n{new string(' ', indent * 2)}}}");

		}
		public string Print()
		{
			StringBuilder sb = new StringBuilder();
			Print(sb);
			return sb.ToString();
		}
		public string CompatPrint()
		{
			CompatMode = true;
			string s = Print();
			CompatMode = false;
			return s;
		}
	}
}