using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.IO;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using static System.FormattableString;

namespace ShenzhenMod
{
    public class ShenzhenLocator
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(ShenzhenLocator));

        private Dictionary<string, string> m_uiStrings;
        private Func<IEnumerable<string>> m_getShenzhenSearchPaths;

        public ShenzhenLocator(string platformName)
        {
            var platform = Environment.OSVersion.Platform;
            if (platformName == "macos")
            {
                // Unfortunately Mono's version of Environment.OSVersion.Platform returns Unix on macOS, and there's no easy
                // way to tell if we're on macOS. So we just rely on a command-line parameter for now.
                platform = PlatformID.MacOSX;
            }

            switch (platform)
            {
                case PlatformID.Win32NT:
                    {
                        m_uiStrings = new Dictionary<string, string>
                        {
                            ["LocateShenzhenFolder"] = "Locate your SHENZHEN I/O installation folder:",
                            ["LocateShenzhenFolderWithHint"] = @"Please locate your SHENZHEN I/O installation folder. This will usually be  C:\Program Files (x86)\Steam\steamapps\common\SHENZHEN IO",
                            ["SaveFilesHint"] = @"Your save files are normally located in: My Documents\My Games\SHENZHEN IO\",
                        };

                        m_getShenzhenSearchPaths = GetWindowsSearchPaths;
                        break;
                    }
                case PlatformID.Unix:
                    {
                        m_uiStrings = new Dictionary<string, string>
                        {
                            ["LocateShenzhenFolder"] = "Locate your SHENZHEN I/O installation directory:",
                            ["LocateShenzhenFolderWithHint"] = @"Please locate your SHENZHEN I/O installation directory. This will usually be $HOME/.steam/steam/steamapps/common/SHENZHEN IO/",
                            ["SaveFilesHint"] = @"Your save files are normally located in: $HOME/.local/share/SHENZHEN IO",
                        };

                        m_getShenzhenSearchPaths = GetLinuxSearchPaths;
                        break;
                    }
                case PlatformID.MacOSX:
                    {
                        m_uiStrings = new Dictionary<string, string>
                        {
                            ["LocateShenzhenFolder"] = "Locate the SHENZHEN I/O application:",
                            ["LocateShenzhenFolderWithHint"] = @"Please locate your SHENZHEN I/O application. This will usually be in your Applications folder or in ~/Library/Application Support/Steam/SteamApps/common/",
                            ["SaveFilesHint"] = @"Your save files are normally located in: ~/Library/Application Support/SHENZHEN IO/",
                        };

                        m_getShenzhenSearchPaths = GetMacSearchPaths;
                        break;
                    }
                default:
                    throw new Exception("Unsupported platform: " + Environment.OSVersion.Platform);
            }
        }

        public string GetUIString(string token)
        {
            return m_uiStrings.TryGetValue(token, out string uiString) ? uiString : $"<Unknown token '{token}'>";
        }

        public string FindShenzhenDirectory()
        {
            foreach (string dir in m_getShenzhenSearchPaths())
            {
                if (Directory.Exists(dir))
                {
                    sm_log.InfoFormat("Found SHENZHEN I/O directory: \"{0}\"", dir);
                    return dir;
                }
                else
                {
                    sm_log.InfoFormat("Did not find SHENZHEN I/O directory: \"{0}\"", dir);
                }
            }

            sm_log.WarnFormat("Could not find SHENZHEN I/O directory");
            return null;
        }

        private IEnumerable<string> GetWindowsSearchPaths()
        {
            string steamPath = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "InstallPath", null) as string;
            sm_log.InfoFormat("Steam install path: \"{0}\"", steamPath);
            if (steamPath != null)
            {
                yield return Path.Combine(steamPath, "steamapps", "common", "SHENZHEN IO");
            }

            string gogPath = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\GOG.com\Games\1640205738", "PATH", null) as string;
            sm_log.InfoFormat("GoG install path: \"{0}\"", gogPath);
            if (gogPath != null)
            {
                yield return gogPath;
            }
        }

        private IEnumerable<string> GetLinuxSearchPaths()
        {
            yield return Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".steam/steam/steamapps/common/SHENZHEN IO");
        }

        private IEnumerable<string> GetMacSearchPaths()
        {
            yield return Path.Combine(Environment.GetEnvironmentVariable("HOME"), "Library/Application Support/Steam/SteamApps/common/SHENZHEN IO/Shenzhen IO.app");
            yield return $"/Applications/Shenzhen IO.app";
        }

        public static string FindUnpatchedShenzhenExecutable(string shenzhenDir)
        {
            sm_log.InfoFormat("Fetching the latest list of unpatched hashes");

            WebRequest webRequest = WebRequest.Create("https://github.com/gtw123/ShenzhenMod/blob/master/hashes.json");
            WebResponse response = webRequest.GetResponse();

            if (((HttpWebResponse)response).StatusDescription != "OK")
            {
                throw new Exception(Invariant($"Cannot fetch hash list. Response: \"{((HttpWebResponse)response).StatusDescription}\""));
            }

            string[] unpatchedHashes;

            using (Stream data = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(data);
                string json = reader.ReadToEnd();
                unpatchedHashes = JArray.Parse(json).ToObject<string[]>();
            }

            response.Close();

            sm_log.InfoFormat("Looking for unpatched SHENZHEN I/O executable in \"{0}\"", shenzhenDir);
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
            foreach (string file in Directory.GetFiles(dir, "*.exe", SearchOption.AllDirectories))
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