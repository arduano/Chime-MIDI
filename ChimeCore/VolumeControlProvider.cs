using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace ChimeCore
{
    public class VolumeControlProvider : IPSampleProvider
    {
        public long Position { get => Stream.Position; set => Stream.Position = value; }

        public long Length => Stream.Length;

        public WaveFormat WaveFormat => Stream.WaveFormat;

        public IPSampleProvider Stream { get; }

        public double Volume => 10 * (Math.Log10(startVol + (endVol - startVol) * Math.Min(1, lastRead.ElapsedMilliseconds / ((double)len / WaveFormat.SampleRate * 1000))));

        double lvolume = 0;
        double rvolume = 0;

        public VolumeControlProvider(IPSampleProvider stream)
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
            for (int i = 0; i < read / 2; i++)
            {
                l = buffer[offset + i * 2];
                r = buffer[offset + i * 2 + 1];
                if (lvolume > l) lvolume = lvolume * 0.90;
                else lvolume = l;
                if (rvolume > r) rvolume = rvolume * 0.90;
                else rvolume = r;
            }
            len = read / 2;
            lastRead.Reset();
            lastRead.Start();
            endVol = (rvolume + lvolume) * 2;
            return read;
        }
    }
}
