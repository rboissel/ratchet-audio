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
        internal enum AUDCLNT_SHAREMODE : int
        {
            AUDCLNT_SHAREMODE_SHARED = 0,
            AUDCLNT_SHAREMODE_EXCLUSIVE
        }

        internal enum CLSCTX : int
        {
            CLSCTX_INPROC_SERVER = 0x1,
            CLSCTX_INPROC_HANDLER = 0x2,
            CLSCTX_LOCAL_SERVER = 0x4,
            CLSCTX_REMOTE_SERVER = 0x10,
            CLSCTX_SERVER = (CLSCTX_INPROC_SERVER | CLSCTX_LOCAL_SERVER | CLSCTX_REMOTE_SERVER),
            CLSCTX_ALL = (CLSCTX_INPROC_HANDLER | CLSCTX_SERVER)
        }

        [System.Runtime.InteropServices.ComImport]
        [Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2")]
        [System.Runtime.InteropServices.InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IAudioClient
        {
            void Initialize(AUDCLNT_SHAREMODE ShareMode, int StreamFlags, long hnsBufferDuration, long hnsPeriodicity, [In] IntPtr Format, Guid AudioSessionGuid);
            void GetBufferSize([Out] out uint numBufferFrames);
            void GetStreamLatency();
            int GetCurrentPadding([Out] out int currentPadding);
            void IsFormatSupported();
            void GetMixFormat([Out] out IntPtr pDeviceFormat);
            void GetDevicePeriod();
            void Start();
            void Stop();
            void Reset();
            int SetEventHandle(IntPtr eventHandle);
            int GetService(ref Guid interfaceId, [Out] out IAudioRenderClient renderClient);
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

        IAudioClient _IAudioClient;
        IAudioRenderClient _IAudioRenderClient;
        System.IO.Stream _Stream;
        uint _BufferFrameCount = 0;
        int _FrameSize = 0;
        uint _BufferDuration = 0;
        byte[] _FullBuffer = null;

        internal PlaybackClient_WindowsCoreApi(IAudioClient IAudioClient, System.IO.Stream Stream, int NumChannel, int FrameSize, uint SamplesRate, Type DataFormat)
        {
            _IAudioClient = IAudioClient;
            _IAudioClient.GetService(ref IID_IAudioRenderClient, out _IAudioRenderClient);
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
            while (true)
            {
                IntPtr pBuffer;
                int padding = 0;
                System.Threading.Thread.Sleep((int)(_BufferDuration / 2));
                lock (this)
                {
                    _IAudioClient.GetCurrentPadding(out padding);
                    int count = _Stream.Read(_FullBuffer, 0, ((int)_BufferFrameCount - padding) * _FrameSize);
                    if (count > 0)
                    {
                        _IAudioRenderClient.GetBuffer(count / _FrameSize, out pBuffer);
                        System.Runtime.InteropServices.Marshal.Copy(_FullBuffer, 0, pBuffer, count);
                        _IAudioRenderClient.ReleaseBuffer(count / _FrameSize, 0);
                    }
                }
            }
        }

        public override void Start()
        {
            lock (this)
            {
                IntPtr pBuffer;
                int count = _Stream.Read(_FullBuffer, 0, (int)_BufferFrameCount * _FrameSize);
                Console.WriteLine(count / _FrameSize);

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
