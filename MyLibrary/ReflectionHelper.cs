using System;
using System.Reflection;

namespace MyLibrary
{
    public class ReflectionHelper
    {
        public static T GetAttribute<T>(ICustomAttributeProvider attributeProvider, bool inherit = false) where T : Attribute
        {
            object[] attrs = attributeProvider.GetCustomAttributes(typeof(T), inherit);
            return GetAttribute<T>(attrs);
        }

        public static void SetValue(object obj, string memberName, object value)
        {
            Type type = obj.GetType();
            MemberInfo[] members;
            while (true)
            {
                members = type.GetMember(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (members.Length == 0 && type.BaseType != null)
                {
                    type = type.BaseType;
                    continue;
                }
                break;
            }

            if (members.Length != 1)
            {
                throw new NotImplementedException();
            }

            MemberInfo member = members[0];
            if (member is FieldInfo)
            {
                FieldInfo field = member as FieldInfo;
                field.SetValue(obj, value);
            }
            else if (member is PropertyInfo)
            {
                PropertyInfo property = member as PropertyInfo;
                property.SetValue(obj, value, null);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private static T GetAttribute<T>(object[] attrs) where T : Attribute
        {
            foreach (object attrObject in attrs)
            {
                if (attrObject is T attr)
                {
                    return attr;
                }
            }
            return default;
        }
    }
}
