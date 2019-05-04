using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace ChimeCore
{
    public class ZeroFillerStream : IPSampleProvider
    {
        public long Position
        {
            get => Source.Position;
            set
            {
                if (value > Source.Length) Source.Position = Source.Length;
                else Source.Position = value;
            }
        }

        public long Length => Source.Length;

        public WaveFormat WaveFormat => Source.WaveFormat;

        public IPSampleProvider Source { get; }

        public ZeroFillerStream(IPSampleProvider source)
        {
            Source = source;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int read = Source.Read(buffer, offset, count);
            if(read == 0)
            {
                read = count;
                for (int i = 0; i < count; i++) buffer[offset + i] = 0;
            }
            return count;
        }
    }
}
