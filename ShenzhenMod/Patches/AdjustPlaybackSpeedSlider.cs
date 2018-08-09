using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using Mono.Cecil.Cil;
using ShenzhenMod.Patching;

namespace ShenzhenMod.Patches
{
    /// <summary>
    /// Adds a wider "playback speed" slider to the sandbox window so the user has more fine-grained control over
    /// the simulation speed.
    /// </summary>
    public class AdjustPlaybackSpeedSlider
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(AdjustPlaybackSpeedSlider));

        private string m_shenzhenDir;
        private ShenzhenTypes m_types;

        private const int EXTRA_WIDTH = 90;

        public AdjustPlaybackSpeedSlider(ShenzhenTypes types, string shenzhenDir)
        {
            m_shenzhenDir = shenzhenDir;
            m_types = types;
        }

        public void Apply()
        {
            sm_log.Info("Applying patch");

            AdjustControls();
            AdjustSandboxPanelTexture();
        }

        /// <summary>
        /// Adjusts the controls at the bottom of the screen to accomodate a larger slider.
        /// </summary>
        private void AdjustControls()
        {
            var updateMethod = m_types.GameLogic.CircuitEditorScreen.Update;

            // Increase the maximum allowed X position of the playback speed slider
            updateMethod.FindInstruction(OpCodes.Ldc_I4, 706).Operand = 706 + EXTRA_WIDTH;

            // Move the "Show Wires" and "Hide Signals" buttons to the right
            foreach (var instr in updateMethod.FindInstructions(OpCodes.Ldc_R4, 850f, 2))
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
                string editorPath = Path.Combine(m_shenzhenDir, "Content", "textures", "editor");
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
                // Since the names of the textures are obfuscated it's difficult to find the one we want.
                // For now we'll use a hard-coded index but we'll check that it matches the field used
                // where CircuitEditorScreen.Update() draws it. This could easily break if the game
                // is updated, but the chance of us getting the wrong texture is very low.
                var panelSandboxTextureField = m_types.TextureManager.Type.Fields[15].FieldType.Resolve().Fields[32];
                m_types.GameLogic.CircuitEditorScreen.Update.FindInstructionAtOffset(0x2775, OpCodes.Ldfld, panelSandboxTextureField); // This will throw if it doesn't match our texture field

                // Find the method that loads all the textures
                var method = m_types.TextureManager.Type.Methods.Single(m => m.Parameters.Count == 1 && m.Parameters[0].ParameterType.ToString() == "System.Action`1<System.Int32>");
                var il = method.Body.GetILProcessor();

                // Change it to use our new file
                // panelSandboxTextureField = LoadTexture(DeobfuscateString(-1809885311))  =>  panelSandboxTextureField = LoadTexture("textures/editor/newTextureName");
                var instr = method.FindInstruction(OpCodes.Stfld, panelSandboxTextureField).Previous.Previous.Previous;
                instr.Set(OpCodes.Ldstr, "textures/editor/" + newTextureName);
                il.Remove(instr.Next);
            }
        }
    }
}
