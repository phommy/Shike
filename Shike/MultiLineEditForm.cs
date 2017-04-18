using System;
using System.Windows.Forms;

namespace Shike
{
    public partial class MultiLineEditForm : Form
    {
        public MultiLineEditForm() { InitializeComponent(); }

        public TextBox DocBox { get { return txtDoc; } }

        void btnOK_Click(object sender, EventArgs e) { DialogResult = DialogResult.OK; }

        void btnCancel_Click(object sender, EventArgs e) { DialogResult = DialogResult.Cancel; }

        void MultiLineEditForm_Shown(object sender, EventArgs e)
        {
            txtDoc.ScrollToCaret();
            DocBox.Select(DocBox.TextLength, 0);
        }
    }
}