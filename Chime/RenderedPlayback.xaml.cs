using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using CSCore;
using CSCore.SoundOut;

namespace Chime
{
    /// <summary>
    /// Interaction logic for RenderedPlayback.xaml
    /// </summary>
    public partial class RenderedPlayback : UserControl
    {
        private ISoundOut GetSoundOut()
        {
            if (WasapiOut.IsSupportedOnCurrentPlatform)
                return new WasapiOut();
            else
                return new DirectSoundOut();
        }

        ISoundOut waveOut;
        public RenderedPlayback(ISampleSource[] sources, WaveFormat format)
        {
            InitializeComponent();
            Sources = sources;
            ISampleSource[] loudmaxed = new ISampleSource[sources.Length];
            Loudmaxes = new LoudMaxStream[sources.Length];
            Volumes = new VolumeControlProvider[sources.Length];
            for (int i = 0; i < sources.Length; i++)
            {
                Loudmaxes[i] = new LoudMaxStream(sources[i]);
                loudmaxed[i] = Loudmaxes[i];
                Volumes[i] = new VolumeControlProvider(loudmaxed[i]);
                var t = new TrackPlayback(Volumes[i], "Track " + i);
                loudmaxed[i] = Volumes[i];
                tracksDock.Children.Add(t);
                DockPanel.SetDock(t, Dock.Left);
                t.Height = double.NaN;

            }
            float[] buffer = new float[48000];
            byte[] bbuffer = new byte[buffer.Length * 4];
            var fs = new LoudMaxStream(new StreamMixer(loudmaxed, format, true));

            FinalMix = new ZeroFillerStream(fs);
            waveOut = GetSoundOut();
            waveOut.Initialize(FinalMix.ToWaveSource());
        }

        public ISampleSource[] Sources { get; }
        public LoudMaxStream[] Loudmaxes { get; }
        public VolumeControlProvider[] Volumes { get; }
        public ISampleSource FinalMix { get; }

        bool paused = false;

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            TogglePause();
        }

        void TogglePause()
        {
            if (paused)
            {
                if (FinalMix.Position == FinalMix.Length) FinalMix.Position = 0;
                waveOut.Play();
                pauseButton.Content = "Pause";
            }
            else
            {
                waveOut.Pause();
                pauseButton.Content = "Play";
            }
            paused = !paused;
        }

        bool ended = false;

        public Task Start()
        {
            return Task.Run(() =>
            {
                waveOut.Play();
                while (!ended)
                {
                    if (FinalMix.Position == FinalMix.Length && !paused)
                        Dispatcher.Invoke(() => TogglePause());

                    bool ended = false;
                    Dispatcher.Invoke(() =>
                    {
                        autoSlider = true;
                        timeSlider.Value = FinalMix.Position / (double)FinalMix.Length;
                        autoSlider = false;
                        foreach (var c in tracksDock.Children)
                        {
                            var t = (TrackPlayback)c;
                            t.Update();
                        }
                        ended = true;
                    });
                    SpinWait.SpinUntil(() => ended);
                    Thread.Sleep(50);
                }
            });
        }

        bool autoSlider = false;
        private void TimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (autoSlider) return;
            long pos = (long)(FinalMix.Length * timeSlider.Value);
            pos -= pos % 2;
            FinalMix.Position = pos;
        }

        private void LoudmaxStrength_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                foreach (var l in Loudmaxes) l.Strength = loudmaxStrength.Value;
            }
            catch { }
        }

        private void SortButton_Click(object sender, RoutedEventArgs e)
        {
            TrackPlayback[] playbacks = new TrackPlayback[tracksDock.Children.Count];
            int i = 0;
            foreach (var c in tracksDock.Children) playbacks[i++] = (TrackPlayback)c;
            var volumes = playbacks.Select(v => v.Control.Volume).ToArray();
            Array.Sort(volumes, playbacks);

            tracksDock.Children.Clear();
            foreach (var t in playbacks.Reverse())
            {
                tracksDock.Children.Add(t);
                DockPanel.SetDock(t, Dock.Left);
                t.Height = double.NaN;
            }
        }

        private void ResetVolumes_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to reset ALL volume sliders?", "Reset", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    foreach (var t in tracksDock.Children) ((TrackPlayback)t).Gain.Value = 0;
                }
                catch { }
            }
        }
    }
}
