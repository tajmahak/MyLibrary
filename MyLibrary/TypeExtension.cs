using System;
using System.Reflection;

namespace MyLibrary
{
    public class TypeExtension
    {
        public static T GetAttribute<T>(Type type, bool inherit = false) where T : Attribute
        {
            object[] attrs = type.GetCustomAttributes(typeof(T), inherit);
            return GetAttribute<T>(attrs);
        }

        public static T GetAttribute<T>(Assembly assembly, bool inherit = false) where T : Attribute
        {
            object[] attrs = assembly.GetCustomAttributes(typeof(T), inherit);
            return GetAttribute<T>(attrs);
        }

        private static T GetAttribute<T>(object[] attrs) where T : Attribute
        {
            foreach (object attrObject in attrs)
            {
                if (attrObject is T attr)
                {
                    return (T)attrObject;
                }
            }
            return default;
        }
    }
}
