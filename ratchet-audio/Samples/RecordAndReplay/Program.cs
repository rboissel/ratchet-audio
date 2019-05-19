using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordAndReplay
{
    class Program
    {
        static Ratchet.Audio.PlaybackDevice FindPlaybackDevice()
        {
            Ratchet.Audio.PlaybackDevice selectedDevice = null;
            List<Ratchet.Audio.PlaybackDevice> playbackDevice = Ratchet.Audio.PlaybackDevice.GetDevices();
            for (int n = 0; n < playbackDevice.Count; n++)
            {
                if (playbackDevice[n].Enabled)
                {
                    if (playbackDevice[n].Default) { return playbackDevice[n]; }
                    selectedDevice = playbackDevice[n];
                }
            }
            return selectedDevice;
        }

        static Ratchet.Audio.RecordingDevice FindRecordingDevice()
        {
            Ratchet.Audio.RecordingDevice selectedDevice = null;
            List<Ratchet.Audio.RecordingDevice> recordingDevices = Ratchet.Audio.RecordingDevice.GetDevices();
            for (int n = 0; n < recordingDevices.Count; n++)
            {
                if (recordingDevices[n].Enabled)
                {
                    //if (recordingDevices[n].Default) { return recordingDevices[n].Default; }
                    selectedDevice = recordingDevices[n];
                }
            }
            return selectedDevice;
        }

        // This will create a Ratchet-Audio-Mixer Source. This is a different library that
        // we are using here to adapt to different sampling rate, channel count ...
        // So we don't have to write a full resampler.
        unsafe class RecordingSource : Ratchet.Audio.Mixer.Source<float>
        {
            Ratchet.Audio.RecordingClient _Client;
            public RecordingSource(Ratchet.Audio.RecordingClient Client)
            {
                _Client = Client;
                if (Client.Format != typeof(float)) { throw new Exception("Only support Float format for this sample"); }
                this.SampleRate = Client.SampleRate;
            }

            public override int Read(float[] Buffer, int FrameCount)
            {
                if (Buffer.Length < FrameCount) { FrameCount = Buffer.Length; }

                byte[] recording = new byte[FrameCount * sizeof(float) * _Client.ChannelCount];
                int byteCount = _Client.Stream.Read(recording, 0, recording.Length);
                FrameCount = (byteCount / sizeof(float) / _Client.ChannelCount);
                fixed (byte* pRecording = &recording[0])
                {
                    float* pRecordingFload = (float*)pRecording;
                    for (int n = 0; n < FrameCount; n++)
                    {
                        // We only care about the first channel here for simplicity purposes
                        Buffer[n] = pRecordingFload[n * _Client.ChannelCount];
                    }
                }

                return FrameCount;
            }
        }

        [STAThread]
        static void Main(string[] args)
        {
            Ratchet.Audio.PlaybackDevice playbackDevice = FindPlaybackDevice();
            if (playbackDevice == null)
            {
                Console.WriteLine("No playback device found on this computer.");
                System.Environment.Exit(1);
            }

            Ratchet.Audio.RecordingDevice recordingDevice = FindRecordingDevice();
            if (recordingDevice == null)
            {
                Console.WriteLine("No recording device found on this computer.");
                System.Environment.Exit(1);
            }

            Console.WriteLine("Recording Device: " + recordingDevice.Name);
            Console.WriteLine("Playback Device: " + playbackDevice.Name);

            Ratchet.Audio.RecordingClient recordingClient = recordingDevice.CreateClient();
            recordingClient.Start();
            System.Threading.Thread.Sleep(5 * 1000);
            recordingClient.Stop();

            Ratchet.Audio.Mixer Mixer = new Ratchet.Audio.Mixer();
            Mixer.AddSource(new RecordingSource(recordingClient));
            Mixer.CreateListener(-1.0f, 0.0f, 0.0f, 0);
            Mixer.CreateListener(1.0f, 0.0f, 0.0f, 1);
            Mixer.CreateListener(-1.0f, 0.0f, 0.0f, 2);
            Mixer.CreateListener(1.0f, 0.0f, 0.0f, 3);

            Ratchet.Audio.PlaybackClient audioClient = playbackDevice.CreateClient(Mixer);
            Mixer.OutputChannelCount = audioClient.ChannelCount;
            Mixer.OutputFormat = audioClient.Format;
            Mixer.OutputSampleRate = audioClient.SampleRate;

            audioClient.Start();
            System.Threading.Thread.Sleep(5 * 1000);
            audioClient.Stop();
        }
    }
}
