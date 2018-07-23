using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ShenzhenMod.Patches
{
    /// <summary>
    /// Patches the code to increase the maximum simulation speed. Also adds a wider "playback speed"
    /// slider to the sandbox window so the user has more fine-grained control over the simulation speed.
    /// </summary>
    public class IncreaseMaxSpeed
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(IncreaseMaxSpeed));

        private string m_shenzhenDir;

        private ModuleDefinition m_module;
        private MethodDefinition m_updateMethod;

        private const int EXTRA_WIDTH = 90;

        public IncreaseMaxSpeed(ModuleDefinition module, string shenzhenDir)
        {
            m_shenzhenDir = shenzhenDir;
                
            m_module = module;
            m_updateMethod = m_module.FindMethod("GameLogic/CircuitEditorScreen", "#=qpYhEvKVxcLZ8qs1HauZIRQ==");
        }

        public void Apply()
        {
            sm_log.Info("Applying patch");

            AdjustMaxSpeed();
            AdjustControls();
            AdjustSandboxPanelTexture();
        }

        /// <summary>
        /// Increases the maximum simulation speed for the sandbox.
        /// </summary>
        private void AdjustMaxSpeed()
        {
            m_updateMethod.FindInstruction(OpCodes.Ldc_R4, 30f).Operand = 100f;
        }

        /// <summary>
        /// Adjusts the controls at the bottom of the screen to accomodate a larger slider.
        /// </summary>
        private void AdjustControls()
        {
            // Increase the maximum allowed X position of the playback speed slider
            m_updateMethod.FindInstruction(OpCodes.Ldc_I4, 706).Operand = 706 + EXTRA_WIDTH;

            // Move the "Show Wires" and "Hide Signals" buttons to the right
            foreach (var instr in m_updateMethod.FindInstructions(OpCodes.Ldc_R4, 850f, 2))
            {
                instr.Operand = 850f + EXTRA_WIDTH;
            }
        }

        /// <summary>
        /// Creates a new version of the sandbox panel texture, with a wider space for the playback
        /// speed slider.
        /// </summary>
        private void AdjustSandboxPanelTexture()
        {
            string newTextureName = "panel_sandbox_wide";

            MakeNewTexture();
            AdjustTextureName();

            void MakeNewTexture()
            {
                string editorPath = Path.Combine(m_shenzhenDir, @"Content\textures\editor");
                string panelTexturePath = Path.Combine(editorPath, "panel_sandbox.png");
                using (var image = Image.FromFile(panelTexturePath))
                {
                    using (var newImage = new Bitmap(image.Width + EXTRA_WIDTH, image.Height))
                    {
                        using (var graphics = Graphics.FromImage(newImage))
                        {
                            graphics.CompositingMode = CompositingMode.SourceCopy;
                            graphics.DrawImageUnscaled(image, new Point(0, 0));

                            // Copy the part just to the right of the "-" sign
                            int startX = 640;
                            var sourceRect = new Rectangle(startX, 0, image.Width - startX, image.Height);
                            graphics.DrawImage(image, startX + EXTRA_WIDTH, 0, sourceRect, GraphicsUnit.Pixel);
                        }

                        string newTexturePath = Path.Combine(editorPath, newTextureName + ".png");
                        newImage.Save(newTexturePath);
                    }
                }
            }

            void AdjustTextureName()
            {
                var method = m_module.FindMethod("#=qP_OYruvcHHy1DUOE9PEtpX9r7_JMX_e00SfXCzZ60Hs=", "#=qcJXbB28Z53jKHYrZi9DF1A==");
                var il = method.Body.GetILProcessor();

                // Change the texture to use our new file
                var instr = method.FindInstruction(OpCodes.Ldc_I4, (int)-1809885311);
                instr.Set(OpCodes.Ldstr, "textures/editor/" + newTextureName);
                il.Remove(instr.Next);
            }
        }
    }
}
