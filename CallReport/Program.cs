using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ReportBL;

namespace CallReport
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            MainForm form = new MainForm();
            MessageService service = new MessageService();
            Report report = new Report();
            MainPresenter presenter = new MainPresenter(form, report, service);
            Application.Run(form);
        }
    }
}
