using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ratchet.Audio
{
    public abstract class PlaybackClient
    {
        public abstract int ChannelCount { get; }
        public abstract Type Format { get; }
        public abstract uint SampleRate { get; }

        public abstract void Start();
        public abstract void Stop();
    }
}
