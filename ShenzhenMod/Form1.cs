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

        }

        private void PatchButtonClick(object sender, System.EventArgs e)
        {
            try
            {
                string shenzhenDir = m_exeFolderField.Text;
                new Installer(shenzhenDir).Install();
            }
            catch (Exception ex)
            {
                string message = "Error patching SHENZHEN I/O." + Environment.NewLine + Environment.NewLine + ex.ToString();
                sm_log.Error(message);
                MessageBox.Show(message, "Error");
            }
        }
    }
}
