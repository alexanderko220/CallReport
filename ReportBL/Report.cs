using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Data;
using Microsoft.VisualBasic.FileIO;
using System.Windows.Forms;
using System.Text;

namespace ReportBL
{
    public interface IReportBL
    {
        bool IsExist(string filePath);
        string[] GetContent(string filePath);
        DataTable Parser(string path, string separator);
        DataTable ShortReport(ref DataTable csvData);
        DataTable ExtendetRoport(ref DataTable csvData);

    }
    public class Report : IReportBL
    {
        public bool IsExist(string filePath)
        {
            bool isExist = File.Exists(filePath);
            return isExist;
        }
        public string[] GetContent(string filePath)
        {
            string[] reader = System.IO.File.ReadAllLines(filePath); ;
            return reader;
        }


        //splitting CSV file
        public DataTable Parser(string path, string separator)
        {
            DataTable csvData = new DataTable();
            using (TextFieldParser parser = new TextFieldParser(path))
            {
                string separationValue = string.Empty;
                switch (separator)
                {
                    case "Comma":
                        separationValue = ",";
                        break;
                    case "Colon":
                        separationValue = ":";
                        break;
                    case "Tabulation":
                        separationValue = "\t";
                        break;
                    case "Semicolon":
                        separationValue = ";";
                        break;
                }
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(separationValue);
                string[] colFields = parser.ReadFields();//чтенеие первой строки с именами столбцов

                DataColumn[] dateColumnsNames = new DataColumn[colFields.Length]; //создание стлбцов

                for (int i = 0; i < colFields.Length; i++)
                {
                    // названия колонок в таблице
                    dateColumnsNames[i] = new DataColumn(colFields[i]);
                    dateColumnsNames[i].AllowDBNull = true; //допускает null значения  
                    csvData.Columns.Add(dateColumnsNames[i]);

                }
                while (!parser.EndOfData)
                {
                    string[] fieldRow = parser.ReadFields();
                    csvData.Rows.Add(fieldRow);

                }
            }
            return csvData;
        }
        //getting the time format (0:00:00 or 00:00:00) 
        private string GetDurationFormat(string columnDuratin, ref DataTable csvData)
        {
            string timeFormat = string.Empty;
            foreach (var rows in csvData.AsEnumerable())
            {
                if (rows.Field<string>(columnDuratin).Equals("00:00:00"))
                {
                    timeFormat = "00:00:00";
                    break;
                }
                else if (rows.Field<string>(columnDuratin).Equals("0:00:00"))
                {
                    timeFormat = "0:00:00";
                    break;
                }
            }
            return timeFormat;
        }

        // getting a list of provider name
        private List<string> GetProvidersNames(string columnProvider, ref DataTable csvData)
        {
            List<string> allProviders = new List<string>();
            foreach (var value in csvData.AsEnumerable())
            {
                
                if (!allProviders.Contains(value.Field<string>(columnProvider)) && !value.Field<string>(columnProvider).Contains("Voicenter")
                    && !value.Field<string>(columnProvider).Equals("") && !value.Field<string>(columnProvider).Equals(columnProvider))
                {
                    allProviders.Add(value.Field<string>(columnProvider));
                    
                }

            }
            
            return allProviders;
        }
       
