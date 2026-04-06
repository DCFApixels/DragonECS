using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DCFApixels.DragonECS.Core.Internal
{
    internal static class JsonDebugger
    {
        internal static string ToJsonLog(object obj)
        {
            if (obj == null) return "null";
            var sb = new StringBuilder();
            ToJsonLog(obj, sb, new HashSet<object>(), "  ", "");
            return sb.ToString();
        }

        private static void ToJsonLog(
            object value,
            StringBuilder sb,
            HashSet<object> visited,
            string indent,
            string currentIndent)
        {
            if (value == null)
            {
                sb.Append("null");
                return;
            }

            Type type = value.GetType();

            if (value is IEnumerable<char> rawString)
            {
                sb.Append('"');
                string str = value as string;
                if (str == null)
                {
                    str = rawString.ToString();
                }
                EscapeString(str, sb);
                sb.Append('"');
                return;
            }

            if (type == typeof(float))
            {
                sb.Append(((float)value).ToString(System.Globalization.CultureInfo.InvariantCulture));
                return;
            }
            if(type == typeof(double))
            {
                sb.Append(((double)value).ToString(System.Globalization.CultureInfo.InvariantCulture));
                return;
            }
            if (type == typeof(decimal))
            {
                sb.Append(((decimal)value).ToString(System.Globalization.CultureInfo.InvariantCulture));
                return;
            }
            if (type == typeof(bool))
            {
                sb.Append((bool)value ? "true" : "false");
                return;
            }
            if (type.IsEnum)
            {
                if(type.TryGetAttribute(out FlagsAttribute _))
                {
                    sb.Append(type.FullName);
                    sb.Append('.');
                    sb.Append(value.ToString());
                }
                else
                {
                    sb.Append(value.ToString());
                }

                return;
            }
            if (type == typeof(DateTime))
            {
                sb.Append('"');
                sb.Append(((DateTime)value).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", System.Globalization.CultureInfo.InvariantCulture));
                sb.Append('"');
                return;
            }
            if (type == typeof(Guid))
            {
                sb.Append('"');
                sb.Append(value.ToString());
                sb.Append('"');
                return;
            }
            if (type.IsPrimitive)
            {
                sb.Append(value);
                return;
            }
            if (value is Exception e)
            {
                sb.Append('"');
                sb.Append(type.Name);
                sb.Append(':').Append(' ');
                sb.Append(e.Message);
                sb.Append('"');
                return;
            }
            if (typeof(Type).IsAssignableFrom(type) ||
                type.Namespace == typeof(FieldInfo).Namespace ||
                type.IsPointer ||
                type.IsFunctionPointer ||
                type.IsUnmanagedFunctionPointer)
            {
                sb.Append('"');
                sb.Append(value.ToString());
                sb.Append('"');
                return;
            }

            if(value is Delegate del)
            {
                var list = del.GetInvocationList();
                if(list.Length == 0)
                {
                    sb.Append("null");
                    return;
                }
                if (list.Length == 1)
                {
                    sb.Append('"');
                    sb.Append(del.Target.GetType().FullName);
                    sb.Append('.');
                    sb.Append(del.Method.Name);
                    sb.Append('"');
                    return;
                }

                value = list;
                type = list.GetType();
                // как дописать приваильно тут вызов ToJsonLog ?
            }

            if (visited.Contains(value))
            {
                sb.Append('#');
                sb.Append(type.Name);
                sb.Append('#');
                return;
            }
            visited.Add(value);

            if (value is IEnumerable enumerable)
            {
                sb.Append('[');
                bool first = true;
                string nextIndent = currentIndent + indent;

                foreach (object item in enumerable)
                {
                    if (!first) { sb.Append(','); } else { first = false; }

                    // Перенос строки и отступ перед элементом
                    sb.AppendLine();
                    sb.Append(nextIndent);
                    ToJsonLog(item, sb, visited, indent, nextIndent);
                }

                // Если были элементы, переносим строку перед закрывающей скобкой
                if (!first)
                {
                    sb.AppendLine();
                    sb.Append(currentIndent);
                }
                sb.Append(']');
            }
            else // Object
            {
                sb.Append('{');
                bool first = true;
                string nextIndent = currentIndent + indent;

                // Fields
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var field in fields)
                {
                    if (field.IsStatic) continue;

                    if (!first) { sb.Append(','); } else { first = false; }

                    sb.AppendLine();
                    sb.Append(nextIndent);
                    sb.Append('"');
                    sb.Append(field.Name);
                    sb.Append('"');
                    sb.Append(':').Append(' ');

                    object fieldValue = field.GetValue(value);
                    ToJsonLog(fieldValue, sb, visited, indent, nextIndent);
                }

                // Properties
                var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var prop in properties)
                {
                    if (prop.GetIndexParameters().Length > 0 ||
                        prop.GetMethod == null ||
                        prop.GetMethod.IsStatic)
                        continue;

                    if (!first) { sb.Append(','); } else { first = false; }

                    sb.AppendLine();
                    sb.Append(nextIndent);
                    sb.Append('"');
                    sb.Append(prop.Name);
                    sb.Append('"');
                    sb.Append(':').Append(' ');

                    object propValue;
                    try
                    {
                        propValue = prop.GetValue(value);
                    }
                    catch (Exception cathcedE)
                    {
                        propValue = cathcedE;
                    }
                    ToJsonLog(propValue, sb, visited, indent, nextIndent);
                }

                // Если были поля/свойства, добавляем перевод строки перед закрывающей скобкой
                if (!first)
                {
                    sb.AppendLine();
                    sb.Append(currentIndent);
                }
                sb.Append('}');
            }

            visited.Remove(value);
        }

        private static void EscapeString(string s, StringBuilder sb)
        {
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (char.IsControl(c))
                        {
                            sb.AppendFormat("\\u{0:x4}", (int)c);
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
        }
    }
}