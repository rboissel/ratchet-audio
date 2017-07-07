using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Ratchet.Audio
{
    internal class PlaybackDevice_WindowsCoreApi : PlaybackDevice
    {

        const ushort WAVE_FORMAT_PCM = 1;

        static Guid IID_IAudioClient = new Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2");

        [StructLayout(LayoutKind.Sequential, Pack =1)]
        internal struct WAVEFORMATEX
        {

            public ushort wFormatTag;
            public ushort nChannels;
            public uint nSamplesPerSec;
            public uint nAvgBytesPerSec;
            public ushort nBlockAlign;
            public ushort wBitsPerSample;
            public ushort cbSize;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct WAVEFORMATEXTENSIBLE
        {
            WAVEFORMATEX Format;
            public ushort wReserved;
            public uint dwChannelMask;
            public Guid SubFormat;
        }

        public override bool Enabled
        {
            get
            {
                uint state = 0;
                _IDevice.GetState(out state);
                return (state & 0x1) != 0;
            }
        }

        string _Name;
        public override string Name
        {
            get { return _Name; }
        }

        int _NumChannels;

        PropertyStore_WindowsCoreApi _PropertyStore;

        unsafe string ReadString(ulong pStr)
        {
            if (pStr == 0) { return ""; }
            List<byte> array = new List<byte>();
            byte* p = (byte*)pStr;
            while (*p != 0 || *(p + 1) != 0)
            {
                array.Add(*p);
                p++;
                array.Add(*p);
                p++;
            }
            return System.Text.Encoding.Unicode.GetString(array.ToArray());
        }

        Factory_WindowsCoreApi.IMMDevice _IDevice;
        internal PlaybackDevice_WindowsCoreApi(Factory_WindowsCoreApi.IMMDevice IDevice)
        {
            _IDevice = IDevice;
            _PropertyStore = new PropertyStore_WindowsCoreApi(IDevice);
            _Name = ReadString(_PropertyStore.GetProperty(14));
        }

        WAVEFORMATEX createWaveFormat(int BitPerSample, int SamplePerSecond, int ChannelCount)
        {
            WAVEFORMATEX waveformat = new WAVEFORMATEX();
            waveformat.wFormatTag = WAVE_FORMAT_PCM;
            waveformat.wBitsPerSample = (ushort)BitPerSample;
            waveformat.nBlockAlign = (ushort)((ChannelCount * BitPerSample) / 8);
            waveformat.nAvgBytesPerSec = (uint)(SamplePerSecond * (uint)waveformat.nBlockAlign);
            waveformat.nChannels = (ushort)ChannelCount;
            waveformat.nSamplesPerSec = (uint)SamplePerSecond;
            return waveformat;
        }

        void GetMixFormat()
        {

        }
        enum WAVE_FORMAT
        {
            PCM = 1,
            IEEE = 6
        }

        static Guid FormatEx_IEEE = new Guid("00000003-0000-0010-8000-00AA00389B71");
        static Guid FormatEx_PCM = new Guid("00000001-0000-0010-8000-00AA00389B71");
        public unsafe override PlaybackClient CreateClient(System.IO.Stream Stream)
        {
            PlaybackClient_WindowsCoreApi.IAudioClient IAudioClient;        
            _IDevice.Activate(IID_IAudioClient, (uint)PlaybackClient_WindowsCoreApi.CLSCTX.CLSCTX_ALL, new IntPtr(0), out IAudioClient);
            IntPtr rawFormatPtr = new IntPtr();
            IAudioClient.GetMixFormat(out rawFormatPtr);
            WAVEFORMATEX* pFormat = (WAVEFORMATEX*)rawFormatPtr.ToPointer();
            if (pFormat->wBitsPerSample % 8 != 0) { throw new Exception("Unsupported bits per sample value"); }
            Type dataFormat = typeof(byte);
            if (pFormat->wFormatTag == 0xFFFE)
            {
                WAVEFORMATEXTENSIBLE* pFormatEx = (WAVEFORMATEXTENSIBLE*)pFormat;
                if (pFormatEx->SubFormat == FormatEx_IEEE)
                {
                    switch (pFormat->wBitsPerSample)
                    {
                        case 0: case 32: dataFormat = typeof(float); break;
                        case 64: dataFormat = typeof(double); break;
                        default: throw new Exception("Unsupported underlying data format");
                    }
                }
                else if (pFormatEx->SubFormat == FormatEx_PCM)
                {
                    switch (pFormat->wBitsPerSample)
                    {
                        case 8: dataFormat = typeof(byte); break;
                        case 16: dataFormat = typeof(Int16); break;
                        case 32: dataFormat = typeof(Int32); break;
                        case 64: dataFormat = typeof(Int64); break;
                        default: throw new Exception("Unsupported underlying data format");
                    }
                }
            }
            else
            {
                switch ((WAVE_FORMAT)pFormat->wFormatTag)
                {
                    case WAVE_FORMAT.PCM:
                        switch (pFormat->wBitsPerSample)
                        {
                            case 8: dataFormat = typeof(byte); break;
                            case 16: dataFormat = typeof(Int16); break;
                            case 32: dataFormat = typeof(Int32); break;
                            case 64: dataFormat = typeof(Int64); break;
                            default: throw new Exception("Unsupported underlying data format");
                        }
                        break;
                    case WAVE_FORMAT.IEEE:
                        switch (pFormat->wBitsPerSample)
                        {
                            case 0: case 32: dataFormat = typeof(float); break;
                            case 64: dataFormat = typeof(double); break;
                            default: throw new Exception("Unsupported underlying data format");
                        }
                        break;
                }
            }
            try
            {
                IAudioClient.Initialize(PlaybackClient_WindowsCoreApi.AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_SHARED, 0, 10000000, 0, new IntPtr(pFormat), Guid.Empty);
            }
            catch { throw new Exception("Unexpected error when creating the client"); }
            return new PlaybackClient_WindowsCoreApi(IAudioClient, Stream, pFormat->nChannels, pFormat->nBlockAlign, pFormat->nSamplesPerSec, dataFormat);
        }

        public override string ToString()
        {
            return _Name;
        }
    }
}
