using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace AsyncWaiter
{
    public partial class ListenForm : Form
    {
        BindingList<CallingMethod> methods;

        public ListenForm() { InitializeComponent(); }

        internal ListenForm(BindingList<CallingMethod> methods) : this()
        {
            this.methods = methods;
            dataGridView1.DataSource = methods;
        }

        void toolStripButton1_Click(object sender, EventArgs e) { }

        void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridView1.Columns[e.ColumnIndex] is DataGridViewButtonColumn && e.RowIndex > -1)
            {
                dataGridView1[2, e.RowIndex].Value = 1;
            }
        }
    }
}