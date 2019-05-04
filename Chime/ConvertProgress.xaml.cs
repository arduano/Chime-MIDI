using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace Chime
{
    /// <summary>
    /// Interaction logic for ConvertProgress.xaml
    /// </summary>
    public partial class ConvertProgress : UserControl
    {
        public event Action OnConversionComplete;
        public ConvertProgress(FileConversion conversion)
        {
            InitializeComponent();
            Conversion = conversion;
        }

        public FileConversion Conversion { get; }
        Task converstionTask;

        public void Start()
        {
            converstionTask = Conversion.Convert();
            Task.Run(UpdateLoop);
        }

        void UpdateLoop()
        {
            double nps = 0;
            long prevNotes = 0;
            Stopwatch s = new Stopwatch();
            s.Start();
            while (!converstionTask.IsCompleted)
            {
                Dispatcher.Invoke(() =>
                {
                    lock (Conversion.Tracks)
                    {
                        for (int i = 0; i < currentTasks.Children.Count; i++)
                        {
                            var item = currentTasks.Children[i];
                            if (i >= Conversion.Tracks.Count)
                            {
                                currentTasks.Children.RemoveAt(i);
                                continue;
                            }
                            ((TrackProgress)item).Progress = Conversion.Tracks[i];
                        }
                        for (int i = currentTasks.Children.Count; i < Conversion.Tracks.Count; i++)
                        {
                            //List<TrackProgress> progresses = new List<TrackProgress>();
                            var a = new TrackProgress(Conversion.Tracks[i]);
                            DockPanel.SetDock(a, Dock.Top);
                            currentTasks.Children.Add(a);
                            //progresses.Add(a);
                            //foreach (var c in currentTasks.Children) progresses.Add((TrackProgress)c);
                            //currentTasks.Children.Clear();
                            //foreach (var c in progresses)
                            //{
                            //    DockPanel.SetDock(c, Dock.Top);
                            //    currentTasks.Children.Add(c);
                            //}
                        }
                        foreach (var item in currentTasks.Children)
                        {
                            ((TrackProgress)item).Update();
                        }
                        long nc = Conversion.GetRenderedNoteCount();
                        double multiply = 1000.0 / s.ElapsedMilliseconds;
                        s.Reset();
                        s.Start();
                        nps = (nps * 10 + (nc - prevNotes) * multiply) / 11;
                        prevNotes = nc;
                        tracksDoneLabel.Content = 
                            "Completed: " + Conversion.TracksRendered + "/" + Conversion.TrackCount + 
                            "   Notes: " + nc.ToString("#,##0") + 
                            "   NPS: " + ((int)nps).ToString("#,##0");
                    }
                });
                Thread.Sleep(200);
            }
            Dispatcher.Invoke(() =>
            {
                currentTasks.Children.Clear();
                tracksDoneLabel.Content =
                    "Completed: " + Conversion.TracksRendered + "/" + Conversion.TrackCount +
                    "   Notes: " + Conversion.GetRenderedNoteCount().ToString("#,##0");
            });
            Dispatcher.Invoke(() => OnConversionComplete());
        }
    }
}
