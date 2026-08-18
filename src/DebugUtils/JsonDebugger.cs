using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DCFApixels.DragonECS.Core.Internal
{
    internal static class JsonDebugger
    {
        private const int MAX_DEPTH = 32;

        [ThreadStatic]
        private static List<string> _indentsCache;
        internal static string ToJsonLog(object obj, bool withProperties)
        {
            if (obj == null) { return "null"; }
            var sb = new StringBuilder();
            int linesCounter = 0;
            var visited = new Dictionary<object, int>();
            ToJsonLog_Internal(ref linesCounter, obj, sb, visited, 0, 2, withProperties);
            string json = sb.ToString();
            return json;
        }
        private static string GetIndentString(int count)
        {
            if (_indentsCache == null)
            {
                _indentsCache = new List<string>();
            }
            int newSize = count + 1;
            while (newSize > _indentsCache.Count)
            {
                _indentsCache.Add(new string(' ', _indentsCache.Count));
            }
            return _indentsCache[count];
        }
        private static void NewLine(
            ref int linesCounter,
            StringBuilder sb,
            int indent,
            int indentStep)
        {
            sb.AppendLine();
            sb.Append(GetIndentString(indent * indentStep));
            linesCounter++;
        }
        private static void ToJsonLog_Internal(
            ref int linesCounter,
            object value,
            StringBuilder sb,
            Dictionary<object, int> visited,
            int indent,
            int indentStep,
            bool withProperties)
        {
            if (value == null)
            {
                sb.Append("null");
                return;
            }
            if (indent >= MAX_DEPTH)
            {
                sb.Append("#MAX_DEPTH#");
                return;
            }

            Type type = value.GetType();

            if (value is IEnumerable<char> rawString)
            {
                sb.Append('"');
                string str = value as string;
                if (str == null)
                {
                    str = new string(rawString.ToArray());
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
            if (type == typeof(double))
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
                sb.Append('"');
                if (type.TryGetAttribute(out FlagsAttribute _))
                {
                    sb.Append(type.FullName);
                    sb.Append('.');
                    sb.Append(value.ToString());
                }
                else
                {
                    sb.Append(value.ToString());
                }
                sb.Append('"');
                return;
            }
            if (type == typeof(char))
            {
                sb.Append('"');
                EscapeString(value.ToString(), sb);
                sb.Append('"');
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
                EscapeString(type.Name + ": " + e.Message, sb);
                sb.Append('"');
                return;
            }
            if (value is Type t)
            {
                sb.Append('"');
                sb.Append(t.GetMeta().TypeName);
                sb.Append('"');
                return;
            }
            if (type.Namespace == typeof(FieldInfo).Namespace || type.IsPointer)
            {
                sb.Append('"');
                sb.Append(value.ToString());
                sb.Append('"');
                return;
            }

            if (value is Delegate del)
            {
                var list = del.GetInvocationList();
                if (list.Length == 0)
                {
                    sb.Append("null");
                    return;
                }
                if (list.Length == 1)
                {
                    sb.Append('"');
                    Type ownerType = del.Target == null ? del.Method.DeclaringType : del.Target.GetType();
                    sb.Append(ownerType == null ? "<static>" : ownerType.FullName);
                    sb.Append('.');
                    sb.Append(del.Method.Name);
                    sb.Append('"');
                    return;
                }
                ToJsonLog_Internal(ref linesCounter, list, sb, visited, indent, indentStep, withProperties);
                return;
            }

            if (type.IsValueType == false)
            {
                if (visited.TryGetValue(value, out var line))
                {
                    sb.Append('#').Append(type.Name).Append('#').Append(line).Append('#');
                    return;
                }
                visited.Add(value, linesCounter);
            }

            // Collections
            IEnumerable enumerable = value as IEnumerable;
            if (enumerable != null)
            {
                try
                {
                    enumerable.GetEnumerator();
                }
                catch (Exception)
                {
                    enumerable = null;
                }
            }
            if (enumerable != null)
            {
                sb.Append('[');
                bool first = true;

                foreach (object item in enumerable)
                {
                    if (!first) { sb.Append(','); } else { first = false; }
                    NewLine(ref linesCounter, sb, indent + 1, indentStep);
                    ToJsonLog_Internal(ref linesCounter, item, sb, visited, indent + 1, indentStep, withProperties);
                }

                // перенос строки если были элементы
                if (!first)
                {
                    NewLine(ref linesCounter, sb, indent, indentStep);
                }
                sb.Append(']');
            }
            else // Object
            {
                sb.Append('{');
                {
                    NewLine(ref linesCounter, sb, indent + 1, indentStep);
                    sb.Append("\"Type\": ");

                    ToJsonLog_Internal(ref linesCounter, type, sb, visited, indent + 1, indentStep, withProperties);
                }

                // Fields
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var field in fields)
                {
                    if (field.IsStatic) { continue; }

                    sb.Append(',');
                    NewLine(ref linesCounter, sb, indent + 1, indentStep);
                    sb.Append('"');
                    sb.Append(field.Name);
                    sb.Append('"');
                    sb.Append(':').Append(' ');

                    object fieldValue = field.GetValue(value);
                    ToJsonLog_Internal(ref linesCounter, fieldValue, sb, visited, indent + 1, 2, withProperties);
                }

                if (withProperties)
                {
                    // Properties
                    var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    foreach (var prop in properties)
                    {
                        if (prop.GetIndexParameters().Length > 0 ||
                            prop.GetMethod == null ||
                            prop.GetMethod.IsStatic)
                        {
                            continue;
                        }

                        sb.Append(',');
                        NewLine(ref linesCounter, sb, indent + 1, indentStep);
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
                        ToJsonLog_Internal(ref linesCounter, propValue, sb, visited, indent + 1, indentStep, withProperties);
                    }
                }

                NewLine(ref linesCounter, sb, indent, indentStep);
                sb.Append('}');
            }

            //visited.Remove(value);
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
