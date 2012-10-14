using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Phocalstream_Web.Models;
using Phocalstream_Web.Application;
using System.Drawing;
using System.Drawing.Imaging;
using Phocalstream_Importer.ViewModels;
using System.Data;
using System.Collections.ObjectModel;

namespace Phocalstream_Importer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CameraSiteViewModel _viewModel = new CameraSiteViewModel();

        public MainWindow()
        {
            _viewModel.Site = new CameraSite();
            _viewModel.ProgressTotal = 1;
            _viewModel.SelectedSiteIndex = -1;
            using (EntityContext ctx = new EntityContext())
            {
                _viewModel.SiteList = new ObservableCollection<CameraSite>(ctx.Sites.Include("Photos").ToList<CameraSite>());
            }

            InitializeComponent();
            base.DataContext = _viewModel;
        }
    }
}
