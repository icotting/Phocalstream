using Phocalstream_Web.Application;
using Phocalstream_Web.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phocalstream_Importer.ViewModels
{
    class CameraSiteViewModel : BindableObject
    {

        private int _progressTotal;
        private int _progressValue;
        private string _progressColor;
        private CameraSite _site;

        public CameraSite Site
        {
            get { return _site; }
            set { _site = value; }
        }

        public string StorageAccountName { get; set; }
        public string StorageAccountKey { get; set; }
        public string ImagePath { get; set; }

        public string CurrentStatus { get; set; }

        public int ProgressValue 
        {
            get { return _progressValue; }
            set { _progressValue = value; this.RaisePropertyChanged("ProgressValue"); }
        }

        public int ProgressTotal
        {
            get { return _progressTotal; }
            set { _progressTotal = value; this.RaisePropertyChanged("ProgressTotal"); }
        }

        public string ProgressColor
        {
            get { return _progressColor; }
            set { _progressColor = value; this.RaisePropertyChanged("ProgressColor"); }
        }

        public string SiteName
        {
            get { return _site.Name; }
            set { _site.Name = value; this.RaisePropertyChanged("SiteName"); }
        }

        public double Latitude
        {
            get { return _site.Latitude; }
            set { _site.Latitude = value; this.RaisePropertyChanged("Latitude"); }
        }

        public double Longitude
        {
            get { return _site.Longitude; }
            set { _site.Longitude = value; this.RaisePropertyChanged("Longitude"); }
        }

        public string ContainerName
        {
            get { return _site.ContainerID; }
            set { _site.ContainerID = value; this.RaisePropertyChanged("ContainerName"); }
        }

        public ObservableCollection<CameraSite> SiteList { get; set; }
    }
}
