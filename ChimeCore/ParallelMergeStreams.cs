using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CSCore;

namespace ChimeCore
{
    public unsafe interface ISampleWriter
    {
        int Position { get; set; }
        void Write(float[] buffer, int offset, int count);
        void Write(float* buffer, int offset, int count);
    }

    public class ParallelMergeStreams : ISampleSource
    {
        class Writer : ISampleWriter
        {
            ParallelMergeStreams stream;

            public int Position { get; set; }

            public Writer(ParallelMergeStreams stream)
            {
                this.stream = stream;
            }

            public unsafe void Write(float[] buffer, int offset, int count)
            {
                lock (stream.data)
                {
                    if (Position + count > stream.data.Length)
                        Array.Resize(ref stream.data, Position + count);
                    fixed (float* dest = stream.data)
                    {
                        float* _dest = dest + Position;
                        for (int i = 0; i < count; i++)
                        {
                            _dest[i] += buffer[i + offset];
                        }
                    }
                }
            }

            public unsafe void Write(float* buffer, int offset, int count)
            {
                lock (stream.data)
                {
                    if (Position + count > stream.Length)
                    {
                        if (Position + count < stream.data.Length) stream.Length = Position + count;
                        else
                        {
                            int newsize = (int)(stream.data.Length * 1.2);
                            if (newsize < Position + count) newsize = Position + count;
                            Array.Resize(ref stream.data, newsize);
                            stream.Length = Position + count;
                            GC.Collect(2);
                        }
                    }
                    fixed (float* dest = stream.data)
                    {
                        float* _dest = dest + Position;
                        for (int i = 0; i < count; i++)
                        {
                            _dest[i] += buffer[i + offset];
                        }
                    }
                    Position += count;
                }
            }
        }

        public bool CanSeek => true;

        public WaveFormat WaveFormat { get; }

        public long Position
        {
            get => position;
            set
            {
                if (value > Length || value < 0) throw new Exception("Position out of range");
                position = value;
            }
        }
        public long Length { get; private set; } = 0;

        float[] data = new float[0];
        private long position = 0;

        public ParallelMergeStreams(WaveFormat format)
        {
            WaveFormat = format;
        }

        public void Dispose()
        {
            data = null;
        }

        public ISampleWriter GetWriter()
        {
            return new Writer(this);
        }

        public unsafe int Read(float[] buffer, int offset, int count)
        {
            if (Position >= Length) return 0;
            if (Length - position < count) count = (int)(Length - position);
            fixed (float* src = data)
            {
                float* _src = src + position;
                Marshal.Copy((IntPtr)_src, buffer, offset, count);
            }
            position += count;
            return count;
        }
    }
}
