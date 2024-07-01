using NAudio.Wave;
using NAudio.CoreAudioApi;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: AudioPlayerFolder <folderPath>");
            return;
        }

        string folderPath = args[0]; // Path to the folder containing audio files

        // Ensure the microphone device is properly set in your system
        var vbCableDeviceNumber = GetVBCableOutputDeviceNumber();


        if (vbCableDeviceNumber == -1)
        {
            Console.WriteLine("VB-CABLE input device not found.");
            return;
        }
        

        while(true)
        {
            ReadAndDeleteAllAudioFiles(folderPath, vbCableDeviceNumber);
            Thread.Sleep(1000);
        }
    }

    static void ReadAndDeleteAllAudioFiles(string folderPath, int vbCableDeviceNumber)
    {
        // Get all audio files in the folder
        var audioFiles = Directory.GetFiles(folderPath, "*.wav")
                                  .OrderBy(f => File.GetCreationTime(f))
                                  .ToList();

        if (audioFiles.Count == 0)
        {
            Console.WriteLine("No audio files found in the specified folder.");
            return;
        }

        foreach (var audioFile in audioFiles)
        {
            Thread feedBackAudio = new Thread(() => PlayAudioFile(-1, audioFile));

            feedBackAudio.Start();
            PlayAudioFile(vbCableDeviceNumber, audioFile);
            while(File.Exists(audioFile))
            {
                try
                {
                    File.Delete(audioFile); // Delete the file after playing
                }
                catch (Exception) { }
            }

        }
    }

    static void PlayAudioFile(int deviceNumber, string audioFilePath)
    {
        var virtualAudioWaveOut = new WaveOutEvent
        {
            DeviceNumber = deviceNumber
        };

        var audioFile = new AudioFileReader(audioFilePath);
        virtualAudioWaveOut.Init(audioFile);

        virtualAudioWaveOut.Play();

        Console.WriteLine($"Playing audio file '{Path.GetFileName(audioFilePath)}' through microphone...");


        // Wait until the end of the audio file
        while (virtualAudioWaveOut.PlaybackState == PlaybackState.Playing)
        {
            Thread.Sleep(500); // Check every 0.5 seconds
        }

        virtualAudioWaveOut.Stop();
        virtualAudioWaveOut.Dispose();

        audioFile.Dispose();

        Console.WriteLine($"Finished playing '{Path.GetFileName(audioFilePath)}'.");

    }

    static int GetVBCableOutputDeviceNumber()
    {
        MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
        MMDeviceCollection devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
        var allDevices = devices.ToList();

        for (int i = 0; i < allDevices.Count; i++)
        {
            var device = allDevices[i];
            Console.WriteLine($"Device {i}: {device.DeviceFriendlyName}, {device.FriendlyName}");
            if (device.DeviceFriendlyName.Contains("Virtual Cable"))
            {
                Console.WriteLine($">FOUND Device {i}: {device.DeviceFriendlyName}, {device.FriendlyName}");
                return i;
            }
        }
        return -1; // VB-CABLE device not found
    }

}
