using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSCore;
using CSCore.Codecs.RAW;

namespace ChimeCore
{
    public class ParallelRawStreams : ParallelAudioStreams
    {
        class RawReader : IWaveSource
        {
            public RawReader(Stream input, WaveFormat waveFormat)
            {
                Input = input;
                WaveFormat = waveFormat;
            }

            public Stream Input { get; }
            public WaveFormat WaveFormat { get; }

            public bool CanSeek => true;

            public long Position { get => Input.Position; set => Input.Position = value; }

            public long Length => Input.Length;

            public void Dispose()
            {
                Input.Close();
                Input.Dispose();
            }

            public int Read(byte[] buffer, int offset, int count)
            {
                return Input.Read(buffer, offset, count);
            }
        }

        class RawWriter : IDisposableWritable
        {
            public RawWriter(Stream output) {
                Output = output;
            }

            public Stream Output { get; }

            public void Dispose()
            {
                Output.Close();
                Output.Dispose();
            }

            public void Write(byte[] buffer, int offset, int count)
            {
                Output.Write(buffer, offset, count);
            }
        }

        public ParallelRawStreams(string filename, WaveFormat waveFormat, int bufferSize) : base(filename, waveFormat, bufferSize)
        {
            
        }

        public override IWaveSource GetReader(int track)
        {
            return new RawReader(new BufferedStream(streams.GetStream(track, true), 4096 * 128), WaveFormat);
            //return new RawDataReader(streams.GetStream(track, true), WaveFormat);
        }

        public override IDisposableWritable GetWriter(int track)
        {
            return new RawWriter(new BufferedStream(streams.GetStream(track), BufferSize));
        }
    }
}
