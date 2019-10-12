using ChimeCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using MIDIModificationFramework;
using System.IO;
using System.Threading;
using CSCore;
using CSCore.Codecs.WAV;
using CSCore.Streams.SampleConverter;

namespace Chime
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        ParallelMergeStreams streams;
        //MIDIFile file = new MIDIFile("E:\\Midi\\tau2.5.9.mid");
        //MIDIFile file = new MIDIFile("E:\\Midi\\Clubstep.mid");
        //MIDIFile file = new MIDIFile("E:\\Midi\\TN3_Divided\\The Nuker 3 F3.mid");
        MIDIFile file = new MIDIFile("E:\\Midi\\[Black MIDI]scarlet_zone-& The Young Descendant of Tepes V.2.mid");
        //MIDIFile file = new MIDIFile("E:\\Midi\\Ra Ra Rasputin Ultimate Black MIDI ~THE ULTIMATE APOCALYSE~ Final.mid");

        WaveFormat format = new WaveFormat(48000, 32, 2, AudioEncoding.IeeeFloat);

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //streams = new ParallelRawStreams("E:\\rendred.streams", new WaveFormat(48000, 32, 2, AudioEncoding.IeeeFloat), 4096 * 4096);
            //streams = new ParallelRawStreams("F:\\rendred.streams", new WaveFormat(48000, 32, 2, AudioEncoding.IeeeFloat), 4096 * 4096);
            //streams = new ParallelOggStreams("E:\\rendred.streams", new WaveFormat(48000, 32, 2, AudioEncoding.IeeeFloat), 4096 * 4096);
            //streams = new ParallelAACStreams("E:\\rendred.streams", new WaveFormat(48000, 32, 2, AudioEncoding.IeeeFloat), 4096 * 4096);

            streams = new ParallelMergeStreams(format);

            file.Parse();
            var convert = new FileConversion(file, 16, 48000, 500, streams);

            //var convert = new FileConversion(file, 16, 48000, 500, (i) =>
            //{
            //    var path = "E:\\m2w\\track" + i + ".wav";
            //    Process ffmpeg = new Process();
            //    ffmpeg.StartInfo = new ProcessStartInfo("ffmpeg", "-f f32le -ar 48000 -ac 2 -i - -c:a pcm_s16le " + path + " -y")
            //    {
            //        RedirectStandardInput = true,
            //        UseShellExecute = false
            //    };
            //    ffmpeg.Start();
            //    //var stdin = new LoudMaxStream(ffmpeg.StandardInput.BaseStream, samplerate, 1.0, 0.0);
            //    return ffmpeg.StandardInput.BaseStream;
            //});
            var convertProgress = new ConvertProgress(convert);
            mainGrid.Children.Add(convertProgress);
            convertProgress.HorizontalAlignment = HorizontalAlignment.Stretch;
            convertProgress.VerticalAlignment = VerticalAlignment.Stretch;
            convertProgress.Width = convertProgress.Height = double.NaN;
            convertProgress.OnConversionComplete += ConversionCompletedPlayElement;
            convertProgress.Start();
            
        }

        //void ConversionCompletedSaveall()
        //{
        //    streams.CloseAllStreams();
        //    for (int i = 0; i < file.TrackCount; i++)
        //    {
        //        var path = "E:\\m2w\\track" + i + ".wav";
        //        var format = new WaveFormat(48000, 16, 2);
        //        int read;
        //        var fs = new StreamToFloatProvider(streams.GetStream(i, true), format);
        //        var s = streams.GetStream(i, true);
        //        float[] buffer = new float[48000];
        //        byte[] bbuffer = new byte[buffer.Length * 4];

        //        Process ffmpeg = new Process();
        //        ffmpeg.StartInfo = new ProcessStartInfo("ffmpeg", "-f f32le -ar 48000 -ac 2 -i - -c:a pcm_s16le " + path + " -y")
        //        {
        //            RedirectStandardInput = true,
        //            UseShellExecute = false
        //        };
        //        ffmpeg.Start();

        //        //while((read = s.Read(bbuffer, 0, bbuffer.Length)) != 0)
        //        //{
        //        //    ffmpeg.StandardInput.BaseStream.Write(bbuffer, 0, bbuffer.Length);
        //        //}

        //        while ((read = fs.Read(buffer, 0, buffer.Length)) != 0)
        //        {
        //            Buffer.BlockCopy(buffer, 0, bbuffer, 0, read * 4);
        //            ffmpeg.StandardInput.BaseStream.Write(bbuffer, 0, read * 4);
        //        }
        //        ffmpeg.StandardInput.Close();
        //    }
        //}

        //void ConversionCompletedMergeall()
        //{
        //    streams.CloseAllStreams();
        //    List<IPSampleProvider> providers = new List<IPSampleProvider>();
        //    var format = new WaveFormat(48000, 16, 2);
        //    for (int i = 0; i < file.TrackCount; i++)
        //    {
        //        IPSampleProvider sp = new StreamToFloatProvider(streams.GetStream(i, true), format);
        //        sp = new LoudMaxStream(sp);
        //        providers.Add(sp);
        //    }
        //    float[] buffer = new float[48000];
        //    byte[] bbuffer = new byte[buffer.Length * 4];
        //    var fs = new LoudMaxStream(new StreamMixer(providers.ToArray(), format));

        //    var path = "E:\\m2w\\_merged.wav";
        //    int read;

        //    Process ffmpeg = new Process();
        //    ffmpeg.StartInfo = new ProcessStartInfo("ffmpeg", "-f f32le -ar 48000 -ac 2 -i - -c:a pcm_s16le " + path + " -y")
        //    {
        //        RedirectStandardInput = true,
        //        UseShellExecute = false
        //    };
        //    ffmpeg.Start();

        //    while ((read = fs.Read(buffer, 0, buffer.Length)) != 0)
        //    {
        //        Buffer.BlockCopy(buffer, 0, bbuffer, 0, read * 4);
        //        ffmpeg.StandardInput.BaseStream.Write(bbuffer, 0, read * 4);
        //    }
        //    ffmpeg.StandardInput.Close();
        //}

        //void ConversionCompletedPlay()
        //{
        //    streams.CloseAllStreams();
        //    List<IPSampleProvider> providers = new List<IPSampleProvider>();
        //    var format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        //    for (int i = 0; i < file.TrackCount; i++)
        //    {
        //        IPSampleProvider sp = new StreamToFloatProvider(streams.GetStream(i, true), format);
        //        sp = new LoudMaxStream(sp);
        //        providers.Add(sp);
        //    }
        //    float[] buffer = new float[48000];
        //    byte[] bbuffer = new byte[buffer.Length * 4];
        //    var fs = new LoudMaxStream(new StreamMixer(providers.ToArray(), format));

        //    using (var waveOut = new WaveOutEvent())
        //    {
        //        waveOut.Init(fs);
        //        waveOut.Play();
        //        while (waveOut.PlaybackState == PlaybackState.Playing)
        //        {
        //            Thread.Sleep(100);
        //        }
        //    }
        //}

        void ConversionCompletedPlayElement()
        {
            streams.Position = 0;
            var s = streams.ToWaveSource(32);
            var dest = new WaveWriter(File.Open("E:\\test.wav", FileMode.Create), format);
            var read = 0;
            byte[] buff = new byte[1024 * 16];
            while((read = s.Read(buff, 0, buff.Length)) != 0)
            {
                dest.Write(buff, 0, read);
            }
            dest.Dispose();
            Console.WriteLine("done");
            //streams.CloseAllStreams();
            //List<ISampleSource> providers = new List<ISampleSource>();
            //for (int i = 0; i < file.TrackCount; i++)
            //{
            //        var reader = streams.GetReader(i);
            //    ISampleSource sp = new WaveToSampleReader(reader, format);
            //    providers.Add(sp);
            //}

            //var playback = new RenderedPlayback(providers.ToArray(), format);
            //mainGrid.Children.Clear();
            //mainGrid.Children.Add(playback);
            //playback.HorizontalAlignment = HorizontalAlignment.Stretch;
            //playback.VerticalAlignment = VerticalAlignment.Stretch;
            //playback.Width = playback.Height = double.NaN;
            //playback.Start();
        }
    }
}
