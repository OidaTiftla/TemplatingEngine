using System;
using System.Collections.Generic;
using System.Text;

namespace ExtensionMethods
{
    public static class EnumerableExtension
    {
        public static string Implode<T>(this IEnumerable<T> list, string sep, string praefix = "", string suffix = "", Func<T, string> toString = null)
        {
            var sb = new StringBuilder();
            sb.Append(praefix);
            string tmp = null;
            foreach (var i in list)
            {
                if (tmp == null)
                    tmp = itemToString(i, toString);
                else
                {
                    sb.Append(tmp + sep);
                    tmp = itemToString(i, toString);
                }
            }
            if (tmp != null)
                sb.Append(tmp);

            sb.Append(suffix);
            return sb.ToString();
        }

        private static string itemToString<T>(T item, Func<T, string> toString = null)
        {
            if (toString == null)
                return item.ToString();
            else
                return toString(item);
        }
    }
}
