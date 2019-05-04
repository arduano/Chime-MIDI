using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSCore;

namespace ChimeCore
{
    public class StreamMixer : ISampleSource
    {
        public WaveFormat WaveFormat { get; }
        public bool CanSeek { get; }
        ISampleSource[] MixingStreams { get; }
        public long Position { get; set; }
        public long Length { get => MixingStreams.Select(s => s.Length).Max(); }

        public StreamMixer(ISampleSource[] streams, WaveFormat format, bool canSeek)
        {
            WaveFormat = format;
            CanSeek = canSeek;
            MixingStreams = streams;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int maxread = 0;
            float[] buf = new float[count];
            for (int i = 0; i < count; i++)
            {
                buffer[i + offset] = 0;
            }
            foreach (var stream in MixingStreams)
            {
                if (Position >= stream.Length) continue;
                if(Position != stream.Position)
                {
                    stream.Position = Position;
                }
                int read = stream.Read(buf, 0, count);
                for(int i = 0; i < read; i++)
                {
                    buffer[i + offset] += buf[i];
                }
                if (maxread < read) maxread = read;
            }
            Position += maxread;
            return maxread;
        }

        public void Dispose()
        {
            foreach (var s in MixingStreams) s.Dispose();
        }
    }
}
