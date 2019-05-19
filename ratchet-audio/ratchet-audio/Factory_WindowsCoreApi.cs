using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace Ratchet.Audio
{
    class Factory_WindowsCoreApi : Factory
    {
        const ushort WAVE_FORMAT_PCM = 1;

        internal enum AUDCLNT_SHAREMODE : int
        {
            AUDCLNT_SHAREMODE_SHARED = 0,
            AUDCLNT_SHAREMODE_EXCLUSIVE
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
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

        enum WAVE_FORMAT
        {
            PCM = 1,
            IEEE = 6
        }

        internal static Guid IID_IAudioClient = new Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2");
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
            int GetService(ref Guid interfaceId, [Out, MarshalAs(UnmanagedType.IUnknown)] out object renderClient);
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

        static Guid FormatEx_IEEE = new Guid("00000003-0000-0010-8000-00AA00389B71");
        static Guid FormatEx_PCM = new Guid("00000001-0000-0010-8000-00AA00389B71");
        internal static unsafe Factory_WindowsCoreApi.IAudioClient CreateClient(IMMDevice IDevice, out WAVEFORMATEX format, out Type dataFormat)
        {
            Factory_WindowsCoreApi.IAudioClient IAudioClient;
            IDevice.Activate(Factory_WindowsCoreApi.IID_IAudioClient, (uint)PlaybackClient_WindowsCoreApi.CLSCTX.CLSCTX_ALL, new IntPtr(0), out IAudioClient);

            IntPtr rawFormatPtr = new IntPtr();
            IAudioClient.GetMixFormat(out rawFormatPtr);
            WAVEFORMATEX* pFormat = (WAVEFORMATEX*)rawFormatPtr.ToPointer();
            if (pFormat->wBitsPerSample % 8 != 0) { throw new Exception("Unsupported bits per sample value"); }
            dataFormat = typeof(byte);
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
                IAudioClient.Initialize(Factory_WindowsCoreApi.AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_SHARED, 0, 10000000, 0, new IntPtr(pFormat), Guid.Empty);
            }
            catch { throw new Exception("Unexpected error when creating the client"); }

            format = *pFormat;
            return IAudioClient;
        }

        [System.Runtime.InteropServices.ComImport]
        [Guid("7991EEC9-7E89-4D85-8390-6C703CEC60C0")]
        [System.Runtime.InteropServices.InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IMMNotificationClient
        {
            void OnDeviceStateChanged([MarshalAs(UnmanagedType.LPWStr)] string deviceId, uint deviceState);
            void OnDeviceAdded([MarshalAs(UnmanagedType.LPWStr)] string deviceId);
            void OnDeviceRemoved([MarshalAs(UnmanagedType.LPWStr)] string deviceId);
            void OnDefaultDeviceChanged(EDataFlow dataFlow,  uint role, [MarshalAs(UnmanagedType.LPWStr)] string deviceId);
            void OnPropertyValueChanged([In, MarshalAs(UnmanagedType.LPWStr)] string deviceId, uint key);
        }

        class MMNotificationClientCallbacks : IMMNotificationClient
        {
            Factory_WindowsCoreApi _Parent;
            public MMNotificationClientCallbacks(Factory_WindowsCoreApi Parent)
            {
                _Parent = Parent;
            }

     
            public void OnDefaultDeviceChanged(EDataFlow dataFlow, uint role, [MarshalAs(UnmanagedType.LPWStr)] string deviceId)
            {
                lock (_Parent)
                {
                }
            }

            public void OnDeviceAdded([MarshalAs(UnmanagedType.LPWStr)] string deviceId)
            {
                lock (_Parent)
                {
                    _Parent.RefreshDevices();
                }
            }

            public void OnDeviceRemoved([MarshalAs(UnmanagedType.LPWStr)] string deviceId)
            {
                lock (_Parent)
                {
                    _Parent.RefreshDevices();
                }
            }

            public void OnDeviceStateChanged([MarshalAs(UnmanagedType.LPWStr)] string deviceId, uint deviceState)
            {
                lock (_Parent)
                {
                }
            }

            public void OnPropertyValueChanged([In, MarshalAs(UnmanagedType.LPWStr)] string deviceId, uint key)
            {
                lock (_Parent)
                {
                }
            }
        }

        [System.Runtime.InteropServices.ComImport]
        [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
        [System.Runtime.InteropServices.InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IMMDeviceEnumerator
        {
            void EnumAudioEndpoints(EDataFlow dataFlow, int dwStateMask, [Out] out IMMDeviceCollection pDevices);
            void GetDefaultAudioEndpoint(EDataFlow dataFlow, int role, [Out] out IMMDevice ppDevice);
            void GetDevice([MarshalAs(UnmanagedType.LPWStr)]string pwstrId, [Out] out IMMDevice ppDevice);
            void RegisterEndpointNotificationCallback(IMMNotificationClient pNotify);
        }
 
        [System.Runtime.InteropServices.ComImport]
        [Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
        [System.Runtime.InteropServices.InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IMMDeviceCollection
        {
            void GetCount([Out] out uint Count);
            void Item(uint nDevice, [Out] out IMMDevice ppDevice);

        }

        [System.Runtime.InteropServices.ComImport]
        [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
        [System.Runtime.InteropServices.InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IMMDevice
        {
            void Activate(ref Guid iid, uint dwClsCtx, IntPtr pActivationParams, [Out] out IAudioClient ppInterface);
            void OpenPropertyStore(uint stgmAccess, [Out] out IPropertyStore propertyStore);
            void GetId([Out, MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);
            void GetState([Out] out uint pdwState);
        }

        internal struct PROPERTYKEY
        {
            public PROPERTYKEY(Guid fmtid, int pid)
            {
                this.fmtid = fmtid;
                this.pid = pid;
            }

            public Guid fmtid;
            public int pid;
        }

        internal struct PROPVARIANT
        {
            public ushort vt;
            short wReserved1;
            short wReserved2;
            short wReserved3;
            public ulong data;
        }

            [System.Runtime.InteropServices.ComImport]
        [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
        [System.Runtime.InteropServices.InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IPropertyStore
        {
            void GetCount(out int count);
            void GetAt(int iProp, [Out] out PROPERTYKEY pkey);
            void GetValue(ref PROPERTYKEY key, [Out] out PROPVARIANT pv);
            void SetValue(ref PROPERTYKEY key, ref PROPVARIANT propvar);
            void Commit();
        }


        Factory_WindowsCoreApi()
        {
            _Callbacks = new MMNotificationClientCallbacks(this);
            SetupDevices();
        }

        static T CreateInstance<T>(string GUID)
        {
            return (T)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid(GUID)));
        }

        public override List<PlaybackDevice> GetPlaybackDevices()
        {
            lock (this)
            {
                return new List<PlaybackDevice>(_PlaybackDevices.Values);
            }
        }

        public override List<RecordingDevice> GetRecordingDevices()
        {
            lock (this)
            {
                return new List<RecordingDevice>(_RecordingDevices.Values);
            }
        }

        Dictionary<string, PlaybackDevice_WindowsCoreApi> _PlaybackDevices = new Dictionary<string, PlaybackDevice_WindowsCoreApi>();
        Dictionary<string, RecordingDevice_WindowsCoreApi> _RecordingDevices = new Dictionary<string, RecordingDevice_WindowsCoreApi>();

        MMNotificationClientCallbacks _Callbacks;
        IMMDeviceEnumerator _IMMDeviceEnumerator;
        void SetupDevices()
        {
            lock (this)
            {
                _IMMDeviceEnumerator = CreateInstance<IMMDeviceEnumerator>("BCDE0395-E52F-467C-8E3D-C4579291692E");

                _IMMDeviceEnumerator.RegisterEndpointNotificationCallback(_Callbacks);
            }
            RefreshDevices();

        }

        void RefreshDevices()
        {
            lock (this)
            {
                IMMDeviceCollection IMMDeviceCollection;
                _IMMDeviceEnumerator.EnumAudioEndpoints(EDataFlow.eRender, 0xF, out IMMDeviceCollection);
                uint deviceCount = 0;
                IMMDeviceCollection.GetCount(out deviceCount);

                for (uint n = 0; n < deviceCount; n++)
                {
                    IMMDevice IMMDevice;
                    IMMDeviceCollection.Item(n, out IMMDevice);
                    string uid = "";
                    IMMDevice.GetId(out uid);
                    if (!_PlaybackDevices.ContainsKey(uid))
                    {
                        _PlaybackDevices.Add(uid, new PlaybackDevice_WindowsCoreApi(IMMDevice));
                    }
                }

                {
                    IMMDevice Default;
                    _IMMDeviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, 0x0, out Default);
                    string uid = "";
                    Default.GetId(out uid);
                    if (!_PlaybackDevices.ContainsKey(uid))
                    {
                        _PlaybackDevices.Add(uid, new PlaybackDevice_WindowsCoreApi(Default));
                    }
                    _PlaybackDevices[uid]._Default = true;
                }

                _IMMDeviceEnumerator.EnumAudioEndpoints(EDataFlow.eCapture, 0xF, out IMMDeviceCollection);

                deviceCount = 0;
                IMMDeviceCollection.GetCount(out deviceCount);

                for (uint n = 0; n < deviceCount; n++)
                {
                    IMMDevice IMMDevice;
                    IMMDeviceCollection.Item(n, out IMMDevice);
                    string uid = "";
                    IMMDevice.GetId(out uid);
                    if (!_RecordingDevices.ContainsKey(uid))
                    {
                        _RecordingDevices.Add(uid, new RecordingDevice_WindowsCoreApi(IMMDevice));
                    }
                }
            }
        }

        public enum EDataFlow
        {
            eRender,
            eCapture,
            eAll,
            EDataFlow_enum_count
        }

        public static Factory Create()
        {
            return new Factory_WindowsCoreApi();
        }
    }
}