        //Shot Report
        public DataTable ShortReport(ref DataTable csvData )
        {
            string columnProvider = string.Empty;
            string columnDuratin = string.Empty;
            string timeFormat = string.Empty;
            
            for (int i = 0; i < csvData.Columns.Count; i++)
            {
                if (csvData.Columns[i].ColumnName.Contains("Duration"))
                {
                    columnDuratin = csvData.Columns[i].ColumnName;
                   
                }
                   
                if (csvData.Columns[i].ColumnName.Contains("Provider"))
                {
                    columnProvider = csvData.Columns[i].ColumnName;
                }
                   
            }
            //getting the time format (0:00:00 or 00:00:00) 
            timeFormat = GetDurationFormat(columnDuratin, ref csvData);

            //Counting calls in general
            var allCalls = (from result in csvData.AsEnumerable()
                              where result.Field<string>(columnProvider) != ""
                              select result).Count();
            var answeredCalls = (from result in csvData.AsEnumerable()
                                             where result.Field<string>(columnDuratin) != timeFormat 
                                             select result).Count();
            //ASR (Answer Seizure Ratio)
            string asr = string.Format("{0:f2}%\n",(double)answeredCalls / allCalls * 100);
            //Counting Unique numbers
            Dictionary<string, int> destinations = new Dictionary<string, int>();
           
            foreach (var destination in csvData.AsEnumerable())
            {
                if (destinations.ContainsKey(destination.Field<string>("Destination")))
                {
                    destinations[destination.Field<string>("Destination")]++;
                }
                else
                {
                    destinations.Add(destination.Field<string>("Destination"), 1);
                }
            }
            
            string uniqueNumbers = string.Format("{0:f2}%\n", (double)destinations.Count / allCalls * 100);

            //Convert and put in the array the time of call
            List<TimeSpan> totalTime = new List<TimeSpan>();
            foreach (var timeValue in csvData.AsEnumerable())
            {
                totalTime.Add(TimeSpan.Parse(timeValue.Field<string>(columnDuratin)));
            }
            TimeSpan totalTimeDuration = new TimeSpan();
            foreach (var timeValue in totalTime)
            {
                totalTimeDuration += timeValue;
            }
            //average call duration
            TimeSpan avgTimeDuration = TimeSpan.FromMinutes(totalTimeDuration.TotalMinutes / allCalls);

            //create a table to display the results
            DataTable displayData = new DataTable("CallReport");
            displayData.Columns.Add("Provider", typeof(string));
            displayData.Columns.Add("Total Calls", typeof(int));
            displayData.Columns.Add("Successful Calls", typeof(int));
            displayData.Columns.Add("ASR", typeof(string));
            displayData.Columns.Add("Averege Time Duration", typeof(string));
            displayData.Columns.Add("Unique Numbers", typeof(string));
            //add result to table
            displayData.Rows.Add("All", allCalls, answeredCalls, asr, avgTimeDuration.ToString(@"hh\:mm\:ss"), uniqueNumbers);

            //getting a list of provider name
            List<string> providerNames = GetProvidersNames(columnProvider, ref csvData);

            //counting calls by non voicenter providers
            for (int i = 0; i < providerNames.Count; i++)
            {
                allCalls = (from result in csvData.AsEnumerable()
                              where result.Field<string>(columnProvider).Equals(providerNames[i])
                              select result).Count();
                answeredCalls = (from result in csvData.AsEnumerable()
                                             where result.Field<string>(columnProvider).Equals(providerNames[i]) 
                                             &&  result.Field<string>(columnDuratin) != timeFormat 
                                             select result).Count();
                asr = string.Format("{0:f2}%\n",(double)answeredCalls / allCalls * 100);
                //Uniques
                destinations.Clear();
                foreach (var destination in csvData.AsEnumerable())
                {
                    if ( destination.Field<string>(columnProvider).Equals(providerNames[i]))
                    {
                        if (destinations.ContainsKey(destination.Field<string>("Destination")))
                        {
                            destinations[destination.Field<string>("Destination")]++;
                        }
                        else
                        {
                            destinations.Add(destination.Field<string>("Destination"), 1);
                        }
                    }
                    
                }
                uniqueNumbers = string.Format("{0:f2}%\n", (double)destinations.Count / allCalls * 100);
                //Convert and put in array the time of call
                totalTime.Clear();
                foreach (var timeValue in csvData.AsEnumerable())
                {
                    if (timeValue.Field<string>(columnProvider) == providerNames[i])
                    totalTime.Add(TimeSpan.Parse(timeValue.Field<string>(columnDuratin)));
                }
                totalTimeDuration = new TimeSpan(00,00,00);
                foreach (var timeValue in totalTime)
                {
                    totalTimeDuration += timeValue;
                }
                
                //average call duration by Providers 
                avgTimeDuration = TimeSpan.FromMinutes(totalTimeDuration.TotalMinutes / allCalls);
                displayData.Rows.Add(providerNames[i], allCalls, answeredCalls, asr, avgTimeDuration.ToString(@"hh\:mm\:ss"),uniqueNumbers);
                
            }
            //Counting calls for Voicenter provider
            allCalls = (from result in csvData.AsEnumerable()
                          where result.Field<string>(columnProvider).Contains("Voicenter")
                          select result).Count();
            answeredCalls = (from result in csvData.AsEnumerable()
                                         where result.Field<string>(columnProvider).Contains("Voicenter") 
                                         && result.Field<string>(columnDuratin) != timeFormat
                                         select result).Count();
            asr = string.Format("{0:f2}%\n",
            (double)answeredCalls / allCalls * 100, allCalls, answeredCalls);
            //Uniques numbers for Voicenter
            destinations.Clear();
            foreach (var destination in csvData.AsEnumerable())
            {
                if (destination.Field<string>(columnProvider).Contains("Voicenter"))
                {
                    if (destinations.ContainsKey(destination.Field<string>("Destination")))
                    {
                        destinations[destination.Field<string>("Destination")]++;
                    }
                    else
                    {
                        destinations.Add(destination.Field<string>("Destination"), 1);
                    }
                }
            }
            uniqueNumbers = string.Format("{0:f2}%\n", (double)destinations.Count / allCalls * 100);
            //Convert and put in the array the time of call
            totalTime.Clear();
            foreach (var timeValue in csvData.AsEnumerable())
            {
                if (timeValue.Field<string>(columnProvider).Contains("Voicenter"))
                    totalTime.Add(TimeSpan.Parse(timeValue.Field<string>(columnDuratin)));
            }
            totalTimeDuration = new TimeSpan(00, 00, 00);
            foreach (var timeValue in totalTime)
            {
                totalTimeDuration += timeValue;
            }
            //average call duration for calls via  Voicenter 
            avgTimeDuration = TimeSpan.FromMinutes(totalTimeDuration.TotalMinutes / allCalls);
            displayData.Rows.Add("Voicenter", allCalls, answeredCalls, asr, avgTimeDuration.ToString(@"hh\:mm\:ss"), uniqueNumbers);


            return displayData;
        }
        // Extendet Report
        public DataTable ExtendetRoport(ref DataTable csvData)
        {

            string columnProvider = string.Empty;
            string columnDuratin = string.Empty;
            string timeFormat = string.Empty;

            for (int i = 0; i < csvData.Columns.Count; i++)
            {
                if (csvData.Columns[i].ColumnName.Contains("Duration"))
                {
                    columnDuratin = csvData.Columns[i].ColumnName;

                }

                if (csvData.Columns[i].ColumnName.Contains("Provider"))
                {
                    columnProvider = csvData.Columns[i].ColumnName;
                }

            }
            //getting the time format (0:00:00 or 00:00:00) 
            timeFormat = GetDurationFormat(columnDuratin, ref csvData);


            //ExtREport
            // Group by Prefix (countries)
            var result = from item in csvData.AsEnumerable()
                         where item.Field<string>("Provider") != ""
                         group item by item.Field<string>("Prefix");

            //creating dataTable to view report
            DataTable displayedData = new DataTable("myTable");
            displayedData.Columns.Add("Prefix", typeof(string));
            displayedData.Columns.Add("Provider", typeof(string));
            displayedData.Columns.Add("Total Calls", typeof(int));
            displayedData.Columns.Add("Successful Calls", typeof(int));
            displayedData.Columns.Add("ASR", typeof(string));
            displayedData.Columns.Add("Unique numbers", typeof(string));
            // Counting Unique numbers in general
            foreach (var item in result)
            {
                Dictionary<string, int> destinations = new Dictionary<string, int>();
                int successfulCalls = 0;
                int totalCalls = 0;

                foreach (var i in item)
                {
                    if (destinations.ContainsKey(i.Field<string>("Destination")))
                    {
                        destinations[i.Field<string>("Destination")]++;
                    }
                    else
                    {
                        destinations.Add(i.Field<string>("Destination"), 1);
                    }
                    if (i.Field<string>(columnDuratin) != timeFormat)
                    {
                        successfulCalls++; //All Calls
                    }
                    totalCalls++; // Answered Calls 
                }
                //ASR (Answer Seizure Ratio)
                string asr = string.Format("{0:f2}%\n",(double)successfulCalls / totalCalls * 100);
                string uniqueNumbers = string.Format("{0:f2}%\n", (double)destinations.Count / totalCalls * 100);
                //Add to result Table
                displayedData.Rows.Add(item.Key, "All", totalCalls, successfulCalls, asr, uniqueNumbers);

                //Getting Providers Names
                List<string> providersNames = new List<string>();
                foreach (var value in item.AsEnumerable())
                {

                    if (!providersNames.Contains(value.Field<string>(columnProvider)) && !value.Field<string>(columnProvider).Contains("Voicenter")
                        && !value.Field<string>(columnProvider).Equals("") && !value.Field<string>(columnProvider).Equals(columnProvider))
                    {
                        providersNames.Add(value.Field<string>(columnProvider));
                    }

                }

                // Counting calls by providers
                for (int i = 0; i < providersNames.Count; i++)
                {
                   
                    var allCalls = (from total in item.AsEnumerable()
                                           where total.Field<string>(columnProvider).Equals(providersNames[i])
                                           select total).Count();
                    var answeredCalls = (from total in item.AsEnumerable()
                                                          where total.Field<string>(columnProvider).Equals(providersNames[i]) && 
                                                          total.Field<string>(columnDuratin) != timeFormat
                                                          select total).Count();
                    asr = string.Format("{0:f2}%\n",(double)answeredCalls / allCalls * 100);

                    displayedData.Rows.Add(item.Key, providersNames[i], allCalls, answeredCalls, asr, null);
                    
                }

                //Counting calls for Voicenter provider
                var allVoicenterCalls = (from total in item.AsEnumerable()
                                      where total.Field<string>(columnProvider).Contains("Voicenter")
                                      select total).Count();
                    var answeredVoicenterCalls = (from total in item.AsEnumerable()
                                                     where total.Field<string>(columnProvider).Contains("Voicenter") &&
                                                     total.Field<string>(columnDuratin) != timeFormat
                                                     select total).Count();
                if (allVoicenterCalls > 0)
                {
                    asr = string.Format("{0:f2}%\n",(double)answeredVoicenterCalls / allVoicenterCalls * 100);

                    displayedData.Rows.Add(item.Key, "Voicenter", allVoicenterCalls, answeredVoicenterCalls, asr, null);
                }
                

            }
            return displayedData;
        }
    }
}
