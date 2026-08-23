using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Backend.Net
{
    /// <summary>
    /// CommandMessage / EventMessage JSON 와이어. UnityEngine·Newtonsoft 없이 동작한다.
    /// </summary>
    public static class WireJson
    {
        /// <summary>클라→서버 커맨드를 JSON 으로 직렬화한다.</summary>
        public static string SerializeCommand(CommandMessage command)
        {
            return SerializeObject(command);
        }

        /// <summary>서버→클라 이벤트를 JSON 으로 직렬화한다.</summary>
        public static string SerializeEvent(EventMessage ev)
        {
            return SerializeObject(ev);
        }

        /// <summary>커맨드 JSON 을 읽는다. 실패하면 null.</summary>
        public static CommandMessage DeserializeCommand(string json)
        {
            return Deserialize<CommandMessage>(json);
        }

        /// <summary>이벤트 JSON 을 읽는다. 실패하면 null.</summary>
        public static EventMessage DeserializeEvent(string json)
        {
            return Deserialize<EventMessage>(json);
        }

        private static string SerializeObject(object value)
        {
            if (value == null)
            {
                return "null";
            }

            var sb = new StringBuilder();
            WriteValue(sb, value);
            return sb.ToString();
        }

        private static T Deserialize<T>(string json) where T : class, new()
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try
            {
                var reader = new Reader(json);
                var parsed = reader.ParseValue();
                if (!(parsed is Dictionary<string, object> map))
                {
                    return null;
                }

                var result = new T();
                ApplyFields(result, map);
                return result;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private static void WriteValue(StringBuilder sb, object value)
        {
            if (value == null)
            {
                sb.Append("null");
                return;
            }

            if (value is string text)
            {
                WriteString(sb, text);
                return;
            }

            if (value is bool flag)
            {
                sb.Append(flag ? "true" : "false");
                return;
            }

            if (value is int i)
            {
                sb.Append(i.ToString(CultureInfo.InvariantCulture));
                return;
            }

            if (value is long l)
            {
                sb.Append(l.ToString(CultureInfo.InvariantCulture));
                return;
            }

            if (value is int[] ints)
            {
                WriteArray(sb, ints.Length, index => WriteValue(sb, ints[index]));
                return;
            }

            if (value is string[] strings)
            {
                WriteArray(sb, strings.Length, index => WriteValue(sb, strings[index]));
                return;
            }

            if (value is bool[] bools)
            {
                WriteArray(sb, bools.Length, index => WriteValue(sb, bools[index]));
                return;
            }

            WriteObject(sb, value);
        }

        private static void WriteObject(StringBuilder sb, object value)
        {
            var fields = value.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
            sb.Append('{');
            var first = true;
            for (var i = 0; i < fields.Length; i++)
            {
                var fieldValue = fields[i].GetValue(value);
                if (fieldValue == null)
                {
                    continue;
                }

                if (!first)
                {
                    sb.Append(',');
                }

                first = false;
                WriteString(sb, fields[i].Name);
                sb.Append(':');
                WriteValue(sb, fieldValue);
            }

            sb.Append('}');
        }

        private static void WriteArray(StringBuilder sb, int length, Action<int> writeItem)
        {
            sb.Append('[');
            for (var i = 0; i < length; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                writeItem(i);
            }

            sb.Append(']');
        }

        private static void WriteString(StringBuilder sb, string value)
        {
            sb.Append('"');
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                switch (c)
                {
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        if (c < 32)
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(c);
                        }

                        break;
                }
            }

            sb.Append('"');
        }

        private static void ApplyFields(object target, Dictionary<string, object> map)
        {
            var fields = target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                if (!map.TryGetValue(field.Name, out var raw) || raw == null)
                {
                    continue;
                }

                field.SetValue(target, Coerce(raw, field.FieldType));
            }
        }

        private static object Coerce(object raw, Type type)
        {
            if (type == typeof(string))
            {
                return raw as string ?? raw.ToString();
            }

            if (type == typeof(int))
            {
                return Convert.ToInt32(raw, CultureInfo.InvariantCulture);
            }

            if (type == typeof(long))
            {
                return Convert.ToInt64(raw, CultureInfo.InvariantCulture);
            }

            if (type == typeof(bool))
            {
                return raw is bool b ? b : Convert.ToBoolean(raw, CultureInfo.InvariantCulture);
            }

            if (type == typeof(int[]))
            {
                return ToIntArray(raw);
            }

            if (type == typeof(string[]))
            {
                return ToStringArray(raw);
            }

            if (type == typeof(bool[]))
            {
                return ToBoolArray(raw);
            }

            if (raw is Dictionary<string, object> nested)
            {
                var instance = Activator.CreateInstance(type);
                ApplyFields(instance, nested);
                return instance;
            }

            return raw;
        }

        private static int[] ToIntArray(object raw)
        {
            if (!(raw is List<object> list))
            {
                return null;
            }

            var result = new int[list.Count];
            for (var i = 0; i < list.Count; i++)
            {
                result[i] = Convert.ToInt32(list[i], CultureInfo.InvariantCulture);
            }

            return result;
        }

        private static string[] ToStringArray(object raw)
        {
            if (!(raw is List<object> list))
            {
                return null;
            }

            var result = new string[list.Count];
            for (var i = 0; i < list.Count; i++)
            {
                result[i] = list[i] == null ? null : list[i].ToString();
            }

            return result;
        }

        private static bool[] ToBoolArray(object raw)
        {
            if (!(raw is List<object> list))
            {
                return null;
            }

            var result = new bool[list.Count];
            for (var i = 0; i < list.Count; i++)
            {
                result[i] = list[i] is bool b && b;
            }

            return result;
        }

        private sealed class Reader
        {
            private readonly string _s;
            private int _i;

            public Reader(string s)
            {
                _s = s;
            }

            public object ParseValue()
            {
                SkipWs();
                if (_i >= _s.Length)
                {
                    throw new FormatException("empty");
                }

                var c = _s[_i];
                if (c == '{')
                {
                    return ParseObject();
                }

                if (c == '[')
                {
                    return ParseArray();
                }

                if (c == '"')
                {
                    return ParseString();
                }

                if (c == 't' || c == 'f')
                {
                    return ParseBool();
                }

                if (c == 'n')
                {
                    ParseNull();
                    return null;
                }

                return ParseNumber();
            }

            private Dictionary<string, object> ParseObject()
            {
                Expect('{');
                var map = new Dictionary<string, object>();
                SkipWs();
                if (TryConsume('}'))
                {
                    return map;
                }

                while (true)
                {
                    SkipWs();
                    var key = ParseString();
                    SkipWs();
                    Expect(':');
                    map[key] = ParseValue();
                    SkipWs();
                    if (TryConsume('}'))
                    {
                        return map;
                    }

                    Expect(',');
                }
            }

            private List<object> ParseArray()
            {
                Expect('[');
                var list = new List<object>();
                SkipWs();
                if (TryConsume(']'))
                {
                    return list;
                }

                while (true)
                {
                    list.Add(ParseValue());
                    SkipWs();
                    if (TryConsume(']'))
                    {
                        return list;
                    }

                    Expect(',');
                }
            }

            private string ParseString()
            {
                Expect('"');
                var sb = new StringBuilder();
                while (_i < _s.Length)
                {
                    var c = _s[_i++];
                    if (c == '"')
                    {
                        return sb.ToString();
                    }

                    if (c != '\\')
                    {
                        sb.Append(c);
                        continue;
                    }

                    if (_i >= _s.Length)
                    {
                        throw new FormatException("bad escape");
                    }

                    var e = _s[_i++];
                    switch (e)
                    {
                        case '"':
                        case '\\':
                        case '/':
                            sb.Append(e);
                            break;
                        case 'n':
                            sb.Append('\n');
                            break;
                        case 'r':
                            sb.Append('\r');
                            break;
                        case 't':
                            sb.Append('\t');
                            break;
                        case 'u':
                            if (_i + 4 > _s.Length)
                            {
                                throw new FormatException("bad unicode");
                            }

                            sb.Append((char)int.Parse(_s.Substring(_i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            _i += 4;
                            break;
                        default:
                            sb.Append(e);
                            break;
                    }
                }

                throw new FormatException("unterminated string");
            }

            private bool ParseBool()
            {
                if (Match("true"))
                {
                    return true;
                }

                if (Match("false"))
                {
                    return false;
                }

                throw new FormatException("bad bool");
            }

            private void ParseNull()
            {
                if (!Match("null"))
                {
                    throw new FormatException("bad null");
                }
            }

            private object ParseNumber()
            {
                var start = _i;
                if (_i < _s.Length && (_s[_i] == '-' || _s[_i] == '+'))
                {
                    _i++;
                }

                while (_i < _s.Length && char.IsDigit(_s[_i]))
                {
                    _i++;
                }

                if (start == _i)
                {
                    throw new FormatException("bad number");
                }

                return long.Parse(_s.Substring(start, _i - start), CultureInfo.InvariantCulture);
            }

            private void SkipWs()
            {
                while (_i < _s.Length && char.IsWhiteSpace(_s[_i]))
                {
                    _i++;
                }
            }

            private void Expect(char c)
            {
                SkipWs();
                if (_i >= _s.Length || _s[_i] != c)
                {
                    throw new FormatException("expected " + c);
                }

                _i++;
            }

            private bool TryConsume(char c)
            {
                if (_i < _s.Length && _s[_i] == c)
                {
                    _i++;
                    return true;
                }

                return false;
            }

            private bool Match(string token)
            {
                if (_i + token.Length > _s.Length)
                {
                    return false;
                }

                for (var i = 0; i < token.Length; i++)
                {
                    if (_s[_i + i] != token[i])
                    {
                        return false;
                    }
                }

                _i += token.Length;
                return true;
            }
        }
    }
}
