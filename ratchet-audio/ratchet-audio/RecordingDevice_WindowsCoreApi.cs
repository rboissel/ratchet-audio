using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        Factory_WindowsCoreApi.IMMDevice _IDevice;
        internal RecordingDevice_WindowsCoreApi(Factory_WindowsCoreApi.IMMDevice IDevice)
        {
            _IDevice = IDevice;
        }

        public override string ToString()
        {
            return _Name;
        }
    }
}
