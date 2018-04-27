using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ReportBL;

namespace CallReport
{
    public interface IMainForm
    {
        string FilePath { get; }
        event EventHandler FileOpenClick;
        event EventHandler GenerateShortReport;
        event EventHandler GenerateExtendetReport;
        Object Content { get; set; }
        string Separator { get; }
        bool IsReshetCall { get; }
    }
    public partial class MainForm : Form, IMainForm
    {
        public MainForm()
        {
            InitializeComponent();
            butOpenFile.Click += ButOpenFile_Click;
            butShortReport.Click += ButShortReport_Click;
            butSelectFile.Click += ButSelectFile_Click;
            butExtendetReport.Click += ButExtendetReport_Click;
            separatorComboBox.SelectedIndex = 0;// Set Comma for default separator 
            
        }

      



        #region Проброс событий
        private void ButShortReport_Click(object sender, EventArgs e)
        {
            if (GenerateShortReport != null) GenerateShortReport(this, EventArgs.Empty);
        }
        private void ButExtendetReport_Click(object sender, EventArgs e)
        {
            if (GenerateExtendetReport != null) GenerateExtendetReport(this, EventArgs.Empty);
        }

        private void ButOpenFile_Click(object sender, EventArgs e)
        {
            if (FileOpenClick != null) FileOpenClick(this, EventArgs.Empty);
        }
       
        #endregion
        #region IMainForm
        
        public string FilePath
        {
            get { return fldFilePath.Text; }
    
        }

        public Object Content
        {
            get
            {
                
                return dataGridView.DataSource;
            }

            set
            {
                dataGridView.DataSource = value;
            }
        }

        public string Separator
        {
            get
            {
                return separatorComboBox.Text;
            }
                       
        }

        public bool IsReshetCall
        {
            get
            {
                return isReshetCall.Checked;
            }
        }

        public event EventHandler FileOpenClick;
        public event EventHandler GenerateShortReport;
        public event EventHandler GenerateExtendetReport;
        #endregion

        private void ButSelectFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "CSV files|*.csv";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                fldFilePath.Text = dlg.FileName;
                if (FileOpenClick != null) FileOpenClick(this, EventArgs.Empty);
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }
    }
}
