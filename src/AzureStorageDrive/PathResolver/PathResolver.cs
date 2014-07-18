using Microsoft.WindowsAzure.Storage.File;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AzureStorageDrive
{
    public class PathResolver
    {
        private const string SharePattern = @"^[a-z0-9][a-z0-9-]{2,}$";
        private const string FilePatter = @"^[^*/]+";
        public const string PathSeparator = "/";
        public const string AlternatePathSeparator = "\\";
        public const string Root = "/";

        public static Dictionary<string, AbstractDriveInfo> Drives = new Dictionary<string, AbstractDriveInfo>();

        public static bool ValidatePath(List<string> parts)
        {
            if (parts.Count == 0) 
            {
                return true;
            }
            
            if (!Regex.Match(parts[0], SharePattern).Success)
            {
                return false;
            }

            for (var i = 1; i < parts.Count; ++i)
            {
                if (!Regex.Match(parts[i], FilePatter).Success) {
                    return false;
                }
            }

            return true;
        }

        public static List<string> SplitPath(string path)
        {
            var list = new List<string>();
            if (path != null)
            {
                //path = path.TrimStart(Root[0]);
                path = path.Trim().Replace(PathResolver.AlternatePathSeparator, PathResolver.PathSeparator);
                var parts = path.Split(new char[] { PathResolver.PathSeparator[0] }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    if (p == "." || string.IsNullOrEmpty(p))
                    {
                        continue;
                    }
                    else if (p == "..")
                    {
                        if (list.Count() > 0)
                        {
                            list.RemoveAt(list.Count() - 1);
                        }
                    }
                    else
                    {
                        list.Add(p);
                    }
                }
            }

            return list;
        }

        public static string GetSubpath(string path)
        {
            var l = SplitPath(path);
            if (l.Count > 0)
            {
                l.RemoveAt(0);
            }

            return string.Join(PathResolver.PathSeparator, l.ToArray());
        }

        public static string Combine(IEnumerable<string> parts)
        {
            return Combine(parts.ToArray());
        }

        public static string Combine(params string[] parts)
        {
            return string.Join(PathSeparator, 
                (from p in parts.Where(s => !string.IsNullOrEmpty(s))
                    select p.Trim(PathSeparator[0], AlternatePathSeparator[0])).ToArray());
        }


        internal static string NormalizePath(string path)
        {
            if (path == null)
            {
                return PathResolver.Root;
            }

            return path.Replace(AlternatePathSeparator.First(), PathResolver.PathSeparator.First());
        }

        internal static bool IsLocalPath(string path)
        {
            return Regex.IsMatch(path, @"^\\[a-zA-Z]\$");
        }

        internal static string ConvertToRealLocalPath(string path)
        {
            path = path.Replace('$', ':');
            if (path.StartsWith(PathResolver.Root))
            {
                path = path.Substring(PathResolver.Root.Length);
            }

            return path;
        }
    }
}
