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
            void Activate(ref Guid iid, uint dwClsCtx, IntPtr pActivationParams, [Out] out PlaybackClient_WindowsCoreApi.IAudioClient ppInterface);
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
