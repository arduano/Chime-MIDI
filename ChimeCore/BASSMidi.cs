﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Midi;

namespace ChimeCore
{
    public class BASSMIDI
    {
        public int Handle { get; private set; }

        public static int Samplerate { get; private set; }
        static BASS_MIDI_FONTEX[] fontarr;

        public static void Init(int samplerate)
        {
            Bass.BASS_Free();
            if (!Bass.BASS_Init(0, samplerate, BASSInit.BASS_DEVICE_NOSPEAKER, IntPtr.Zero))
                throw new Exception();
            Samplerate = samplerate;
        }

        public static void Dispose()
        {
            Bass.BASS_Free();
        }

        public BASSMIDI(int samplerate, int voices)
        {
            Handle = BassMidi.BASS_MIDI_StreamCreate(16,
                BASSFlag.BASS_SAMPLE_FLOAT |
                BASSFlag.BASS_STREAM_DECODE |
                BASSFlag.BASS_MIDI_SINCINTER,
                Samplerate);

            if (Handle == 0)
            {
                var error = Bass.BASS_ErrorGetCode();
                throw new Exception(error.ToString());
            }

            Bass.BASS_ChannelSetAttribute(Handle, BASSAttribute.BASS_ATTRIB_MIDI_VOICES, voices);
            Bass.BASS_ChannelSetAttribute(Handle, BASSAttribute.BASS_ATTRIB_SRC, 3);

            Bass.BASS_ChannelFlags(Handle, BASSFlag.BASS_MIDI_NOFX, BASSFlag.BASS_MIDI_NOFX);

            BassMidi.BASS_MIDI_StreamSetFonts(Handle, fontarr, fontarr.Length);
        }

        public static void LoadDefaultSoundfont()
        {
            String omconfig = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!String.IsNullOrEmpty(omconfig))
                omconfig = Path.Combine(omconfig, "OmniMIDI", "lists", "OmniMIDI_A.omlist");
            List<BASS_MIDI_FONTEX> fonts = new List<BASS_MIDI_FONTEX>();
            if (File.Exists(omconfig))
            {
                String[] lines = File.ReadAllLines(omconfig, Encoding.UTF8);


                BASS_MIDI_FONTEX currfont = new BASS_MIDI_FONTEX();
                String currfilename = null;
                bool xgdrums = false;
                bool add = true;

                int lineno = 0;

                foreach (String line in lines)
                {
                    lineno++;

                    if (String.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                        continue;

                    if (line == "sf.start")
                    {
                        currfont = new BASS_MIDI_FONTEX(0, -1, -1, -1, 0, 0);
                        currfilename = null;
                        xgdrums = false;
                        add = true;
                        continue;
                    }

                    if (line == "sf.end")
                    {
                        if (add)
                        {
                            if (currfilename == null)
                            {
                                throw new Exception("Missing filename at line " + lineno);
                            }

                            currfont.font = BassMidi.BASS_MIDI_FontInit(currfilename,
                                xgdrums ? BASSFlag.BASS_MIDI_FONT_XGDRUMS : BASSFlag.BASS_DEFAULT);

                            if (currfont.font != 0)
                            {
                                fonts.Add(currfont);

                                BassMidi.BASS_MIDI_FontLoad(currfont.font, currfont.spreset, currfont.sbank);
                            }
                        }
                        currfilename = null;
                        continue;
                    }

                    if (!line.StartsWith("sf."))
                    {
                        throw new Exception("Invalid line " + lineno);
                    }

                    int idx = line.IndexOf(" = ");
                    if (idx < 4)
                    {
                        throw new Exception("Invalid instruction at line " + lineno);
                    }

                    String instr = line.Substring(3, idx - 3);
                    String idata = line.Substring(idx + 3);

                    switch (instr)
                    {
                        case "path": currfilename = idata; break;
                        case "enabled": add = idata != "0"; break;
                        case "srcb": currfont.sbank = int.Parse(idata); break;
                        case "srcp": currfont.spreset = int.Parse(idata); break;
                        case "desb": currfont.dbank = int.Parse(idata); break;
                        case "desp": currfont.dpreset = int.Parse(idata); break;
                        case "xgdrums": xgdrums = idata != "0"; break;

                        default:
                            throw new Exception("Invalid instruction at line " + lineno);
                    }
                }

                fontarr = fonts.ToArray();
                Array.Reverse(fontarr);
            }
            else
            {
                throw new Exception("OmniMIDI config file missing");
            }
        }

