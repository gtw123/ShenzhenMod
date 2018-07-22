using System;
using Mono.Cecil;
using ShenzhenMod.Patches;

namespace ShenzhenMod
{
    public class ShenzhenPatcher : IDisposable
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(ShenzhenPatcher));

        private ModuleDefinition m_module;

        public ShenzhenPatcher(string unpatchedPath)
        {
            sm_log.InfoFormat("Reading module \"{0}\"", unpatchedPath);
            m_module = ModuleDefinition.ReadModule(unpatchedPath);
        }

        public void Dispose()
        {
            if (m_module != null)
            {
                m_module.Dispose();
                m_module = null;
            }
        }

        public void ApplyPatches()
        {
            new IncreaseMaxBoardSize(m_module).Apply();
            new AddBiggerSandbox(m_module).Apply();
        }

        /// <summary>
        /// Saves the patched executable to disk.
        /// </summary>
        public void SavePatchedFile(string targetFile)
        {
            sm_log.InfoFormat("Saving patched file to \"{0}\"", targetFile);
            m_module.Write(targetFile);
        }
    }
}
 
 