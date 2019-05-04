using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ChimeCore;

namespace Chime
{
    /// <summary>
    /// Interaction logic for TrackProgress.xaml
    /// </summary>
    public partial class TrackProgress : UserControl
    {
        public TrackProgress(AsyncProgress progress)
        {
            InitializeComponent();
            Progress = progress;
            Width = double.NaN;
            Height = double.NaN;
        }

        public AsyncProgress Progress { get; set; }

        public void Update()
        {
            taskName.Content = "Track " + Progress.Number;
            taskProgress.Value = Progress.Progress;
        }
    }
}
