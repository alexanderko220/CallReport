using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Data;
using Microsoft.VisualBasic.FileIO;
using System.Windows.Forms;
using System.Text;
using System.Text.RegularExpressions;

namespace ReportBL
{
    public interface IReportBL
    {
        bool IsExist(string filePath);
        string[] GetContent(string filePath);
        DataTable Parser(string path, string separator);
        DataTable ShortReport(ref DataTable csvData);
        DataTable ExtendetRoport(ref DataTable csvData);
        DataTable RCShortReport(DataTable csvData);
        DataTable RCExtendetRoport(DataTable csvData);

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


        //Counting Unique numbers
        private Dictionary<string, int> CountingUniqueNumbers(DataTable csvData, string fieldName)
        {
            Dictionary<string, int> destinations = new Dictionary<string, int>();

            foreach (var destination in csvData.AsEnumerable())
            {
                if (destinations.ContainsKey(destination.Field<string>(fieldName)))
                {
                    destinations[destination.Field<string>(fieldName)]++;
                }
                else
                {
                    destinations.Add(destination.Field<string>(fieldName), 1);
                }
            }
            return destinations;
        }


        //Shot Report
        public DataTable ShortReport(ref DataTable csvData)
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

            //create a table to display the results
            DataTable displayData = new DataTable("CallReport");
            displayData.Columns.Add("Provider", typeof(string));
            displayData.Columns.Add("Total calls", typeof(int));
            displayData.Columns.Add("Answered calls", typeof(int));
            displayData.Columns.Add("Answered calls > 00:00:59", typeof(int));
            displayData.Columns.Add("ASR", typeof(string));
            displayData.Columns.Add("ASR > 00:00:59", typeof(string));
            displayData.Columns.Add("Average call duration", typeof(string));
            displayData.Columns.Add("Average call duration > 00:00:59", typeof(string));
            displayData.Columns.Add("Unique Numbers", typeof(string));
            displayData.Columns.Add("Unique Numbers > 00:00:59", typeof(string));

            //Counting calls in general
            var allCalls = (from result in csvData.AsEnumerable()
                            where result.Field<string>(columnProvider) != ""
                            select result).Count();
            var answeredCalls = (from result in csvData.AsEnumerable()
                                 where result.Field<string>(columnDuratin) != timeFormat
                                 select result).Count();
            //ASR (Answer Seizure Ratio)
            string asr = string.Format("{0:f2}%\n", (double)answeredCalls / allCalls * 100);

            //Counting call with more than a minute duration
            TimeSpan time =  TimeSpan.Parse("00:00:59");
            var answeredMoreMinuteCalls = (from result in csvData.AsEnumerable()
                                 where TimeSpan.Parse(result.Field<string>(columnDuratin)) > time
                                 select result).Count();
            //ASR (Answer Seizure Ratio)
            string asr2 = string.Format("{0:f2}%\n", (double)answeredMoreMinuteCalls / allCalls * 100);

            //Counting Unique numbers
            Dictionary<string, int> destinations = CountingUniqueNumbers(csvData, "Destination");
                       
            string uniqueNumbers = string.Format("{0:f2}%\n", (double)destinations.Count / allCalls * 100);

            //Counting Unique numbers with duration more than minute
            Dictionary<string, int> destinationsMoreMinute = new Dictionary<string, int>();

            foreach (var detin in csvData.AsEnumerable())
            {
                if (TimeSpan.Parse(detin.Field<string>(columnDuratin)) > time)
                {
                    if (destinationsMoreMinute.ContainsKey(detin.Field<string>("Destination")))
                    {
                        destinationsMoreMinute[detin.Field<string>("Destination")]++;
                    }
                    else
                    {
                        destinationsMoreMinute.Add(detin.Field<string>("Destination"), 1);
                    }
                }
            }

            string uniqueNumbersMoreMinute = string.Format("{0:f2}%\n", (double)destinationsMoreMinute.Count / answeredMoreMinuteCalls * 100);

