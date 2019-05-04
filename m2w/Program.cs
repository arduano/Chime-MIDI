using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Un4seen.Bass.AddOn.Midi;

namespace m2w
{
    class Program
    {
        static int ppq = -1;
        static Stream output;
        static Stream input;
        static BASSMIDI bass;
        static long pos = 0;
        static long lastWriteTime = 0;
        static long tempoMult;
        static int samplerate = -1;
        static ulong progress = 0;
        static long scale = 1000000;
        static bool ended = false;
        static void Main(string[] args)
        {
            try
            {
                tempoMult = (long)500000 * samplerate * scale / ppq / 1000000;
                int voices = 2000;

                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "--ppq")
                    {
                        i++;
                        if (i >= args.Length) throw new Exception("Missing value for --ppq");
                        try
                        {
                            ppq = Convert.ToInt32(args[i]);
                        }
                        catch { throw new Exception(args[i] + " is not an integer number"); }
                    }
                    if (args[i] == "--samplerate" || args[i] == "-sr")
                    {
                        if (i + 1 >= args.Length) throw new Exception("Missing value for " + args[i]);
                        i++;
                        try
                        {
                            samplerate = Convert.ToInt32(args[i]);
                        }
                        catch { throw new Exception(args[i] + " is not an integer number"); }
                    }
                    if (args[i] == "--voices" || args[i] == "-v")
                    {
                        if (i + 1 >= args.Length) throw new Exception("Missing value for " + args[i]);
                        i++;
                        try
                        {
                            voices = Convert.ToInt32(args[i]);
                        }
                        catch { throw new Exception(args[i] + " is not an integer number"); }
                    }
                }

                if (ppq == -1) throw new Exception("PPQ not defined. Use --ppq");
                if (samplerate == -1) throw new Exception("Sample rate not defined. Use --samplerate or -sr");
                bass = new BASSMIDI(samplerate, voices);
                bass.LoadDefaultSoundfont();

                input = new BufferedStream(Console.OpenStandardInput());
                output = new BufferedStream(Console.OpenStandardOutput());

