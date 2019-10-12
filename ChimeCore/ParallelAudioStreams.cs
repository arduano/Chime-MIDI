using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MIDIModificationFramework;
using CSCore;

namespace ChimeCore
{
    public abstract class ParallelAudioStreams
    {
        protected ParallelStream streams;
        public ParallelAudioStreams(string filename, WaveFormat waveFormat, int bufferSize)
        {
            streams = new ParallelStream(File.Open(filename, FileMode.Create));
            WaveFormat = waveFormat;
            BufferSize = bufferSize;
        }

        public WaveFormat WaveFormat { get; }
        public int BufferSize { get; }

        public abstract IWaveSource GetReader(int track);
        public abstract IDisposableWritable GetWriter(int track);

        public void CloseAllStreams() => streams.CloseAllStreams();
    }
}
