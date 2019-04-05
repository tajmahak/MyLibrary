using System;
using System.IO;

namespace MyLibrary.Data
{
    public static class PathExtension
    {
        public static string ReplaceWrongChars(string value, params char[] chars)
        {
            foreach (var c in chars)
                value = value.Replace(c, '_');
            return value;
        }
        public static string GetCorrectPath(string directoryPath, string fileName)
        {
            // преобразование пути из переменного окружения
            directoryPath = Environment.ExpandEnvironmentVariables(directoryPath);

            // удаление неиспользуемых символов
            directoryPath = ReplaceWrongChars(directoryPath, Path.GetInvalidPathChars());
            fileName = ReplaceWrongChars(fileName, Path.GetInvalidFileNameChars());

            directoryPath = Path.GetFullPath(directoryPath);
            var path = Path.Combine(directoryPath, fileName);

            if (path.Length > 259)
            {
                // обрезка имени файла до нужной длины
                var fileNameExt = Path.GetExtension(fileName);
                fileName = Path.GetFileNameWithoutExtension(fileName);

                path = Path.Combine(directoryPath, fileName);
                path = path.Substring(0, 259 - fileNameExt.Length);
                path += fileNameExt;
            }

            return path;
        }
        public static void CreateDirectory(string filePath)
        {
            var directoryPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
        }
    }
}
