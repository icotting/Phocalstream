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

namespace Phocalstream_PhotoView
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
            string photo = App.Current.Host.InitParams["photo"].ToString();
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
