using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Phocalstream_PivotView
{
    public partial class MainPage : UserControl
    {
        public MainPage()
        {
            InitializeComponent();
            Loaded += new RoutedEventHandler(MainPage_Loaded);
        }

        protected void MainPage_Loaded(object sender, RoutedEventArgs e) 
        {

            if (App.Current.Host.InitParams.ContainsKey("collection"))
            {
                string collection = App.Current.Host.InitParams["collection"].ToString();
                Pivot.Visibility = System.Windows.Visibility.Visible;
                try
                {
                    Pivot.LoadCollection(collection, string.Empty);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    Console.WriteLine(ex.ToString());
                }
            }
            else if (App.Current.Host.InitParams.ContainsKey("photo"))
            {
                string photo = App.Current.Host.InitParams["photo"].ToString();
                Image.Visibility = System.Windows.Visibility.Visible;
                try
                {
                    Image.Source = new DeepZoomImageTileSource(new Uri(photo));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    Console.WriteLine(ex.ToString());
                }
            }
        }
    }
}