            //Convert and put in the array the time of call
            List<TimeSpan> totalTime = new List<TimeSpan>();
            List<TimeSpan> totalTimeMoreMinute = new List<TimeSpan>();
            foreach (var timeValue in csvData.AsEnumerable())
            {
                totalTime.Add(TimeSpan.Parse(timeValue.Field<string>(columnDuratin)));
                if(TimeSpan.Parse(timeValue.Field<string>(columnDuratin)) > time)
                {
                    totalTimeMoreMinute.Add(TimeSpan.Parse(timeValue.Field<string>(columnDuratin)));
                }
            }
            TimeSpan totalTimeDuration = new TimeSpan();
            TimeSpan totalTimeDurationMoreMinute = new TimeSpan();
            foreach (var timeValue in totalTime)
            {
                totalTimeDuration += timeValue;
            }
            foreach (var timeValue in totalTimeMoreMinute)
            {
                totalTimeDurationMoreMinute += timeValue;
            }

            //average call duration
            TimeSpan avgTimeDuration = TimeSpan.FromMinutes(totalTimeDuration.TotalMinutes / allCalls);
            TimeSpan avgTimeDurationMoreMinute;
            if (answeredMoreMinuteCalls > 0)
            {
                avgTimeDurationMoreMinute = TimeSpan.FromMinutes(totalTimeDurationMoreMinute.TotalMinutes / answeredMoreMinuteCalls);
            }
            else
            {
                avgTimeDurationMoreMinute = new TimeSpan(00, 00, 00);
            }

            //add result to table
            displayData.Rows.Add("All", allCalls, answeredCalls, answeredMoreMinuteCalls, asr,asr2, avgTimeDuration.ToString(@"hh\:mm\:ss"), avgTimeDurationMoreMinute.ToString(@"hh\:mm\:ss"), uniqueNumbers, uniqueNumbersMoreMinute);

            //getting a list of provider's name
            List<string> providerNames = GetProvidersNames(columnProvider, ref csvData);

            //counting calls by non voicenter providers*************************************************************************************************

