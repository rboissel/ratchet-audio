using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ratchet.Audio
{
    public abstract class RecordingDevice
    {
        public abstract bool Enabled { get; }
        public abstract string Name { get; }
        public static List<RecordingDevice> GetDevices() { return Factory.One.GetRecordingDevices(); }

    }
}
