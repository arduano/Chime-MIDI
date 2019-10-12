using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSCore;

namespace ChimeCore
{
    public class WaveToSampleReader : ISampleSource
    {
        public WaveToSampleReader(IWaveSource input, WaveFormat waveFormat)
        {
            Input = input;
            WaveFormat = waveFormat;
        }

        public IWaveSource Input { get; }
        public WaveFormat WaveFormat { get; }

        public bool CanSeek => true;

        public long Position { get => Input.Position / 4; set => Input.Position = value * 4; }

        public long Length => Input.Length / 4;

        public void Dispose()
        {
            Input.Dispose();
        }

        public int Read(float[] buffer, int offset, int count)
        {
            byte[] data = new byte[count * 4];
            int read = Input.Read(data, 0, count * 4);
            if (read % 4 != 0) throw new Exception("Data length not divisible by 4");
            Buffer.BlockCopy(data, 0, buffer, offset * 4, read);
            return read / 4;
        }
    }
}