            for (int i = 0; i < providerNames.Count; i++)
            {
                allCalls = (from result in csvData.AsEnumerable()
                            where result.Field<string>(columnProvider).Equals(providerNames[i])
                            select result).Count();
                answeredCalls = (from result in csvData.AsEnumerable()
                                 where result.Field<string>(columnProvider).Equals(providerNames[i])
                                 && result.Field<string>(columnDuratin) != timeFormat
                                 select result).Count();
                asr = string.Format("{0:f2}%\n", (double)answeredCalls / allCalls * 100);

                //counting calls by non voicenter providers > minute duration
                answeredMoreMinuteCalls = (from result in csvData.AsEnumerable()
                                           where TimeSpan.Parse(result.Field<string>(columnDuratin)) > time 
                                           & result.Field<string>(columnProvider).Equals(providerNames[i])
                                           select result).Count();

                asr2 = string.Format("{0:f2}%\n", (double)answeredMoreMinuteCalls / allCalls * 100);

                //Counting Unique numbers
                destinations.Clear();
                destinationsMoreMinute.Clear();
                foreach (var destination in csvData.AsEnumerable())
                {
                    if (destination.Field<string>(columnProvider).Equals(providerNames[i]))
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
                    if (TimeSpan.Parse(destination.Field<string>(columnDuratin)) > time
                        & destination.Field<string>(columnProvider).Equals(providerNames[i]))
                    {
                        if (destinationsMoreMinute.ContainsKey(destination.Field<string>("Destination")))
                        {
                            destinationsMoreMinute[destination.Field<string>("Destination")]++;
                        }
                        else
                        {
                            destinationsMoreMinute.Add(destination.Field<string>("Destination"), 1);
                        }
                    }

                }
                
                uniqueNumbers = string.Format("{0:f2}%\n", (double)destinations.Count / allCalls * 100);

                if (answeredMoreMinuteCalls > 0)
                {
                    uniqueNumbersMoreMinute = string.Format("{0:f2}%\n", (double)destinationsMoreMinute.Count / answeredMoreMinuteCalls * 100);
                }
                else
                {
                    uniqueNumbersMoreMinute = string.Format("{0:f2}%\n", "0");
                }
                    
                //Convert and put in array the total time of call
                totalTime.Clear();
                totalTimeMoreMinute.Clear();
                foreach (var timeValue in csvData.AsEnumerable())
                {
                    if (timeValue.Field<string>(columnProvider) == providerNames[i])
                        totalTime.Add(TimeSpan.Parse(timeValue.Field<string>(columnDuratin)));
                    if (TimeSpan.Parse(timeValue.Field<string>(columnDuratin)) > time
                        & timeValue.Field<string>(columnProvider) == providerNames[i])
                    {
                        totalTimeMoreMinute.Add(TimeSpan.Parse(timeValue.Field<string>(columnDuratin)));
                    }
                }
                totalTimeDuration = new TimeSpan(00, 00, 00);
                totalTimeDurationMoreMinute = new TimeSpan(00, 00, 00);
                foreach (var timeValue in totalTime)
                {
                    totalTimeDuration += timeValue;
                }
                foreach (var timeValue in totalTimeMoreMinute)
                {
                    totalTimeDurationMoreMinute += timeValue;
                }
                //average call duration by Providers 
                avgTimeDuration = TimeSpan.FromMinutes(totalTimeDuration.TotalMinutes / allCalls);
                if(answeredMoreMinuteCalls > 0)
                {
                    avgTimeDurationMoreMinute = TimeSpan.FromMinutes(totalTimeDurationMoreMinute.TotalMinutes / answeredMoreMinuteCalls);
                }
                else
                {
                    avgTimeDurationMoreMinute = new TimeSpan(00, 00, 00);
                }
                //displaying data                
                displayData.Rows.Add(providerNames[i], allCalls, answeredCalls, answeredMoreMinuteCalls, asr, asr2, avgTimeDuration.ToString(@"hh\:mm\:ss"), avgTimeDurationMoreMinute.ToString(@"hh\:mm\:ss"), uniqueNumbers, uniqueNumbersMoreMinute);

            }
            //Counting calls for Voicenter provider*****************************************************************************************************************
            allCalls = (from result in csvData.AsEnumerable()
                        where result.Field<string>(columnProvider).Contains("Voicenter")
                        select result).Count();
            answeredCalls = (from result in csvData.AsEnumerable()
                             where result.Field<string>(columnProvider).Contains("Voicenter")
                             && result.Field<string>(columnDuratin) != timeFormat
                             select result).Count();
            asr = string.Format("{0:f2}%\n",(double)answeredCalls / allCalls * 100);

            answeredMoreMinuteCalls = (from result in csvData.AsEnumerable()
                                       where TimeSpan.Parse(result.Field<string>(columnDuratin)) > time
                                       & result.Field<string>(columnProvider).Contains("Voicenter")
                                       select result).Count();
            asr2 = string.Format("{0:f2}%\n", (double)answeredMoreMinuteCalls / allCalls * 100);

            //Uniques numbers for Voicenter
            destinations.Clear();
            destinationsMoreMinute.Clear();
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
                if (TimeSpan.Parse(destination.Field<string>(columnDuratin)) > time
                        & destination.Field<string>(columnProvider).Contains("Voicenter"))
                {
                    if (destinationsMoreMinute.ContainsKey(destination.Field<string>("Destination")))
                    {
                        destinationsMoreMinute[destination.Field<string>("Destination")]++;
                    }
                    else
                    {
                        destinationsMoreMinute.Add(destination.Field<string>("Destination"), 1);
                    }
                }
            }
            uniqueNumbers = string.Format("{0:f2}%\n", (double)destinations.Count / allCalls * 100);
            if (answeredMoreMinuteCalls > 0)
            {
                uniqueNumbersMoreMinute = string.Format("{0:f2}%\n", (double)destinationsMoreMinute.Count / answeredMoreMinuteCalls * 100);
            }
            else
            {
                uniqueNumbersMoreMinute = string.Format("{0:f2}%\n", "0");
            }

