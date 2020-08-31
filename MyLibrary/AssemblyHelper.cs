using System;
using System.Reflection;

namespace MyLibrary
{
    public static class AssemblyHelper
    {
        public static string GetAssemblyName(Assembly assembly)
        {
            AssemblyName assemblyName = assembly.GetName();
            return assemblyName.Name;
        }

        public static string GetProductName(Assembly assembly)
        {
            AssemblyProductAttribute attr = AttributeHelper.GetAttribute<AssemblyProductAttribute>(assembly);
            return attr.Product;
        }

        public static string GetAssemblyTitle(Assembly assembly)
        {
            AssemblyTitleAttribute attr = AttributeHelper.GetAttribute<AssemblyTitleAttribute>(assembly);
            return attr.Title;
        }

        public static Version GetAssemblyVersion(Assembly assembly)
        {
            AssemblyName assemblyName = assembly.GetName();
            Version version = assemblyName.Version;
            return version;
        }

        public static string GetVersionText(Version version)
        {
            return $"{version.Major}.{version.Minor:00}";
        }

        public static string GetVersionText(Assembly assembly)
        {
            Version version = GetAssemblyVersion(assembly);
            return GetVersionText(version);
        }

        public static DateTime GetReleaseDate(Version version)
        {
            DateTime date = new DateTime(2000, 1, 1);
            date = date.AddDays(version.Build);
            date = date.AddSeconds(version.Revision * 2);
            return date;
        }

        public static DateTime GetReleaseDate(Assembly assembly)
        {
            Version version = GetAssemblyVersion(assembly);
            return GetReleaseDate(version);
        }
    }
}
