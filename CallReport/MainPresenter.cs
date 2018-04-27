using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReportBL;
using System.Data;

namespace CallReport
{
    public class MainPresenter
    {
        private readonly IMainForm _view;
        private readonly IReportBL _report;
        private readonly IMessageService _messageService;
        private string _currentFilePath;
        DataTable csvData = new DataTable();
        public MainPresenter(IMainForm view, IReportBL report, IMessageService service)
        {
            _view = view;
            _report = report;
            _messageService = service;
            _view.FileOpenClick += _view_FileOpenClick;
            _view.GenerateShortReport += _view_GenerateShortReport;
            _view.GenerateExtendetReport += _view_GenerateExtendetReport;
        }

        private void _view_GenerateExtendetReport(object sender, EventArgs e)
        {

            if (_view.IsReshetCall)
            {
                _view.Content = _report.RCExtendetRoport(csvData);
            }
            else
            {
                _view.Content = _report.ExtendetRoport(ref csvData);
            }
        }

        private void _view_GenerateShortReport(object sender, EventArgs e)
        {
            if (_view.IsReshetCall)
            {
                _view.Content = _report.RCShortReport(csvData);
            }
            else
            {
                _view.Content = _report.ShortReport(ref csvData);
            }
                
        }

        private void _view_FileOpenClick(object sender, EventArgs e)
        {
            try
            {
                string filePath = _view.FilePath;
                bool isExist = _report.IsExist(filePath);
                if (!isExist)
                {
                    _messageService.ShowExclamation("The file is doesn't exist");
                    return;
                }
                _currentFilePath = filePath;
                
                _view.Content = csvData = _report.Parser(filePath, _view.Separator);
            }
            catch (Exception ex)
            {
                _messageService.ShowError(ex.Message);
            }
            
        }
    }
}
