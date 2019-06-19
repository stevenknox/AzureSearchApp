using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;

namespace AzureSearch
{

    //https://stackoverflow.com/a/10478008/2409660
    public class ObjectDumper
    {
        private int _level;
        private readonly int _indentSize;
        private readonly StringBuilder _stringBuilder;
        private readonly List<int> _hashListOfFoundElements;
        public bool _isHtml { get; set; }

        private ObjectDumper(int indentSize, bool isHtml)
        {
            _indentSize = indentSize;
            _isHtml = isHtml;
            _stringBuilder = new StringBuilder();
            _hashListOfFoundElements = new List<int>();
        }

        public static string DumpHtml(object element)
        {
            return Dump(element, 2, true);
        }
        public static string Dump(object element)
        {
            return Dump(element, 2, false);
        }

        public static string Dump(object element, int indentSize, bool isHtml)
        {
            var instance = new ObjectDumper(indentSize, isHtml);
            
            return instance.DumpElement(element);
        }

        private string DumpElement(object element)
        {
            if (element == null || element is ValueType || element is string)
            {
                Write(FormatValue(element));
            }
            else if(element.GetType() == typeof(JObject))
            {
                foreach (JProperty property in ((JObject)element).Properties())
                {
                    Write("{0}: {1}", property.Name, property.Value);
                }
            }
            else
            {
                var objectType = element.GetType();
                if (!typeof(IEnumerable).IsAssignableFrom(objectType))
                {
                    Write("{{{0}}}", objectType.FullName);
                    _hashListOfFoundElements.Add(element.GetHashCode());
                    _level++;
                }

                var enumerableElement = element as IEnumerable;
                if (enumerableElement != null)
                {
                    foreach (object item in enumerableElement)
                    {
                        if (item is IEnumerable && !(item is string))
                        {
                            _level++;
                            DumpElement(item);
                            _level--;
                        }
                        else
                        {
                            if (!AlreadyTouched(item))
                                DumpElement(item);
                            else
                                Write("{{{0}}} <-- bidirectional reference found", item.GetType().FullName);
                        }
                    }
                }
                else
                {
                    MemberInfo[] members = element.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var memberInfo in members)
                    {
                        var fieldInfo = memberInfo as FieldInfo;
                        var propertyInfo = memberInfo as PropertyInfo;

                        if (fieldInfo == null && propertyInfo == null)
                            continue;

                        var type = fieldInfo != null ? fieldInfo.FieldType : propertyInfo.PropertyType;
                        object value = fieldInfo != null
                                           ? fieldInfo.GetValue(element)
                                           : propertyInfo.GetValue(element, null);

                        if (type.IsValueType || type == typeof(string))
                        {
                            Write("{0}: {1}", memberInfo.Name, FormatValue(value));
                        }
                        else
                        {
                            var isEnumerable = typeof(IEnumerable).IsAssignableFrom(type);
                            Write("{0}: {1}", memberInfo.Name, isEnumerable ? "..." : "{ }");

                            var alreadyTouched = !isEnumerable && AlreadyTouched(value);
                            _level++;
                            if (!alreadyTouched)
                                DumpElement(value);
                            else
                                Write("{{{0}}} <-- bidirectional reference found", value.GetType().FullName);
                            _level--;
                        }
                    }
                }

                if (!typeof(IEnumerable).IsAssignableFrom(objectType))
                {
                    _level--;
                }
            }

            return _stringBuilder.ToString();
        }

        private bool AlreadyTouched(object value)
        {
            if (value == null)
                return false;

            var hash = value.GetHashCode();
            for (var i = 0; i < _hashListOfFoundElements.Count; i++)
            {
                if (_hashListOfFoundElements[i] == hash)
                    return true;
            }
            return false;
        }

        private void Write(string value, params object[] args)
        {
             if(_isHtml) { WriteHtml(value, args); } else {  WriteText(value, args); }
        }
        private void WriteHtml(string value, params object[] args)
        {
            if (args != null)
                value = HtmlItem(_level, args);

            _stringBuilder.Append(value);
        }

        private string HtmlItem(int indent, params object[] args)
        {
            var padding = indent == 0 ? "" : $" style='padding-left: {indent}0px;'";

            switch (args.Length)
            {
                case 1 : return $"<li class='list-group-item'{padding}><strong>{args[0]}</strong></li>";
                case 2 : return $"<li class='list-group-item'{padding}><strong>{args[0]}</strong><br />{args[1].ToString().Replace("\"", "")}</li>"; 
                default: return "";
            }
        }
        

        private void WriteText(string value, params object[] args)
        {
            var space = new string(' ', _level * _indentSize);

            if (args != null)
                value = string.Format(value, args);

            _stringBuilder.AppendLine(space + value);
        }

        private string FormatValue(object o)
        {
            if (o == null)
                return ("null");

            if (o is DateTime)
                return (((DateTime)o).ToShortDateString());

            if (o is string)
                return string.Format("\"{0}\"", o);

            if (o is char && (char)o == '\0')
                return string.Empty;

            if (o is ValueType)
                return (o.ToString());

            if (o is IEnumerable)
                return ("...");

            return ("{ }");
        }

    }
}