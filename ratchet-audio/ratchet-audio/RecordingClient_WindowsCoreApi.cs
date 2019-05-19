using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Ratchet.Audio
{
    class RecordingClient_WindowsCoreApi : RecordingClient
    {
        class RecordingStream : System.IO.Stream
        {
            public override bool CanRead { get { return true; } }

            public override bool CanSeek { get { return false; } }

            public override bool CanWrite { get { return false; } }

            public override long Length { get { throw new NotSupportedException(); } }

            public override long Position { get { throw new NotSupportedException(); }  set { throw new NotSupportedException(); } }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int ReadCount = 0;
                lock (this)
                {
                    while (count > 0)
                    {
                        if (_Frames.Count == 0) { return ReadCount; }
                        byte[] currentFrame = _Frames.Peek();
                        int localCount = count;
                        if (_FrameOffset + localCount >= currentFrame.Length) { localCount = currentFrame.Length - _FrameOffset; }
                        for (int n = 0; n < localCount; n++)
                        {
                            buffer[offset] = currentFrame[_FrameOffset];
                            offset++;
                            _FrameOffset++;
                        }
                        ReadCount += localCount;
                        count -= localCount;
                        if (_FrameOffset >= currentFrame.Length)
                        {
                            _Parent.PutBuffer(_Frames.Dequeue());
                            _FrameOffset = 0;
                        }
                    }
                    return ReadCount;
                }
            }

            public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
            public override void SetLength(long value) { throw new NotSupportedException(); }
            public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }

            RecordingClient_WindowsCoreApi _Parent;
            internal RecordingStream(RecordingClient_WindowsCoreApi Parent)
            {
                _Parent = Parent;
            }

            Queue<byte[]> _Frames = new Queue<byte[]>();
            int _FrameOffset = 0;

            internal void PushFrame(byte[] Frame)
            {
                lock (this)
                {
                    _Frames.Enqueue(Frame);
                }
            }
        }
        static Guid IID_IAudioCaptureClient = new Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317");

        [System.Runtime.InteropServices.ComImport]
        [Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317")]
        [System.Runtime.InteropServices.InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IAudioCaptureClient
        {
            void GetBuffer([Out] out IntPtr ppData, [Out] out UInt32 pNumFramesToRead, [Out] out UInt32 pdwFlag, [Out] out UInt64 pu64DevicePosition, [Out] out UInt64 pu64QPCPosition);
            void ReleaseBuffer([In] UInt32 NumFramesRead);
            void GetNextPacketSize([Out] out UInt32 pNumFramesInNextPacket);
        }

        Factory_WindowsCoreApi.IAudioClient _IAudioClient;
        IAudioCaptureClient _IAudioCaptureClient;

        Queue<byte[]> _AvailableFullBuffer = new Queue<byte[]>();
        internal void PutBuffer(byte[] Buffer)
        {
            lock (this)
            {
                _AvailableFullBuffer.Enqueue(Buffer);
            }
        }
        byte[] GetBuffer(int Size)
        {
            lock (this)
            {
                if (_AvailableFullBuffer.Count == 0) { return new byte[Size]; }
                int count = _AvailableFullBuffer.Count;
                for (int n = 0; n < count; n++)
                {
                    byte[] buffer = _AvailableFullBuffer.Dequeue();
                    if (buffer.Length <= Size) { return buffer; }
                    _AvailableFullBuffer.Enqueue(buffer);
                }
                return new byte[Size];
            }
        }

        System.Threading.Thread _Thread;
        int _ChannelCount = 0;
        public override int ChannelCount { get { return _ChannelCount; } }

        uint _SampleRate = 0;
        public override uint SampleRate { get { return _SampleRate; } }

        Type _Format;
        public override Type Format { get { return _Format; } }

        int _FrameSize = 0;
        uint _BufferFrameCount = 0;
        public RecordingClient_WindowsCoreApi(Factory_WindowsCoreApi.IAudioClient Client, int NumChannel, int FrameSize, uint SamplesRate, Type DataFormat)
        {
            object opaqueService;
            _IAudioClient = Client;
            _IAudioClient.GetService(ref IID_IAudioCaptureClient, out opaqueService);
            _IAudioCaptureClient = (IAudioCaptureClient)opaqueService;
            _IAudioClient.GetBufferSize(out _BufferFrameCount);
            _SampleRate = SamplesRate;
            _Format = DataFormat;
            _ChannelCount = NumChannel;

            _IAudioClient = Client;
            _Stream = new RecordingStream(this);
            _Thread = new System.Threading.Thread(Loop);
            _FrameSize = FrameSize;


        }

        void Loop()
        {
            int padding = 0;
            _IAudioClient.GetCurrentPadding(out padding);

            while (true)
            {
                IntPtr pBuffer;
                System.Threading.Thread.Sleep(1);
                lock (this)
                {
                    uint numFrame;
                    uint flag;
                    ulong position, hpTimer;
                    bool discontinuity = false;

                    _IAudioCaptureClient.GetBuffer(out pBuffer, out numFrame, out flag, out position, out hpTimer);

                    discontinuity = (flag & 1) != 0;
                    flag = (uint)(flag & ~1);

                    if (numFrame > 0 && flag == 0 && pBuffer.ToInt64() != 0)
                    {
                        byte[] buffer = GetBuffer((int)(numFrame * _FrameSize));
                        System.Runtime.InteropServices.Marshal.Copy(pBuffer, buffer, 0, (int)(numFrame * _FrameSize));
                        _IAudioCaptureClient.ReleaseBuffer(numFrame);
                        _Stream.PushFrame(buffer);
                    }
                    else
                    {
                        _IAudioCaptureClient.ReleaseBuffer(numFrame);
                    }
                }
            }
        }

        RecordingStream _Stream;
        public override Stream Stream { get { return _Stream; } }

        bool _Started = false;


        public override DateTime Start()
        {
            lock (this)
            {
                if (_Started) { throw new Exception("Already Started"); }
                _Started = true;
                _IAudioClient.Start();

                try
                {
                    IntPtr pBuffer;
                    uint numFrame;
                    uint flag;
                    ulong position, hpTimer;
                    _IAudioCaptureClient.GetBuffer(out pBuffer, out numFrame, out flag, out position, out hpTimer);
                    if (numFrame > 0 && flag == 0 && pBuffer.ToInt64() != 0)
                    {
                        byte[] buffer = GetBuffer((int)(numFrame * _FrameSize));
                        System.Runtime.InteropServices.Marshal.Copy(pBuffer, buffer, 0, (int)(numFrame * _FrameSize));
                        _IAudioCaptureClient.ReleaseBuffer(numFrame);
                        _Stream.PushFrame(buffer);
                    }
                    else
                    {
                        _IAudioCaptureClient.ReleaseBuffer(numFrame);
                    }
                }
                catch
                { throw new Exception("Failed to start the recording"); }
                _Thread.Start();
            }

            return DateTime.Now;
        }

        public override void Stop()
        {
            lock (this)
            {
                if (!_Started) { throw new Exception("Not Started"); }
                _IAudioClient.Stop();
                _Thread.Abort();
                _Started = false;
                _Thread = new System.Threading.Thread(Loop);
            }
        }
    }
}
