using System;
using System.Windows.Forms;

namespace ShenzhenMod
{
    public partial class Form1 : Form
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(Form1));

        public Form1()
        {
            InitializeComponent();

            m_exeFolderField.Text = ShenzhenLocator.FindShenzhenDirectory();
        }

        private void BrowseButtonClick(object sender, System.EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.RootFolder = Environment.SpecialFolder.MyComputer;
                dialog.SelectedPath = m_exeFolderField.Text;
                dialog.Description = @"Please locate your SHENZHEN I/O installation folder. This will usually be C:\Program Files (x86)\Steam\steamapps\common\SHENZHEN IO";
                var result = dialog.ShowDialog(this);
                if (result == DialogResult.OK)
                {
                    m_exeFolderField.Text = dialog.SelectedPath;
                }
            }
        }

        private void PatchButtonClick(object sender, System.EventArgs e)
        {
            try
            {
                string shenzhenDir = m_exeFolderField.Text;
                new Installer(shenzhenDir).Install();
                MessageBox.Show(this, "Installation complete!");
                Application.Exit();
            }
            catch (Exception ex)
            {
                string message = "Error patching SHENZHEN I/O." + Environment.NewLine + Environment.NewLine + ex.ToString();
                sm_log.Error(message);
                MessageBox.Show(this, message, "Error");
            }
        }
    }
}