        public bool WriteBass(int buflen, Stream bs, ref ulong progress)
        {
            buflen <<= 3;
            byte[] buf = new byte[buflen];

            int ret = Bass.BASS_ChannelGetData(Handle, buf, buflen | (int)BASSData.BASS_DATA_FLOAT);
            if (ret > 0)
            {
                progress += (uint)ret;
                bs.Write(buf, 0, ret);
                return true;
            }
            else
            {
                var err = Bass.BASS_ErrorGetCode();
                if (err != BASSError.BASS_ERROR_ENDED)
                    throw new Exception("ret " + ret + " " + Bass.BASS_ErrorGetCode());
                return false;
            }
        }

        public float[] WriteFloatArray(int buflen, ref ulong progress)
        {
            byte[] buf = new byte[buflen * 4];
            float[] flt = new float[buflen];

            int ret = Bass.BASS_ChannelGetData(Handle, buf, buflen * 4);
            if (ret > 0)
            {
                progress += (uint)ret;
                Buffer.BlockCopy(buf, 0, flt, 0, buflen * 4);
                return flt;
            }
            else
            {
                var err = Bass.BASS_ErrorGetCode();
                if (err != BASSError.BASS_ERROR_ENDED)
                    throw new Exception("ret " + ret + " " + Bass.BASS_ErrorGetCode());
                return null;
            }
        }

        public int KShortMessage(int dwParam1, int sampleoffset)
        {
            if ((byte)dwParam1 == 0xFF)
                return 1;

            byte cmd = (byte)dwParam1;

            BASS_MIDI_EVENT ev;

            if (cmd < 0xA0) //Note
            {
                ev = new BASS_MIDI_EVENT(BASSMIDIEvent.MIDI_EVENT_NOTE,
                    cmd < 0x90 ? (byte)(dwParam1 >> 8) : (ushort)(dwParam1 >> 8), (int)dwParam1 & 0xF, 0, sampleoffset << 3);
            }
            else if (cmd < 0xB0) //AfterTouch
            {
                ev = new BASS_MIDI_EVENT(BASSMIDIEvent.MIDI_EVENT_KEYPRES,
                    (ushort)(dwParam1 >> 8), (int)dwParam1 & 0xF, 0, sampleoffset << 3);
            }
            else if (cmd < 0xC0) //Control
            {
                //TODO
                return 0;
            }
            else if (cmd < 0xD0) //InstrumentSelect
            {
                ev = new BASS_MIDI_EVENT(BASSMIDIEvent.MIDI_EVENT_PROGRAM,
                    (byte)(dwParam1 >> 8), (int)dwParam1 & 0xF, 0, sampleoffset << 3);
            }
            else if (cmd < 0xE0) //???
            {
                ev = new BASS_MIDI_EVENT(BASSMIDIEvent.MIDI_EVENT_CHANPRES,
                    (byte)(dwParam1 >> 8), (int)dwParam1 & 0xF, 0, sampleoffset << 3);
            }
            else if (cmd < 0xF0) //PitchBend
            {
                //TODO: check bit pack
                ev = new BASS_MIDI_EVENT(BASSMIDIEvent.MIDI_EVENT_PITCH,
                    (int)((byte)(dwParam1 >> 16) | ((dwParam1 & 0x7F00) >> 1)), (int)dwParam1 & 0xF, 0, sampleoffset << 3);
            }
            else
            {
                return 0;
            }

            BassStreamEvents(new BASS_MIDI_EVENT[] { ev });

            return 0;
        }

        public int SendEvent(BASSMIDIEvent type, int param, int chan, int tick, int time)
        {
            var ev = new BASS_MIDI_EVENT(type, param, chan, tick, time << 3);
            var mode = BASSMIDIEventMode.BASS_MIDI_EVENTS_TIME | BASSMIDIEventMode.BASS_MIDI_EVENTS_STRUCT;
            return BassMidi.BASS_MIDI_StreamEvents(Handle, mode, new BASS_MIDI_EVENT[] { ev });
        }

        public int BassStreamEvents(BASS_MIDI_EVENT[] events)
        {
            var mode = BASSMIDIEventMode.BASS_MIDI_EVENTS_TIME | BASSMIDIEventMode.BASS_MIDI_EVENTS_STRUCT;
            return BassMidi.BASS_MIDI_StreamEvents(Handle, mode, events);
        }
    }
}
