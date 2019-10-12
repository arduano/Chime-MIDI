using CSCore;
using CSCore.Codecs.RAW;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChimeCore
{
    public class ParallelAACStreams : ParallelAudioStreams
    {
        class DWStream : IDisposableWritable
        {
            public DWStream(Stream output)
            {
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
        class AacEncoderDW : CSCore.Codecs.AAC.AacEncoder, IDisposableWritable
        {
            public AacEncoderDW(WaveFormat format, Stream stream) : base(format, stream) { }
        }

        public ParallelAACStreams(string filename, WaveFormat waveFormat, int bufferSize) : base(filename, waveFormat, bufferSize)
        {

        }

        public override IWaveSource GetReader(int track)
        {
            return new CSCore.Codecs.AAC.AacDecoder(streams.GetStream(track, true));
        }

        public override IDisposableWritable GetWriter(int track)
        {
            Process ffmpeg = new Process();
            ffmpeg.StartInfo = new ProcessStartInfo("ffmpeg", "-f f32le -i - -f adts -c:a aac -ar 48000 -ac 2 -")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            ffmpeg.Start();
            var output = new BufferedStream(streams.GetStream(track), BufferSize);
            ffmpeg.StandardOutput.BaseStream.CopyToAsync(output);
            return new DWStream(ffmpeg.StandardInput.BaseStream);
        }
    }
}
