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
    /// Interaction logic for TrackPlayback.xaml
    /// </summary>
    public partial class TrackPlayback : UserControl
    {
        public TrackPlayback(VolumeControlProvider control, string name)
        {
            InitializeComponent();
            Control = control;
            this.name.Content = name;
        }

        public VolumeControlProvider Control { get; }

        public void Update()
        {
            double vol = Control.Volume;
            if (double.IsNaN(vol)) vol = double.NegativeInfinity;
            dbLabel.Content = vol.ToString() + "db";
            if (vol < volume.Minimum) vol = volume.Minimum;
            if (vol > volume.Maximum) vol = volume.Maximum;
            if (vol != volume.Value)
                volume.Value = vol;
        }
    }
}
