using CSCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChimeCore
{
    public class VolumeControlProvider : ISampleSource
    {
        public long Position { get => Stream.Position; set => Stream.Position = value; }

        public long Length => Stream.Length;

        public WaveFormat WaveFormat => Stream.WaveFormat;

        public ISampleSource Stream { get; }

        double gain = 1;
        public double Gain { get => 10 * Math.Log10(gain); set => gain = Math.Pow(10, value / 10); }

        public double Volume => 10 * (Math.Log10(startVol + (endVol - startVol) * Math.Min(1, lastRead.ElapsedMilliseconds / ((double)len / WaveFormat.SampleRate * 1000))));

        public bool CanSeek => Stream.CanSeek;

        double lvolume = 0;
        double rvolume = 0;

        public VolumeControlProvider(ISampleSource stream)
        {
            Stream = stream;
        }

        Stopwatch lastRead = new Stopwatch();
        double startVol = 0;
        double endVol = 0;
        int len = 0;

        public int Read(float[] buffer, int offset, int count)
        {
            int read = Stream.Read(buffer, offset, count);
            if (read % 2 != 0) throw new Exception("Read not multiple of 2");
            double l;
            double r;
            startVol = (rvolume + lvolume) * 2;
            float change = 1 - (float)read / WaveFormat.SampleRate / WaveFormat.Channels / 5;
            for (int i = 0; i < read / 2; i++)
            {
                l = buffer[offset + i * 2];
                r = buffer[offset + i * 2 + 1];
                if (lvolume > l) lvolume = lvolume * change;
                else lvolume = l;
                if (rvolume > r) rvolume = rvolume * change;
                else rvolume = r;
                buffer[offset + i * 2] = (float)(l * gain);
                buffer[offset + i * 2 + 1] = (float)(r * gain);
            }
            len = read / 2;
            lastRead.Reset();
            lastRead.Start();
            endVol = (rvolume + lvolume) * 2;
            return read;
        }

        public void Dispose()
        {
            Stream.Dispose();
        }
    }
}
