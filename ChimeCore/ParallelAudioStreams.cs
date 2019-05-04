using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MIDIModificationFramework;
using CSCore;

namespace ChimeCore
{
    class ParallelAudioStreams
    {
        ParallelStream streams;
        public ParallelAudioStreams(string filename)
        {
            streams = new ParallelStream(File.Open(filename, FileMode.Create));
        }

        public abstract  (int track);
    }
}
