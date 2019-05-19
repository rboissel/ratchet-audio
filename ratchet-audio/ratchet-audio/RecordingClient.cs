using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ratchet.Audio
{
    public abstract class RecordingClient
    {
        public abstract int ChannelCount { get; }
        public abstract Type Format { get; }
        public abstract uint SampleRate { get; }

        public abstract System.IO.Stream Stream { get; }

        public abstract DateTime Start();
        public abstract void Stop();
    }
}
