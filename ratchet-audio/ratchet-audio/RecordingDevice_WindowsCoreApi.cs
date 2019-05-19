using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;

namespace Ratchet.Audio
{
    class RecordingDevice_WindowsCoreApi : RecordingDevice
    {
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

        PropertyStore_WindowsCoreApi _PropertyStore;


        Factory_WindowsCoreApi.IMMDevice _IDevice;
        internal RecordingDevice_WindowsCoreApi(Factory_WindowsCoreApi.IMMDevice IDevice)
        {
            _IDevice = IDevice;
            _PropertyStore = new PropertyStore_WindowsCoreApi(IDevice);
            _Name = WindowsCoreApiTools.ReadString(_PropertyStore.GetProperty(14));
        }

        public override RecordingClient CreateClient()
        {
            Factory_WindowsCoreApi.WAVEFORMATEX format;
            Type dataFormat;
            Factory_WindowsCoreApi.IAudioClient IAudioClient = Factory_WindowsCoreApi.CreateClient(_IDevice, out format, out dataFormat);
            return new RecordingClient_WindowsCoreApi(IAudioClient, format.nChannels, format.nBlockAlign, format.nSamplesPerSec, dataFormat);
        }

        public override string ToString()
        {
            return _Name;
        }
    }
}
