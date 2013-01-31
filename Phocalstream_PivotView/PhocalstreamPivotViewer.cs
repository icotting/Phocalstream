using System;
using System.Net;
using System.Windows;
using System.Windows.Browser;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Pivot;
using System.Windows.Shapes;

namespace Phocalstream_PivotView
{
    public class PhocalstreamPivotViewer : PivotViewer
    {
        public PhocalstreamPivotViewer()
        {
            ItemDoubleClicked += new EventHandler<ItemEventArgs>(HandleItemDoubleClick);
        }

        protected void HandleItemDoubleClick(object sender, ItemEventArgs e)
        {
            string source = Application.Current.Host.Source.AbsoluteUri;
            string root = source.Substring(0, source.ToLower().IndexOf("clientbin"));
            HtmlPage.Window.Navigate(new Uri(string.Format("{0}/photo?photoID={1}", root, e.ItemId)), "_blank");
        }
    }
}
