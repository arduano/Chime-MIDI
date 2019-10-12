using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSCore;

namespace ChimeCore
{
    public class ParallelOggStreams : ParallelAudioStreams
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
        public ParallelOggStreams(string filename, WaveFormat waveFormat, int bufferSize) : base(filename, waveFormat, bufferSize)
        {

        }

        public override IWaveSource GetReader(int track)
        {
            return new CSCore.Codecs.OGG.OggSource(streams.GetStream(track, true)).ToWaveSource();
        }

        public override IDisposableWritable GetWriter(int track)
        {
            Process ffmpeg = new Process();
            ffmpeg.StartInfo = new ProcessStartInfo("ffmpeg", "-f f32le -ar 48000 -ac 2 -i - -f ogg -c:a libvorbis -ar 48000 -ac 2 -strict 2 -")
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
