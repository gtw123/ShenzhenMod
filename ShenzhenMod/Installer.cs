using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Reflection;
using Mono.Cecil;
using ShenzhenMod.Patches;
using static System.FormattableString;
using ShenzhenMod.Patching;

namespace ShenzhenMod
{
    public class Installer
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(Installer));

        private string m_shenzhenDir;

        public Installer(string shenzhenDir)
        {
            if (String.IsNullOrEmpty(shenzhenDir))
            {
                throw new Exception("No SHENZHEN I/O directory specified");
            }

            m_shenzhenDir = shenzhenDir;
        }

        public void Install()
        {
            sm_log.Info("Finding unpatched SHENZHEN I/O executable");
            string exePath = ShenzhenLocator.FindUnpatchedShenzhenExecutable(m_shenzhenDir);
            string exeDir = Path.GetDirectoryName(exePath);

            if (Path.GetFileName(exePath).Equals("Shenzhen.exe", StringComparison.OrdinalIgnoreCase))
            {
                string backupPath = Path.Combine(exeDir, "Shenzhen.Unpatched.exe");
                sm_log.InfoFormat("Backing up unpatched executable \"{0}\" to \"{1}\"", exePath, backupPath);
                File.Copy(exePath, backupPath,  overwrite: true);
            }

            string patchedPath = Path.Combine(exeDir, "Shenzhen.Patched.exe");
            if (File.Exists(patchedPath))
            {
                sm_log.InfoFormat("Deleting existing patched file \"{0}\"", patchedPath);
                File.Delete(patchedPath);
            }

            ApplyPatches(exePath, patchedPath);

            string targetPath = Path.Combine(exeDir, "Shenzhen.exe");
            if (File.Exists(targetPath))
            {
                // Rename the existing Shenzhen.exe before overwriting it, in case the user wants to roll back
                string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
                string backupPath = Path.Combine(exeDir, Invariant($"Shenzhen.{timestamp}.exe"));
                sm_log.InfoFormat("Moving \"{0}\" to \"{1}\"", targetPath, backupPath);
                File.Move(targetPath, backupPath);
            }

            sm_log.InfoFormat("Moving \"{0}\" to \"{1}\"", patchedPath, targetPath);
            File.Move(patchedPath, targetPath);
        }

        private void ApplyPatches(string unpatchedPath, string patchedPath)
        {
            sm_log.InfoFormat("Reading module \"{0}\"", unpatchedPath);

            using (var patcher = new Patcher(Assembly.GetExecutingAssembly(), unpatchedPath))
            {
                patcher.InjectMembers();

                var module = patcher.TargetModule;

                sm_log.Info("Locating types");
                var types = new ShenzhenTypes(module);

                sm_log.Info("Applying patches");
                string exeDir = Path.GetDirectoryName(unpatchedPath);
                new IncreaseMaxBoardSize(types).Apply();
                new AddBiggerSandbox(patcher, types, exeDir).Apply();
                new AdjustPlaybackSpeedSlider(types, exeDir).Apply();

                if (bool.TryParse(ConfigurationManager.AppSettings["IncreaseMaxSpeed"], out bool increaseMaxSpeed) && increaseMaxSpeed)
                {
                    new IncreaseMaxSpeed(types).Apply();
                }

                sm_log.InfoFormat("Saving patched file to \"{0}\"", patchedPath);
                module.Write(patchedPath);
            }
        }
    }
}
