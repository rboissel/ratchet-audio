using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;


namespace Ratchet.Audio
{
    class PlaybackClient_WindowsCoreApi : PlaybackClient
    {
        internal enum CLSCTX : int
        {
            CLSCTX_INPROC_SERVER = 0x1,
            CLSCTX_INPROC_HANDLER = 0x2,
            CLSCTX_LOCAL_SERVER = 0x4,
            CLSCTX_REMOTE_SERVER = 0x10,
            CLSCTX_SERVER = (CLSCTX_INPROC_SERVER | CLSCTX_LOCAL_SERVER | CLSCTX_REMOTE_SERVER),
            CLSCTX_ALL = (CLSCTX_INPROC_HANDLER | CLSCTX_SERVER)
        }

        static Guid IID_IAudioRenderClient = new Guid("F294ACFC-3146-4483-A7BF-ADDCA7C260E2");

        [System.Runtime.InteropServices.ComImport]
        [Guid("F294ACFC-3146-4483-A7BF-ADDCA7C260E2")]
        [System.Runtime.InteropServices.InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IAudioRenderClient
        {
            int GetBuffer(int numFramesRequested, [Out] out IntPtr dataBufferPointer);
            int ReleaseBuffer(int numFramesWritten, int bufferFlags);
        }

        int _ChannelCount = 0;
        public override int ChannelCount { get { return _ChannelCount; } }

        uint _SampleRate = 0;
        public override uint SampleRate { get { return _SampleRate; } }

        Type _Format;
        public override Type Format { get { return _Format; } }

        System.Threading.Thread _Thread;

        Factory_WindowsCoreApi.IAudioClient _IAudioClient;
        IAudioRenderClient _IAudioRenderClient;
        System.IO.Stream _Stream;
        uint _BufferFrameCount = 0;
        int _FrameSize = 0;
        uint _BufferDuration = 0;
        byte[] _FullBuffer = null;

        internal PlaybackClient_WindowsCoreApi(Factory_WindowsCoreApi.IAudioClient IAudioClient, System.IO.Stream Stream, int NumChannel, int FrameSize, uint SamplesRate, Type DataFormat)
        {
            object opaqueService;
            _IAudioClient = IAudioClient;
            _IAudioClient.GetService(ref IID_IAudioRenderClient, out opaqueService);
            _IAudioRenderClient = (IAudioRenderClient)opaqueService;
            _IAudioClient.GetBufferSize(out _BufferFrameCount);
            _Stream = Stream;
            _FrameSize = FrameSize;
            _BufferDuration = _BufferFrameCount / (SamplesRate / 1000);
            _FullBuffer = new byte[_FrameSize * _BufferFrameCount];
            _Thread = new System.Threading.Thread(Loop);
            _ChannelCount = NumChannel;
            _SampleRate = SamplesRate;
            _Format = DataFormat;
        }



        void Loop()
        {
            int padding = 0;
            _IAudioClient.GetCurrentPadding(out padding);

            int waitTime = (int)((ulong)padding / (_SampleRate / 1000) / 2);
            while (true)
            {
                IntPtr pBuffer;
                System.Threading.Thread.Sleep((int)(waitTime));
                lock (this)
                {
                    _IAudioClient.GetCurrentPadding(out padding);
                    int count = _Stream.Read(_FullBuffer, 0, (((int)_BufferFrameCount - padding) * _FrameSize));
                    if (count > ((int)_BufferFrameCount - padding) * _FrameSize) { throw new Exception("More data provided by than asked for"); }
                    if (count > 0)
                    {
                        _IAudioRenderClient.GetBuffer(count / _FrameSize, out pBuffer);
                        System.Runtime.InteropServices.Marshal.Copy(_FullBuffer, 0, pBuffer, count);
                        _IAudioRenderClient.ReleaseBuffer(count / _FrameSize, 0);
                        waitTime = (int)((ulong)(padding + count / _FrameSize) / (_SampleRate / 1000)) / 2;
                    }
                    if (waitTime == 0 && count == 0) { waitTime = 20; }
                }
            }
        }

        public override void Start()
        {
            lock (this)
            {
                IntPtr pBuffer;
                int count = _Stream.Read(_FullBuffer, 0, (int)_BufferFrameCount * _FrameSize);

                if (count > 0)
                {
                    _IAudioRenderClient.GetBuffer(count / _FrameSize, out pBuffer);
                    System.Runtime.InteropServices.Marshal.Copy(_FullBuffer, 0, pBuffer, count);
                    _IAudioRenderClient.ReleaseBuffer(count / _FrameSize, 0);
                }
                _IAudioClient.Start();
                _Thread.Start();
            }
        }


        public override void Stop()
        {
            lock (this)
            {
                _IAudioClient.Stop();
                _Thread.Abort();
                _Thread = new System.Threading.Thread(Loop);
            }
        }

    }
}
