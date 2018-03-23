using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ExtensionMethods {

    internal static class AnonymousTypeHelper {

        /// <summary>
        /// source: http://stackoverflow.com/questions/2630370/c-sharp-dynamic-cannot-access-properties-from-anonymous-types-declared-in-anot
        /// with little modifications
        ///  - recursive
        ///  - check for anonymous type
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static object ToExpandoObjectIfNecessary(this object obj) {
            if (obj == null)
                throw new ArgumentNullException("obj");
            var type = obj.GetType();
            if (!type.IsAnonymousType())
                return obj;

            var expandoObj = new ExpandoObject() as IDictionary<string, object>;

            foreach (var property in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
                var value = property.GetValue(obj, null);
                if (value == null)
                    expandoObj.Add(property.Name, value);
                else
                    expandoObj.Add(property.Name, value.ToExpandoObjectIfNecessary());
            }

            return expandoObj as ExpandoObject;
        }

        /// <summary>
        /// Determine through reflection if this type is an compiler generated anonymous type
        ///
        /// There is no C# language construct which allows you to say
        /// "Is this an anonymous type". You can use a simple heuristic
        /// to approximate if a type is an anonymous type, but it's possible
        /// to get tricked by people hand coding IL, or using a language where
        /// such characters as > and < are valid in identifiers.
        ///
        /// source: http://stackoverflow.com/questions/2483023/how-to-test-if-a-type-is-anonymous
        /// </summary>
        /// <param name="type">the type to check</param>
        /// <returns>true if the passed type is a compiler generated anonymous type</returns>
        /// <remarks>This code is copied from http://www.liensberger.it/web/blog/?p=191 </remarks>
        public static bool IsAnonymousType(this Type type) {
            if (type == null)
                throw new ArgumentNullException("type");

            // TODO: The only way to detect anonymous types right now.
            return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
                   && type.IsGenericType
                   && type.Name.Contains("AnonymousType")
                   && (type.Name.StartsWith("<>", StringComparison.OrdinalIgnoreCase) || type.Name.StartsWith("VB$", StringComparison.OrdinalIgnoreCase))
                   && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }
    }
}