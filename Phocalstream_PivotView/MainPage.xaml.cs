using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Pivot;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Phocalstream_PivotView
{
    public partial class MainPage : UserControl
    {

        private CxmlCollectionSource _source;

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
                    _source = new CxmlCollectionSource(new Uri(collection));
                    _source.StateChanged += _source_StateChanged;
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

        void _source_StateChanged(object sender, CxmlCollectionStateChangedEventArgs e)
        {
            if (e.NewState == CxmlCollectionState.Loaded)
            {
                Pivot.PivotProperties =_source.ItemProperties.ToList();
                Pivot.ItemTemplates = _source.ItemTemplates;
                Pivot.ItemsSource = _source.Items;
            }
        }
    }
}
