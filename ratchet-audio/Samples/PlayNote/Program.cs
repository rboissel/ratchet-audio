using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayNote
{
    class Program
    {
        unsafe class NoteStream : System.IO.Stream
        {

            public override bool CanRead { get { throw new NotImplementedException(); }  }
            public override bool CanSeek { get { throw new NotImplementedException(); } }
            public override bool CanWrite { get { throw new NotImplementedException(); } }
            public override long Length { get { throw new NotImplementedException(); } }
            public override long Seek(long offset, SeekOrigin origin) { throw new NotImplementedException(); }
            public override void SetLength(long value) { throw new NotImplementedException(); }
            public override void Write(byte[] buffer, int offset, int count) { throw new NotImplementedException(); }

            public override long Position
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            public override void Flush() { }

            Type _Format;
            uint _SamplesRate;
            int _NumChannels;
            double _Offset = 0;
            double _Step = 0;

            public override int Read(byte[] buffer, int offset, int count)
            {
                fixed (byte* pBuffer = &buffer[0])
                {
                    if (_Format == typeof(float))
                    {
                        float* pData = (float*)pBuffer;
                        for (int f = 0; f < count / sizeof(float) / _NumChannels; f++)
                        {
                            for (int n = 0; n < _NumChannels; n++)
                            {
                                *pData++ = (float)System.Math.Cos(_Offset);
                            }
                            _Offset += _Step;
                        }
                    }
                    else
                    {
                        // We assume that the client will most likly be a float ieee format.
                        // This is not a guarenty and we should support other format here (byte, int16, int32)
                        // but for simplicity we just implement float
                    }
                }
                return count;
            }


            public void Configure(Type Format, uint SamplesRate, int NumChannels, int Frequency)
            {
                _Format = Format;
                _SamplesRate = SamplesRate;
                _NumChannels = NumChannels;
                _Step = ((System.Math.PI * 2.0) / (double)SamplesRate) * (double)Frequency;
            }
        }


        [STAThread]
        static void Main(string[] args)
        {
            List<Ratchet.Audio.PlaybackDevice> playbackDevice = Ratchet.Audio.PlaybackDevice.GetDevices();
            for (int n = 0; n < playbackDevice.Count; n++)
            {
                // Play a A 440 on each enabled device
                if (playbackDevice[n].Enabled)
                {
                    NoteStream stream = new NoteStream();
                    Ratchet.Audio.PlaybackClient Client = playbackDevice[n].CreateClient(stream);
                    stream.Configure(Client.Format, Client.SampleRate, Client.ChannelCount, 440);
                    Client.Start();
                    System.Threading.Thread.Sleep(10000);
                    Client.Stop();

                }
            }
            
        }
    }
}
