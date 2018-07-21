using System;
using System.Globalization;
using System.IO;
using System.Reflection;
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

            sm_log.Info("Patching executable");
            using (var patcher = new ShenzhenPatcher(exePath))
            {
                patcher.ApplyPatches();
                patcher.SavePatchedFile(patchedPath);
            }

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

            CopyContent();
        }

        private void CopyContent()
        {
            using (var stream = Assembly.GetCallingAssembly().GetManifestResourceStream("ShenzhenMod.Content.messages.en.bigger-prototyping-area.txt"))
            {
                string path = Path.Combine(m_shenzhenDir, @"Content\messages.en\bigger-prototyping-area.txt");
                using (var file = File.Create(path))
                {
                    sm_log.InfoFormat("Writing resource file to \"{0}\"", path);
                    stream.CopyTo(file);
                }

                // Although we haven't got a Chinese version, we need to have a corresponding file in messages.zh to avoid a crash.
                string path2 = Path.Combine(m_shenzhenDir, @"Content\messages.zh\bigger-prototyping-area.txt");
                sm_log.InfoFormat("Copying \"{0}\" to \"{1}\"", path, path2);
                File.Copy(path, path2, overwrite: true);
            }
        }
    }
}
