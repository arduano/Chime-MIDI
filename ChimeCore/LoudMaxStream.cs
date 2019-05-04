using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio;
using NAudio.Wave;

namespace ChimeCore
{
    public class LoudMaxStream : IPSampleProvider
    {
        public LoudMaxStream(IPSampleProvider provider)
        {
            Provider = provider;
            WaveFormat = provider.WaveFormat;
        }

        public WaveFormat WaveFormat { get; }
        public IPSampleProvider Provider { get; }
        public long Position { get => Provider.Position; set => Provider.Position = value; }

        public long Length => Provider.Length;

        double loudnessL = 1;
        double loudnessR = 1;
        double attack = 100;
        double falloff = 48000 / 3;
        double strength = 1;
        double minThresh = 0.4;
        public int Read(float[] buffer, int offset, int count)
        {
            int read = Provider.Read(buffer, offset, count);
            int end = offset + read;
            if (read % 2 != 0) throw new Exception("Must be a multiple of 2");
            for (int i = offset; i < end; i += 2)
            {
                double l = Math.Abs(buffer[i]);
                double r = Math.Abs(buffer[i + 1]);

                if (loudnessL > l)
                    loudnessL = (loudnessL * falloff + l) / (falloff + 1);
                else
                    loudnessL = (loudnessL * attack + l) / (attack + 1);

                if (loudnessR > r)
                    loudnessR = (loudnessR * falloff + r) / (falloff + 1);
                else
                    loudnessR = (loudnessR * attack + r) / (attack + 1);

                if (loudnessL < minThresh) loudnessL = minThresh;
                if (loudnessR < minThresh) loudnessR = minThresh;

                buffer[i] = (float)(buffer[i] / (loudnessL * strength + 2 * (1 - strength)) / 2);
                buffer[i + 1] = (float)(buffer[i + 1] / (loudnessR * strength + 2 * (1 - strength)) / 2);
            }
            return read;
        }
    }
}
