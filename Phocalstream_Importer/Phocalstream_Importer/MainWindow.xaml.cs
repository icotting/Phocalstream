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
using System.Configuration;
using System.Data.SqlClient;

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

            _viewModel.StorageAccountKey = ConfigurationManager.AppSettings["storageAccountKey"];
            _viewModel.StorageAccountName = ConfigurationManager.AppSettings["storageAccountName"];

            Dictionary<long, int> counts = new Dictionary<long, int>();

            try
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConnection"].ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand command = new SqlCommand("select s.ID, count(P.ID) from Photos as p inner join CameraSites s on p.Site_ID = s.ID group by s.ID", conn))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                counts[reader.GetInt64(0)] = reader.GetInt32(1);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            try
            {
                using (EntityContext ctx = new EntityContext())
                {
                    _viewModel.SiteList = new ObservableCollection<CameraSite>(ctx.Sites.ToList<CameraSite>());

                    foreach (CameraSite site in _viewModel.SiteList)
                    {
                        site.PhotoCount = counts[site.ID];
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            InitializeComponent();
            base.DataContext = _viewModel;
        }
    }
}
