using Phocalstream_Shared.Data.Model.Photo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Xml.Serialization;

namespace Phocalstream_TimeLapse
{
    public partial class MainPage : UserControl
    {

        public TimelapseVideo Video { get; set; }
        public List<MultiScaleImage> ImageFrames { get; set; }

        public MainPage(string encodedVideo)
        {
            InitializeComponent();

            byte[] videoDescriptor = Convert.FromBase64String(encodedVideo);
            string xml = Encoding.UTF8.GetString(videoDescriptor, 0, videoDescriptor.Length);

            XmlSerializer serializer = new XmlSerializer(typeof(TimelapseVideo));
            using (StringReader reader = new StringReader(xml))
            {
                this.Video = (TimelapseVideo)serializer.Deserialize(reader);
            }

            this.ImageFrames = new List<MultiScaleImage>(10) { null, null, null, null, null, null, null, null, null, null};
            for (var i = 9; i >= 0; i--)
            {
                MultiScaleImage image = new MultiScaleImage();
                image.Width = 800;
                image.Height = 600;
                image.Source = new DeepZoomImageTileSource(new Uri(this.Video.Frames.ElementAt<TimelapseFrame>(i).Url, UriKind.Absolute));
                image.Opacity = 0;

                if (i == 0)
                {
                    image.ImageOpenSucceeded += ImageOpened;
                }

                this.ImageFrames.Insert(i, image);
                ImageCanvas.Children.Add(image);
            }
            this.ImageFrames.ElementAt(0).Opacity = 1;


            /*
            ThreadStart loader = delegate()
            {
                foreach (TimelapseFrame frame in this.Video.Frames)
                {
                    Dispatcher.BeginInvoke(new Action(() => this.Image.Source = new DeepZoomImageTileSource(new Uri(frame.Url, UriKind.Absolute))));
                    Thread.Sleep(1000);
                }
            };

            new Thread(loader).Start(); */
        }

        private void ImageOpened(Object sender, EventArgs args)
        {
            MessageBox.Show("Final image opened!");
        }
    }
}
