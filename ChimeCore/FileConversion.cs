using MIDIModificationFramework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CSCore;

namespace ChimeCore
{
    public class FileConversion
    {
        ParallelMergeStreams AudioOutput;
        public List<AsyncProgress> Tracks { get; } = new List<AsyncProgress>();
        MIDIFile file;
        public int MaxThreads { get; }
        public int Samplerate { get; }
        public int Voices { get; }
        public int TracksRendered { get; private set; } = 0;
        public bool[] TrackRendered { get; private set; }
        public int TrackCount => file.TrackCount;

        bool cancelled = false;

        public FileConversion(MIDIFile file, int maxThreads, int samplerate, int voices, ParallelMergeStreams audioOutput)
        {
            BASSMIDI.Init(samplerate);
            BASSMIDI.LoadDefaultSoundfont();
            TrackRendered = new bool[file.TrackCount];
            AudioOutput = audioOutput;
            this.file = file;
            MaxThreads = maxThreads;
            Samplerate = samplerate;
            Voices = voices;
        }

        public Task Convert()
        {
            return Task.Run(() =>
            {
                if (cancelled) throw new Exception("Conversion already closed/cancelled");

                int[] trackOrder = new int[file.TrackCount];
                for (int i = 0; i < trackOrder.Length; i++) trackOrder[i] = i;
                Array.Sort(file.TrackSizes, trackOrder);
                trackOrder = trackOrder.Reverse().ToArray();

                object locker1 = new object();

                ParallelForeach(trackOrder, MaxThreads, (i) =>
                {
                    var track = file.GetTrack(i).GetSingleUse();
                    var mergedWithTempo = new TrackMerger(track, file.TempoEvents);
                    var process = Task.Run(() =>
                    {
                        Process m2w = new Process();
                        m2w.StartInfo = new ProcessStartInfo("m2w",
                            " --ppq " + file.PPQ +
                            " -sr " + Samplerate +
                            " -v " + Voices)
                        {
                            RedirectStandardInput = true,
                            RedirectStandardOutput = true,
                            UseShellExecute = false
                        };
                        m2w.Start();
                        Stream send = m2w.StandardInput.BaseStream;

                        ISampleWriter write = AudioOutput.GetWriter();
                        var copytask = Task.Run(() =>
                        {
                            Stream m2wout = m2w.StandardOutput.BaseStream;
                            byte[] buffer = new byte[2048 * 2048];
                            int read = 0;
                            unsafe
                            {
                                fixed (byte* buff = buffer)
                                {
                                    float* fbuff = (float*)buff;
                                    while ((read = m2wout.Read(buffer, 0, buffer.Length)) != 0)
                                    {
                                        write.Write(fbuff, 0, read / 4);
                                    }
                                }
                            }
                        });

                        byte[] bytes;
                        foreach (var e in mergedWithTempo)
                        {
                            bytes = e.GetDataWithDelta();
                            send.Write(bytes, 0, bytes.Length);
                            if (cancelled) break;
                        }

                        bytes = new byte[] { 0, 0xFF, 0x2F, 0x00 };
                        send.Write(bytes, 0, bytes.Length);
                        send.Close();
                        copytask.GetAwaiter().GetResult();
                    });
                    var p = new AsyncProgress(process, () => track.Progress, i);
                    lock (Tracks)
                        Tracks.Add(p);
                    process.GetAwaiter().GetResult();
                    lock (Tracks)
                    {
                        Tracks.Remove(p);
                        TrackRendered[i] = true;
                    }
                    lock (locker1)
                        TracksRendered++;
                });
                cancelled = true;
                TracksRendered = file.TrackCount;
            });
        }

        void ParallelForeach<T>(IEnumerable<T> items, int threads, Action<T> func)
        {
            object locker = new object();
            bool finished = false;
            List<Task> tasks = new List<Task>();
            foreach (var i in items)
            {
                tasks.Add(Task.Run(() =>
                {
                    func(i);
                    finished = true;
                }));
                if (tasks.Count >= threads)
                {
                    SpinWait.SpinUntil(() => finished);
                    finished = false;
                    for (int j = 0; j < tasks.Count; j++)
                    {
                        if (tasks[j].IsCompleted)
                        {
                            tasks.RemoveAt(j);
                            j--;
                        }
                    }
                }
            }
            while (tasks.Count != 0)
            {
                SpinWait.SpinUntil(() => finished);
                finished = false;
                for (int j = 0; j < tasks.Count; j++)
                {
                    if (tasks[j].IsCompleted)
                    {
                        tasks.RemoveAt(j);
                        j--;
                    }
                }
            }
        }

        public long GetRenderedNoteCount()
        {
            long nc = 0;
            for (int i = 0; i < file.TrackCount; i++)
            {
                if (TrackRendered[i]) nc += file.TrackSizes[i];
            }
            foreach (var t in Tracks)
            {
                nc += (long)(t.Progress * file.TrackSizes[t.Number]);
            }
            return nc;
        }

        public void Cancel()
        {
            cancelled = true;
        }
    }
}