            //Convert and put in the array the time of call
            totalTime.Clear();
            totalTimeMoreMinute.Clear();
            foreach (var timeValue in csvData.AsEnumerable())
            {
                if (timeValue.Field<string>(columnProvider).Contains("Voicenter"))
                    totalTime.Add(TimeSpan.Parse(timeValue.Field<string>(columnDuratin)));
                if (TimeSpan.Parse(timeValue.Field<string>(columnDuratin)) > time
                        & timeValue.Field<string>(columnProvider).Contains("Voicenter"))
                {
                    totalTimeMoreMinute.Add(TimeSpan.Parse(timeValue.Field<string>(columnDuratin)));
                }
            }
            totalTimeDuration = new TimeSpan(00, 00, 00);
            totalTimeDurationMoreMinute = new TimeSpan(00, 00, 00);
            foreach (var timeValue in totalTime)
            {
                totalTimeDuration += timeValue;
            }
            foreach (var timeValue in totalTimeMoreMinute)
            {
                totalTimeDurationMoreMinute += timeValue;
            }
            //average call duration for calls via  Voicenter 
            avgTimeDuration = TimeSpan.FromMinutes(totalTimeDuration.TotalMinutes / allCalls);
            if (answeredMoreMinuteCalls > 0)
            {
                avgTimeDurationMoreMinute = TimeSpan.FromMinutes(totalTimeDurationMoreMinute.TotalMinutes / answeredMoreMinuteCalls);
            }
            else
            {
                avgTimeDurationMoreMinute = new TimeSpan(00, 00, 00);
            }
            //displaying data
            displayData.Rows.Add("Voicenter", allCalls, answeredCalls, answeredMoreMinuteCalls, asr, asr2, avgTimeDuration.ToString(@"hh\:mm\:ss"), 
                avgTimeDurationMoreMinute.ToString(@"hh\:mm\:ss"), uniqueNumbers, uniqueNumbersMoreMinute);
            


