using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ShenzhenMod.Patches
{
    /// <summary>
    /// Patches the code to increase the maximum simulation speed.
    public class IncreaseMaxSpeed
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(IncreaseMaxSpeed));

        private ShenzhenTypes m_types;

        public IncreaseMaxSpeed(ShenzhenTypes types)
        {
            m_types = types;
        }

        public void Apply()
        {
            sm_log.Info("Applying patch");

            AdjustMaxSpeed();
        }

        /// <summary>
        /// Increases the maximum simulation speed for the sandbox. This also affects the first three test runs
        /// of a normal puzzle.
        /// </summary>
        private void AdjustMaxSpeed()
        {
            m_types.GameLogic.CircuitEditorScreen.Update.FindInstruction(OpCodes.Ldc_R4, 30f).Operand = 100f;
        }
    }
}
