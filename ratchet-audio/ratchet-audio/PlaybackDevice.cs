using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ratchet.Audio
{
    public abstract class PlaybackDevice
    {
        public abstract bool Enabled { get; }
        public abstract string Name { get; }
        public static List<PlaybackDevice> GetDevices() { return Factory.One.GetPlaybackDevices(); }

        /// <summary>
        /// Create a new Playback client using the default settings for this device.
        /// </summary>
        /// <param name="Stream">The stream used to get the sound data</param>
        /// <returns>The new playback client is returned</returns>
        public abstract PlaybackClient CreateClient(System.IO.Stream Stream);
    }
}