            return displayData;
        }

        public DataTable RCShortReport(DataTable csvData)
        {

            DataTable displayData = new DataTable("RC_CallReport");
            displayData.Columns.Add("Total calls", typeof(int));
            displayData.Columns.Add("Answered calls", typeof(int));
            displayData.Columns.Add("Answered calls > 00:00:59", typeof(int));
            displayData.Columns.Add("ASR", typeof(string));
            displayData.Columns.Add("ASR calls > 00:00:59", typeof(string));
            displayData.Columns.Add("Average call duration", typeof(string));
            displayData.Columns.Add("Average call duration > 00:00:59", typeof(string));
            displayData.Columns.Add("Unique numbers", typeof(string));
            displayData.Columns.Add("Unique numbers > 00:00:59", typeof(string));

            TimeSpan time = TimeSpan.FromMilliseconds(59000);// 00:00:59
           
            //Counting calls in general
            var allCalls = (from result in csvData.AsEnumerable()
                            where result.Field<string>("call_type") == "outbound"
                            select result).Count();
            var answeredCalls = (from result in csvData.AsEnumerable()
                                 where result.Field<string>("answered") == "t" & result.Field<string>("call_type") == "outbound"
                                 select result).Count();
           
            var answeredCallsMoreMinute = (from result in csvData.AsEnumerable()
                                               where result.Field<string>("answered") == "t" & result.Field<string>("call_type") == "outbound"
                                               & result.Field<string>("talkms") != "" 
                                               select result into t where TimeSpan.FromMilliseconds(double.Parse(t.Field<string>("talkms"))) > time
                                               select t ).Count();
            
            //ASR (Answer Seizure Ratio)
            string asr = string.Format("{0:f2}%\n", (double)answeredCalls / allCalls * 100);
            string asr2 = string.Format("{0:f2}%\n", (double)answeredCallsMoreMinute / allCalls * 100);

            //Convert and put in the array the time of call
            List<TimeSpan> totalTime = new List<TimeSpan>();
            List<TimeSpan> totalTimeMoreMinute = new List<TimeSpan>();

            foreach (var timeValue in csvData.AsEnumerable())
            {
                if (timeValue.Field<string>("answered") == "t" & timeValue.Field<string>("talkms") != "")
                    totalTime.Add(TimeSpan.FromMilliseconds(double.Parse(timeValue.Field<string>("talkms"))));

                if (timeValue.Field<string>("answered") == "t" & timeValue.Field<string>("talkms") != "")
                    
                {
                    if(TimeSpan.FromMilliseconds(double.Parse(timeValue.Field<string>("talkms"))) > time)
                    totalTimeMoreMinute.Add(TimeSpan.FromMilliseconds(double.Parse(timeValue.Field<string>("talkms"))));
                }
            }
            TimeSpan totalTimeDuration = new TimeSpan();
            TimeSpan totalTimeDurationMoreMinute = new TimeSpan();
            foreach (var timeValue in totalTime)
            {
                totalTimeDuration += timeValue;
            }
            foreach (var timeValue in totalTime)
            {
                totalTimeDurationMoreMinute += timeValue;
            }
            TimeSpan avgTimeDuration = TimeSpan.FromMinutes(totalTimeDuration.TotalMinutes / allCalls);
            TimeSpan avgTimeDurationMoreMinute = TimeSpan.FromMinutes(totalTimeDurationMoreMinute.TotalMinutes / answeredCallsMoreMinute);
            
            //Calculating unique numbers
            Dictionary<string, int> destinations = new Dictionary<string, int>();
            Dictionary<string, int> destinationsMoreMinute = new Dictionary<string, int>();
            string pattern = @"^[0-3]{2}";//find all numbers beginning on 01, 02 or 03

            foreach (var destination in csvData.AsEnumerable())
            {
                if (Regex.IsMatch(destination.Field<string>("dst_num"), pattern))
                {
                    if (destination.Field<string>("answered") == "t" & destination.Field<string>("talkms") != "")
                    {
                        if(TimeSpan.FromMilliseconds(double.Parse(destination.Field<string>("talkms"))) > time)
                        {
                            char[] replaceNumbers = new char[destination.Field<string>("dst_num").Length - 2];
                            destination.Field<string>("dst_num").CopyTo(2, replaceNumbers, 0, destination.Field<string>("dst_num").Length - 2);
                            string numbers = new string(replaceNumbers);
                            if (destinationsMoreMinute.ContainsKey(numbers))
                            {
                                destinationsMoreMinute[numbers]++;
                            }
                            else
                            {
                                destinationsMoreMinute.Add(numbers, 1);
                            }
                        }
                        
                    }

                    char[] replaceNumber = new char[destination.Field<string>("dst_num").Length - 2];
                    destination.Field<string>("dst_num").CopyTo(2, replaceNumber, 0, destination.Field<string>("dst_num").Length - 2);
                    string number = new string(replaceNumber);
                    if (destinations.ContainsKey(number))
                    {
                        destinations[number]++;
                    }
                    else
                    {
                        destinations.Add(number, 1);
                    }
                }
                else
                {
                    if (destination.Field<string>("answered") == "t" & destination.Field<string>("talkms") != "")
                    {
                        if (TimeSpan.FromMilliseconds(double.Parse(destination.Field<string>("talkms"))) > time)
                        {
                            if (destinationsMoreMinute.ContainsKey(destination.Field<string>("dst_num")))
                            {
                                destinationsMoreMinute[destination.Field<string>("dst_num")]++;
                            }
                            else
                            {
                                destinationsMoreMinute.Add(destination.Field<string>("dst_num"), 1);
                            }
                        }
                    }

                    if (destinations.ContainsKey(destination.Field<string>("dst_num")))
                    {
                        destinations[destination.Field<string>("dst_num")]++;
                    }
                    else
                    {
                        destinations.Add(destination.Field<string>("dst_num"), 1);
                    }
                }

            }

            string uniqueNumbers = string.Format("{0:f2}%\n", (double)destinations.Count / allCalls * 100);
            string uniqueNumbersMoreMinute = string.Format("{0:f2}%\n", (double)destinationsMoreMinute.Count / answeredCallsMoreMinute * 100);
            //displaying data
            displayData.Rows.Add(allCalls, answeredCalls, answeredCallsMoreMinute, asr,asr2 ,avgTimeDuration.ToString(@"hh\:mm\:ss"),
             avgTimeDurationMoreMinute.ToString(@"hh\:mm\:ss"), uniqueNumbers, uniqueNumbersMoreMinute);
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
                string asr = string.Format("{0:f2}%\n", (double)successfulCalls / totalCalls * 100);
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
                    asr = string.Format("{0:f2}%\n", (double)answeredCalls / allCalls * 100);

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
                    asr = string.Format("{0:f2}%\n", (double)answeredVoicenterCalls / allVoicenterCalls * 100);

                    displayedData.Rows.Add(item.Key, "Voicenter", allVoicenterCalls, answeredVoicenterCalls, asr, null);
                }


            }
            return displayedData;
        }


        // Extendet RC Report
        public DataTable RCExtendetRoport(DataTable csvData)
        {
            //ExtREport
            // Group by Prefix (countries)
            var result = from item in csvData.AsEnumerable()
                         where item.Field<string>("a2") != "" & item.Field<string>("call_type") == "outbound"
                         group item by item.Field<string>("a2");

            //creating dataTable to view report
            DataTable displayedData = new DataTable("RC_Ext_Report");
            displayedData.Columns.Add("Prefix", typeof(string));
            displayedData.Columns.Add("Total calls", typeof(int));
            displayedData.Columns.Add("Answered calls", typeof(int));
            displayedData.Columns.Add("ASR", typeof(string));
            displayedData.Columns.Add("Avarege call duration", typeof(string));
            displayedData.Columns.Add("Unique numbers", typeof(string));


            // Counting Unique numbers in general
            foreach (var item in result)
            {
                //Counting calls in general
                var allCalls = (from resultCalls in item.AsEnumerable()
                                where resultCalls.Field<string>("call_type") == "outbound"
                                select resultCalls).Count();
                var answeredCalls = (from resultCalls in item.AsEnumerable()
                                     where resultCalls.Field<string>("answered") == "t" & resultCalls.Field<string>("call_type") == "outbound"
                                     select resultCalls).Count();
                //ASR (Answer Seizure Ratio)
                string asr = string.Format("{0:f2}%\n", (double)answeredCalls / allCalls * 100);


                //Convert and put in the array the time of call
                List<TimeSpan> totalTime = new List<TimeSpan>();

                foreach (var timeValue in item.AsEnumerable())
                {
                    if (timeValue.Field<string>("answered") == "t")
                        totalTime.Add(TimeSpan.FromMilliseconds(double.Parse(timeValue.Field<string>("talkms"))));
                }
                TimeSpan totalTimeDuration = new TimeSpan();
                foreach (var timeValue in totalTime)
                {
                    totalTimeDuration += timeValue;
                }
                TimeSpan avgTimeDuration = TimeSpan.FromMinutes(totalTimeDuration.TotalMinutes / allCalls);


                //Calculating unique numbers
                Dictionary<string, int> destinations = new Dictionary<string, int>();
                string pattern = @"^[0-3]{2}";
                foreach (var prefix in item)
                {
                    if (Regex.IsMatch(prefix.Field<string>("dst_num"), pattern))
                    {
                        char[] replaceNumber = new char[prefix.Field<string>("dst_num").Length - 2];
                        prefix.Field<string>("dst_num").CopyTo(2, replaceNumber, 0, prefix.Field<string>("dst_num").Length - 2);
                        string number = new string(replaceNumber);
                        if (destinations.ContainsKey(number))
                        {
                            destinations[number]++;
                        }
                        else
                        {
                            destinations.Add(number, 1);
                        }
                    }
                    else
                    {
                        if (destinations.ContainsKey(prefix.Field<string>("dst_num")))
                        {
                            destinations[prefix.Field<string>("dst_num")]++;
                        }
                        else
                        {
                            destinations.Add(prefix.Field<string>("dst_num"), 1);
                        }
                    }
                }
                string uniqueNumbers = string.Format("{0:f2}%\n", (double)destinations.Count / allCalls * 100);

                //displaing data
                displayedData.Rows.Add(item.Key, allCalls, answeredCalls, asr, avgTimeDuration.ToString(@"hh\:mm\:ss"), uniqueNumbers);
                
            }
            return displayedData;
        }
    }
}
