using System;
using System.Linq;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Net;
using System.Windows;
using System.Windows.Browser;
using System.Windows.Controls;
using System.Windows.Controls.Pivot;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Phocalstream_PivotView
{
    public class PhocalstreamPivotViewer : PivotViewer
    {
        public PhocalstreamPivotViewer()
        {
            ItemDoubleClick += new EventHandler<PivotViewerItemDoubleClickEventArgs>(HandleItemDoubleClick);
            (InScopeItems as INotifyCollectionChanged).CollectionChanged += new NotifyCollectionChangedEventHandler(HandleCollectionChangeEvent);
        }

        protected void HandleCollectionChangeEvent(object sender, NotifyCollectionChangedEventArgs e)
        {
            string new_collection = String.Join(",", (from i in InScopeItems select ((PivotViewerItem)i).Id));
            HtmlPage.Window.Invoke("registerNewSelection", new_collection);
        }

        protected void HandleItemDoubleClick(object sender, PivotViewerItemDoubleClickEventArgs e)
        {
            string source = Application.Current.Host.Source.AbsoluteUri;
            string root = source.Substring(0, source.ToLower().IndexOf("clientbin"));
            HtmlPage.Window.Navigate(new Uri(string.Format("{0}/photo?photoID={1}", root, ((PivotViewerItem)e.Item).Id)), "_blank");
        }
    }
}
