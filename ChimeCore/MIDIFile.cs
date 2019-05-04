using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MIDIModificationFramework;
using MIDIModificationFramework.MIDI_Events;

namespace ChimeCore
{
    public class MIDIFile : IDisposable
    {
        MidiFile file;

        public long NoteCount { get; set; }
        public double TimeLength { get; set; }
        public LinkedList<TempoEvent> TempoEvents { get; set; }
        public int PPQ => file.PPQ;
        public int TrackCount => file.TrackCount;

        public int[] TrackSizes { get; private set; }

        public MIDIFile(string path)
        {
            file = new MidiFile(path);
            if (file.PPQ < 0) throw new Exception("Division < 0 not supported");
            if (file.Format == 2) throw new Exception("Format 2 midi not supported");
        }

        public void Parse()
        {
            var tempoEventTracks = new List<FastList<TempoEvent>>();

            int[] trackSizes = new int[file.TrackCount];

            int nc = 0;
            Parallel.For(0, file.TrackCount, (j) =>
            {
                int tnc = 0;
                FastList<TempoEvent> tempos = new FastList<TempoEvent>();
                var track = file.GetTrack(j);
                uint delta = 0;
                foreach (var e in track)
                {
                    delta += e.DeltaTime;
                    if (e is NoteOnEvent && (e as NoteOnEvent).Velocity != 0) tnc++;
                    if (e is TempoEvent)
                    {
                        e.DeltaTime = delta;
                        delta = 0;
                        tempos.Add(e as TempoEvent);
                    }
                }
                lock (tempoEventTracks)
                {
                    tempoEventTracks.Add(tempos);
                    nc += tnc;
                }
                trackSizes[j] = tnc;
            });
            var mergedTempos = new LinkedList<TempoEvent>();
            var merge = new TreeTrackMerger(tempoEventTracks);
            foreach (var e in merge) mergedTempos.AddLast(e as TempoEvent);
            TempoEvents = mergedTempos;
            TrackSizes = trackSizes;
        }

        public void Dispose()
        {
            file.Dispose();
        }

        public TrackReader GetTrack(int track)
        {
            return file.GetTrack(track);
        }
    }
}
