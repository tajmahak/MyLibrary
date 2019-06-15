using System;
using System.IO;

namespace MyLibrary.Data
{
    public static class PathExtension
    {
        public static string ReplaceWrongChars(string value, params char[] chars)
        {
            foreach (var c in chars)
            {
                value = value.Replace(c, '_');
            }

            return value;
        }
        public static string GetCorrectPath(string directoryPath, string fileName)
        {
            // преобразование пути из переменного окружения
            directoryPath = Environment.ExpandEnvironmentVariables(directoryPath);

            // удаление неиспользуемых символов
            directoryPath = ReplaceWrongChars(directoryPath, Path.GetInvalidPathChars());

            directoryPath = Path.GetFullPath(directoryPath);
            if (fileName == null)
            {
                return directoryPath;
            }
            else
            {
                fileName = ReplaceWrongChars(fileName, Path.GetInvalidFileNameChars());

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
        }
        /// <summary>
        /// Создает все каталоги и подкаталоги, указанные в параметре path
        /// </summary>
        /// <param name="path">Путь к создаваемому каталогу</param>
        /// <param name="isFilePath">Путь к создаваемому каталогу содержить путь к файлу</param>
        public static void CreateDirectory(string path, bool isFilePath)
        {
            if (isFilePath)
            {
                path = Path.GetDirectoryName(path);
            }
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