                while (true)
                {
                    #region Parse Events
                    pos += ReadVariableLen() * tempoMult;
                    long offset = pos - lastWriteTime;
                    offset = offset - offset % scale;
                    offset /= scale;

                    while (offset > 1 << 20)
                    {
                        bass.WriteBass(1 << 20, output, ref progress);
                        lastWriteTime += (1 << 20) * scale;
                        offset -= 1 << 20;
                    }

                    if (offset > 1)
                    {
                        bass.WriteBass((int)offset, output, ref progress);
                        lastWriteTime += offset * scale;
                    }

                    byte command = (byte)input.ReadByte();
                    if (command < 0x80)
                    {
                        pushback = command;
                        command = prevEvent;
                    }
                    prevEvent = command;
                    byte comm = (byte)(command & 0b11110000);
                    if (comm == 0b10010000)
                    {
                        byte channel = (byte)(command & 0b00001111);
                        byte key = Read();
                        byte vel = (byte)input.ReadByte();
                        bass.SendEvent(BASSMIDIEvent.MIDI_EVENT_NOTE, (vel << 8) | key, channel, 0, 0);
                    }
                    else if (comm == 0b10000000)
                    {
                        byte channel = (byte)(command & 0b00001111);
                        byte key = Read();
                        byte vel = (byte)input.ReadByte();
                        bass.SendEvent(BASSMIDIEvent.MIDI_EVENT_NOTE, key, channel, 0, 0);
                    }
                    else if (comm == 0b10100000)
                    {
                        Read();
                        input.ReadByte();
                    }
                    else if (comm == 0b11000000)
                    {
                        int channel = command & 0b00001111;
                        var program = (byte)input.ReadByte();
                        //bass.SendEvent(BASSMIDIEvent.MIDI_EVENT_PROGRAM, (byte)input.ReadByte(), channel, 0, 0);
                        //Console.Error.WriteLine("Program change " + channel);
                    }
                    else if (comm == 0b11010000)
                    {
                        int channel = command & 0b00001111;
                        var pressure = Read();
                        //bass.SendEvent(BASSMIDIEvent.MIDI_EVENT_CHANPRES, pressure, channel, 0, 0);
                        //Console.Error.WriteLine("Channel Pressure " + channel + " " + pressure);
                    }
                    else if (comm == 0b11100000)
                    {
                        int channel = command & 0b00001111;
                        var b1 = Read();
                        var b2 = (byte)input.ReadByte();
                        //bass.SendEvent(BASSMIDIEvent.MIDI_EVENT_PITCH, (b2 << 7) | b1, channel, 0, 0);
                    }
                    else if (comm == 0b10110000)
                    {
                        int channel = command & 0b00001111;
                        var controller = Read();
                        var value = (byte)input.ReadByte();
                        //bass.SendEvent(BASSMIDIEvent.MIDI_EVENT_CONTROL, (value << 8) | controller, channel, 0, 0);
                        //Console.Error.WriteLine("Control Event "+ channel + " " + controller + " " + value);
                    }
                    else if (command == 0b11110000)
                    {
                        while (Read() != 0b11110111) ;
                    }
                    else if (command == 0b11110100 || command == 0b11110001 || command == 0b11110101 || command == 0b11111001 || command == 0b11111101)
                    {
                        //printf("Undefined\n");
                    }
                    else if (command == 0b11110010)
                    {
                        int channel = command & 0b00001111;
                        Read();
                        Read();
                    }
                    else if (command == 0b11110011)
                    {
                        Read();
                    }
                    else if (command == 0b11110110)
                    {
                    }
                    else if (command == 0b11110111)
                    {
                    }
                    else if (command == 0b11111000)
                    {
                    }
                    else if (command == 0b11111010)
                    {
                    }
                    else if (command == 0b11111100)
                    {
                    }
                    else if (command == 0b11111110)
                    {
                    }
                    else if (command == 0xFF)
                    {
                        command = Read();
                        if (command == 0x00)
                        {
                            if (Read() != 2)
                            {
                                throw new Exception("Corrupt Track");
                            }
                        }
                        else if ((command >= 0x01 &&
                                command <= 0x0A) || command == 0x7F)
                        {
                            int size = (int)ReadVariableLen();
                            for (int i = 0; i < size; i++)
                                input.ReadByte();
                        }
                        else if (command == 0x20)
                        {
                            command = (byte)input.ReadByte();
                            if (command != 1)
                            {
                                throw new Exception("Corrupt Track");
                            }
                            input.ReadByte();
                        }
                        else if (command == 0x21)
                        {
                            command = (byte)input.ReadByte();
                            if (command != 1)
                            {
                                throw new Exception("Corrupt Track");
                            }
                            input.ReadByte();
                            //TODO:  MIDI port
                        }
                        else if (command == 0x2F)
                        {
                            command = (byte)input.ReadByte();
                            if (command != 0)
                            {
                                throw new Exception("Corrupt Track");
                            }
                            ended = true;
                            bass.SendEvent(BASSMIDIEvent.MIDI_EVENT_END_TRACK, 0, 0, 0, samplerate * 2);
                            bass.SendEvent(BASSMIDIEvent.MIDI_EVENT_END, 0, 0, 0, samplerate * 2);
                        }
                        else if (command == 0x51)
                        {
                            command = (byte)input.ReadByte();
                            if (command != 3)
                            {
                                throw new Exception("Corrupt Track");
                            }
                            int btempo = 0;
                            for (int i = 0; i != 3; i++)
                                btempo = (int)((btempo << 8) | (byte)input.ReadByte());
                            tempoMult = (long)btempo * samplerate * scale / ppq / 1000000;
                        }
                        else if (command == 0x54)
                        {
                            command = (byte)input.ReadByte();
                            if (command != 5)
                            {
                                throw new Exception("Corrupt Track");
                            }
                            input.ReadByte();
                            input.ReadByte();
                            input.ReadByte();
                            input.ReadByte();
                        }
                        else if (command == 0x58)
                        {
                            command = (byte)input.ReadByte();
                            if (command != 4)
                            {
                                throw new Exception("Corrupt Track");
                            }
                            input.ReadByte();
                            input.ReadByte();
                            input.ReadByte();
                            input.ReadByte();
                        }
                        else if (command == 0x59)
                        {
                            command = (byte)input.ReadByte();
                            if (command != 2)
                            {
                                throw new Exception("Corrupt Track");
                            }
                            input.ReadByte();
                            input.ReadByte();
                            //TODO: Key Signature
                        }
                        else
                        {
                            throw new Exception("Corrupt Track");
                        }
                    }
                    else
                    {
                        throw new Exception("Corrupt Track");
                    }
                    #endregion

                    if (ended)
                    {
                        while (bass.WriteBass(samplerate, output, ref progress)) continue;
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                Console.Error.WriteLine(e.StackTrace);
                Console.Error.WriteLine(e.InnerException);
            }
            output.Close();
        }

        static int pushback = -1;
        static byte prevEvent = 0;
        static byte Read()
        {
            if (pushback == -1) return (byte)input.ReadByte();
            else
            {
                byte temp = (byte)pushback;
                pushback = -1;
                return temp;
            }
        }

        static long ReadVariableLen()
        {
            byte c;
            int val = 0;
            for (int i = 0; i < 4; i++)
            {
                c = (byte)input.ReadByte();
                if (c > 0x7F)
                {
                    val = (val << 7) | (c & 0x7F);
                }
                else
                {
                    val = val << 7 | c;
                    return val;
                }
            }
            return val;
        }
    }
}
