using System;
using System.IO;

namespace MyLibrary
{
    public static class PathHelper
    {
        public static string ReplaceWrongChars(string value, params char[] chars)
        {
            foreach (char c in chars)
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

                string path = Path.Combine(directoryPath, fileName);

                if (path.Length > 259)
                {
                    // обрезка имени файла до нужной длины
                    string fileNameExt = Path.GetExtension(fileName);
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

        /// <summary>
        /// Добавление префикса к имени файла
        /// </summary>
        /// <param name="path"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static string AddFileNamePrefix(string path, string prefix)
        {
            string dirPath = Path.GetDirectoryName(path);
            string fileName = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);
            return Path.Combine(dirPath, prefix + fileName + ext);
        }

        /// <summary>
        /// Добавление суффикса к имени файла
        /// </summary>
        /// <param name="path"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public static string AddFileNameSuffix(string path, string suffix)
        {
            string dirPath = Path.GetDirectoryName(path);
            string fileName = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);
            return Path.Combine(dirPath, fileName + suffix + ext);
        }
    }
}
