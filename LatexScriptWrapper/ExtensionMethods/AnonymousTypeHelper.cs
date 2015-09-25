using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace ExtensionMethods
{
    public static class AnonymousTypeHelper
    {
        public static object ToExpandoObjectIfNecessary(this object obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            var type = obj.GetType();
            if (!type.IsAnonymousType())
                return obj;

            var expandoObj = new ExpandoObject() as IDictionary<string, object>;

            foreach (var property in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var value = property.GetValue(obj, null);
                if (value == null)
                    expandoObj.Add(property.Name, value);
                else
                    expandoObj.Add(property.Name, value.ToExpandoObjectIfNecessary());
            }

            return expandoObj as ExpandoObject;
        }

        public static Boolean IsAnonymousType(this Type type)
        {
            Boolean hasCompilerGeneratedAttribute = type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Count() > 0;
            Boolean nameContainsAnonymousType = type.FullName.Contains("AnonymousType");
            Boolean isAnonymousType = hasCompilerGeneratedAttribute && nameContainsAnonymousType;

            return isAnonymousType;
        }
    }
}
