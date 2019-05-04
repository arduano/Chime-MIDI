using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace ChimeCore
{
    public interface IPSampleProvider : ISampleProvider
    {
        long Position { get; set; }
        long Length { get; }
    }
}
