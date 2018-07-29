using System;
using System.Windows.Forms;

namespace ShenzhenMod
{
    public partial class Form1 : Form
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(Form1));

        private ShenzhenLocator m_locator;

        public Form1(string platformName)
        {
            m_locator = new ShenzhenLocator(platformName);
            InitializeComponent();

            this.label4.Text = m_locator.GetUIString("LocateShenzhenFolder");
            this.label7.Text = m_locator.GetUIString("SaveFilesHint");
            m_exeFolderField.Text = m_locator.FindShenzhenDirectory();
        }

        private void BrowseButtonClick(object sender, System.EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.RootFolder = Environment.SpecialFolder.MyComputer;
                dialog.SelectedPath = m_exeFolderField.Text;
                dialog.Description = m_locator.GetUIString("LocateShenzhenFolderWithHint");
                var result = dialog.ShowDialog(this);
                if (result == DialogResult.OK)
                {
                    m_exeFolderField.Text = dialog.SelectedPath;
                }
            }
        }

        private void InstallButtonClick(object sender, System.EventArgs e)
        {
            try
            {
                string shenzhenDir = m_exeFolderField.Text;
                sm_log.InfoFormat("Installing to \"{0}\"", shenzhenDir);
                new Installer(shenzhenDir).Install();

                sm_log.Info("Installation complete");
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
