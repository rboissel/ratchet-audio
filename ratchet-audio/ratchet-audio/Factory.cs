using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ratchet.Audio
{
    public class Factory
    {
        static Factory _One;

        static Factory()
        {
            _One = CreateFactory();
        }

        public static Factory One { get { return _One; } }

        static Factory CreateFactory()
        {
            if (System.Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return Factory_WindowsCoreApi.Create();
            }
            return null;
        }

        public virtual List<PlaybackDevice> GetPlaybackDevices() { return new List<PlaybackDevice>(); }
        public virtual List<RecordingDevice> GetRecordingDevices() { return new List<RecordingDevice>(); }
    }
}
