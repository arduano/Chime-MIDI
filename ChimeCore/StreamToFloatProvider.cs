using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChimeCore
{
    public class StreamToFloatProvider : IPSampleProvider
    {
        public StreamToFloatProvider(Stream stream, WaveFormat format)
        {
            this.stream = stream;
            WaveFormat = format;
        }

        public WaveFormat WaveFormat { get; }
        public long Position { get => stream.Position / 4; set => stream.Position = value * 4; }

        public long Length => stream.Length / 4;

        Stream stream;

        public int Read(float[] buffer, int offset, int count)
        {
            byte[] bbuffer = new byte[count * 4];
            int read = stream.Read(bbuffer, 0, bbuffer.Length);
            if (read % 4 != 0) throw new Exception("Read length not divisible by 4");
            Buffer.BlockCopy(bbuffer, 0, buffer, offset, read);
            return read / 4;
        }
    }
}
