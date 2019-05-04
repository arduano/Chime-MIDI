using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSCore;

namespace ChimeCore
{
    public class ZeroFillerStream : ISampleSource
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

        public ISampleSource Source { get; }

        public bool CanSeek => throw new NotImplementedException();

        CSCore.WaveFormat IAudioSource.WaveFormat => Source.WaveFormat;

        public ZeroFillerStream(ISampleSource source)
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

        public void Dispose()
        {
            Source.Dispose();
        }
    }
}
