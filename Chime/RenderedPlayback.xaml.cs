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
using NAudio.Wave;

namespace Chime
{
    /// <summary>
    /// Interaction logic for RenderedPlayback.xaml
    /// </summary>
    public partial class RenderedPlayback : UserControl
    {
        WaveOutEvent waveOut = new WaveOutEvent();
        public RenderedPlayback(IPSampleProvider[] sources, WaveFormat format)
        {
            InitializeComponent();
            Sources = sources;
            IPSampleProvider[] loudmaxed = new IPSampleProvider[sources.Length];
            for (int i = 0; i < sources.Length; i++)
            {
                loudmaxed[i] = new LoudMaxStream(sources[i]);
                var vol = new VolumeControlProvider(loudmaxed[i]);
                var t = new TrackPlayback(vol, "Track " + i);
                loudmaxed[i] = vol;
                tracksDock.Children.Add(t);
                DockPanel.SetDock(t, Dock.Left);
                t.Height = double.NaN;

            }
            float[] buffer = new float[48000];
            byte[] bbuffer = new byte[buffer.Length * 4];
            var fs = new LoudMaxStream(new StreamMixer(loudmaxed, format));

            FinalMix = new ZeroFillerStream(fs);
            waveOut.Init(FinalMix);
        }

        public IPSampleProvider[] Sources { get; }
        public IPSampleProvider FinalMix { get; }

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
                Task volumes = Task.Run(() => {
                    while (!ended)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            foreach (var c in tracksDock.Children)
                            {
                                var t = (TrackPlayback)c;
                                t.Update();
                            }
                        });
                        Thread.Sleep(20);
                    }
                });
                waveOut.Play();
                while (!ended)
                {
                    if (FinalMix.Position == FinalMix.Length && !paused)
                        Dispatcher.Invoke(() => TogglePause());
                    Dispatcher.Invoke(() => timeSlider.Value = FinalMix.Position / (double)FinalMix.Length);
                    Thread.Sleep(20);
                }
                volumes.GetAwaiter().GetResult();
            });
        }

        private void TimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            long pos = (long)(FinalMix.Length * timeSlider.Value);
            pos -= pos % 2;
            FinalMix.Position = pos;
        }
    }
}
