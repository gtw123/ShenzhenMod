using System;
using System.Linq;
using System.Security.Cryptography;
using System.IO;
using Microsoft.Win32;
using static System.FormattableString;

namespace ShenzhenMod
{
    public static class ShenzhenLocator
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(ShenzhenLocator));

        public static string FindShenzhenDirectory()
        {
            return FindShenzhenSteamDirectory() ?? FindShenzhenGoGDirectory();
        }

        public static string FindShenzhenSteamDirectory()
        {
            string steamPath = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "InstallPath", null) as string;
            sm_log.InfoFormat("Steam install path: \"{0}\"", steamPath);
            if (steamPath != null)
            {
                var shenzhenDir = Path.Combine(steamPath, @"steamapps\common\SHENZHEN IO");
                if (Directory.Exists(shenzhenDir))
                {
                    sm_log.InfoFormat("Found SHENZHEN I/O directory: \"{0}\"", shenzhenDir);
                    return shenzhenDir;
                }
                else
                {
                    sm_log.WarnFormat("Could not find SHENZHEN I/O directory: directory \"{0}\" does not exist", shenzhenDir);
                }
            }

            return null;
        }

        public static string FindShenzhenGoGDirectory()
        {
            string shenzhenDir = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\GOG.com\Games\1640205738", "PATH", null) as string;
            sm_log.InfoFormat("GoG install path: \"{0}\"", shenzhenDir);
            if (shenzhenDir != null)
            {
                if (Directory.Exists(shenzhenDir))
                {
                    sm_log.InfoFormat("Found SHENZHEN I/O directory: \"{0}\"", shenzhenDir);
                    return shenzhenDir;
                }
                else
                {
                    sm_log.WarnFormat("Could not find SHENZHEN I/O directory: directory \"{0}\" does not exist", shenzhenDir);
                }
            }

            return null;
        }

        public static string FindUnpatchedShenzhenExecutable(string shenzhenDir)
        {
            sm_log.InfoFormat("Looking for unpatched SHENZHEN I/O executable in \"{0}\"", shenzhenDir);
            var unpatchedHashes = new[] {
                "1D-65-40-5A-63-82-77-4F-2E-99-F2-00-B0-59-9B-4D-B3-71-2B-1B-C1-01-8A-4B-D6-02-C4-7A-8B-11-DB-E8",  // Steam
                "02-DB-AC-A8-8C-27-4E-A2-8F-A2-7E-D5-92-72-08-9C-F7-5A-DB-B7-5A-88-64-B9-54-05-55-51-7F-6D-56-F6"   // GoG
            };
            string path = FindExecutableWithHash(shenzhenDir, unpatchedHashes);
            if (path == null)
            {
                throw new Exception(Invariant($"Cannot locate unpatched SHENZHEN I/O executable in \"{shenzhenDir}\""));
            }

            sm_log.InfoFormat("Found unpatched SHENZHEN I/O executable: \"{0}\"", path);
            return path;
        }

        private static string FindExecutableWithHash(string dir, string[] expectedHashes)
        {
            foreach (string file in Directory.GetFiles(dir, "*.exe"))
            {
                try
                {
                    string hash = CalculateHash(file);
                    if (expectedHashes.Contains(hash))
                    {
                        return file;
                    }
                }
                catch (Exception e)
                {
                    sm_log.ErrorFormat("Error calculating hash for file \"{0}\": {1}", file, e.Message);
                }
            }

            return null;
        }

        private static string CalculateHash(string file)
        {
            sm_log.InfoFormat("Calculating hash for \"{0}\"", file);
            using (var stream = File.OpenRead(file))
            {
                var sha = SHA256.Create();
                byte[] hash = sha.ComputeHash(stream);
                string hashString = BitConverter.ToString(hash);

                sm_log.InfoFormat("Calculated hash: \"{0}\"", hashString);
                return hashString;
            }
        }
    }
}