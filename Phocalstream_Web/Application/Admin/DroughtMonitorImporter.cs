using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Web;
using Phocalstream_Shared.Data.Model.External;
using Phocalstream_Shared.Data;
using Microsoft.Practices.Unity;

namespace Phocalstream_Web.Application.Admin
{
                
    public class DroughtMonitorImporter
    {

        private readonly IDroughtMonitorRepository _repo; 

        private static DroughtMonitorImporter _instance;
        private bool _importRunning;
        private String _firstDate;
        private String _lastDate;

        public bool ImportRunning
        {
            get { return _importRunning; }
        }
        public String FirstDate
        {
            get { return _firstDate; }
        }
        public String LastDate
        {
            get { return _lastDate; }
        }

        private DroughtMonitorImporter(IDroughtMonitorRepository repository)
        {
            _repo = repository;

            this._firstDate = "None";
            this._lastDate = "None";
            this._importRunning = false;
            this.SetDates();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void InitWithContainer(IUnityContainer container)
        {
            if (_instance != null)
            {
                throw new Exception("Cannot reinitialize the singleton");
            }
            else
            {
                _instance = new DroughtMonitorImporter(container.Resolve<IDroughtMonitorRepository>());
            }
        }

        public static DroughtMonitorImporter getInstance()
        {
            if (_instance == null)
            {
                throw new Exception("The importer must first be initialized");
            }
            return _instance;
        } //End getInstance

        public void RunDMImport(string type)
        {
            switch (type)
            {
                case "full":
                    // Import All DM data
                    this.RunDMImportAll();
                    break;
                case "current":
                    // Import Current week
                    this.RunDMImport(DateTime.Now);
                    break;
                default:
                    this.RunDMImport(DateTime.Now);
                    break;
            }
        } //End RunDMImport (type)

        public void RunDMImportAll()
        {
            DateTime startDate = new DateTime(2009, 1, 6);
            DateTime endDate = DateTime.UtcNow;
            this.RunDMImport(startDate, endDate);
        } //End RunDMImportAll

        public void RunDMImport(DateTime date)
        {
            this.RunDMImport(date, date);
        } //End RunDMImport (one date)

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void RunDMImport(DateTime start, DateTime end)
        {
            if (!ImportRunning)
            {
                new Task(() =>
                {
                    this._importRunning = true;
                    this.ImportDMData(start, end);
                    this.SetDates(); //Reset the first and last dates to the new dates in the DB
                    this._importRunning = false;
                }).Start();
            }
        } //End RunDMImport (two dates)

        private void ImportDMData(DateTime startDate, DateTime endDate)
        {
            startDate = DroughtMonitorWeek.ConvertDateToTuesday(startDate);
            endDate = DroughtMonitorWeek.ConvertDateToTuesday(endDate);

            DateTime importWeek = startDate;
            List<DateTime> importDates = new List<DateTime>();
            while (importWeek <= endDate)
            {
                importDates.Add(importWeek);
                importWeek = importWeek.AddDays(7);
            }

            foreach (DateTime week in importDates)
            {
                if (!AlreadyImported(week))
                {
                    foreach (DMDataType type in Enum.GetValues(typeof(DMDataType)))
                    {
                        if (type != DMDataType.ALL)
                        {
                            //Get information
                            string url = String.Format(@"http://usdmdataservices.unl.edu/?mode=table&aoi={0}&date={1}", type.ToString().ToLower(), week.ToString("yyyyMMdd"));
                            WebClient client = new WebClient();
                            string response = client.DownloadString(url);

                            // split the response into rows based on the new line character
                            List<string> rows = response.Split('\n').ToList<string>();
                            rows.RemoveAt(0); // remove the header row

                            this.WriteData(type, rows, week);
                        }
                    }
                }
            } //End foreach week in importDates
             
        } //End ImportDMData (two dates)

        private bool AlreadyImported(DateTime week)
        {
            List<DroughtMonitorWeek> importedList =  _repo.FindUS(week, 0).ToList<DroughtMonitorWeek>();

            return (importedList.Count > 0);
        } //End AlreadyImported

        private void WriteData(DMDataType type, List<string> rows, DateTime date)
        {
            DroughtMonitorWeek dmWeek = new DroughtMonitorWeek();
            dmWeek.Type = type;
            dmWeek.Week = date;
            dmWeek.County = new USCounty();
            dmWeek.County.State = new USState();
            dmWeek.State = new USState();

            bool wroteUS = false;
            foreach (string line in rows)
            {
                if (line.Equals("") || wroteUS)
                {
                    continue;
                }

                // split out each column
                string[] cols = line.Split(',');

                int offset = 2; // Offset is 2 for the State and US data sets, but is 4 for the County data
                switch (type)
                {
                    case DMDataType.COUNTY:
                        dmWeek.County.ID = -1;
                        dmWeek.County.Name = cols[2];
                        dmWeek.County.Fips = int.Parse(cols[1]);
                        dmWeek.County.State.Name = cols[3];
                        dmWeek.State.Name = cols[3];
                        offset = 4;
                        break;
                    case DMDataType.STATE:
                        dmWeek.State.ID = -1;
                        dmWeek.State.Name = cols[1];
                        break;
                    case DMDataType.US:
                        //only write first line of data for US data
                        wroteUS = true;
                        break;
                }

                //Add DM values for all six columns
                for (int i = 0; i < 6; i++)
                {
                    // Set value for col[i+offset] with category i 
                    dmWeek[i] = float.Parse(cols[i + offset]);
                }

                _repo.Add(dmWeek);
            } //End foreach line in rows

        } //End WriteData

        private void SetDates()
        {
            try
            {
                this._firstDate = _repo.GetDmDate(0).ToString("MM/dd/yyyy");
                this._lastDate = _repo.GetDmDate(1).ToString("MM/dd/yyyy");
            }
            catch (ArgumentException e)
            {
                this._firstDate = "None";
                this._lastDate = "None";
            }
        } //End SetDates
    }
}