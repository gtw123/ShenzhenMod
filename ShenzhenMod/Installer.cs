using System;
using System.Globalization;
using System.IO;
using Mono.Cecil;
using ShenzhenMod.Patches;
using static System.FormattableString;

namespace ShenzhenMod
{
    public class Installer
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(Installer));

        private string m_shenzhenDir;

        public Installer(string shenzhenDir)
        {
            m_shenzhenDir = shenzhenDir;
        }

        public void Install()
        {
            sm_log.Info("Finding unpatched SHENZHEN I/O executable");
            string exePath = ShenzhenLocator.FindUnpatchedShenzhenExecutable(m_shenzhenDir);

            if (Path.GetFileName(exePath).Equals("Shenzhen.exe", StringComparison.OrdinalIgnoreCase))
            {
                string backupPath = Path.Combine(m_shenzhenDir, "Shenzhen.Unpatched.exe");
                sm_log.InfoFormat("Backing up unpatched executable \"{0}\" to \"{1}\"", exePath, backupPath);
                File.Copy(exePath, backupPath,  overwrite: true);
            }

            string patchedPath = Path.Combine(m_shenzhenDir, "Shenzhen.Patched.exe");
            if (File.Exists(patchedPath))
            {
                sm_log.InfoFormat("Deleting existing patched file \"{0}\"", patchedPath);
                File.Delete(patchedPath);
            }

            ApplyPatches(exePath, patchedPath);

            string targetPath = Path.Combine(m_shenzhenDir, "Shenzhen.exe");
            if (File.Exists(targetPath))
            {
                // Rename the existing Shenzhen.exe before overwriting it, in case the user wants to roll back
                string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
                string backupPath = Path.Combine(m_shenzhenDir, Invariant($"Shenzhen.{timestamp}.exe"));
                sm_log.InfoFormat("Moving \"{0}\" to \"{1}\"", targetPath, backupPath);
                File.Move(targetPath, backupPath);
            }

            sm_log.InfoFormat("Moving \"{0}\" to \"{1}\"", patchedPath, targetPath);
            File.Move(patchedPath, targetPath);
        }

        private void ApplyPatches(string unpatchedPath, string patchedPath)
        {
            sm_log.InfoFormat("Reading module \"{0}\"", unpatchedPath);
            using (var module = ModuleDefinition.ReadModule(unpatchedPath))
            {
                sm_log.Info("Locating types");
                var types = new ShenzhenTypes(module);

                sm_log.Info("Applying patches");
                new IncreaseMaxBoardSize(types).Apply();
                new AddBiggerSandbox(types, m_shenzhenDir).Apply();
                new IncreaseMaxSpeed(types, m_shenzhenDir).Apply();

                sm_log.InfoFormat("Saving patched file to \"{0}\"", patchedPath);
                module.Write(patchedPath);
            }
        }
    }
}
