using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Threading.Tasks;
using AI_Scribe.Services;
using System.ComponentModel;
using System.Reflection.Metadata;
using IniParser;
using NAudio.Wave;
using NAudio.Lame;
using System.Globalization;


namespace AI_Scribe
{
    public class ScribeRecording : INotifyPropertyChanged
    {
        private string audioFilePath;
        private string noteFile;
        private string transcriptFile;
        private string nameFile;

        private bool isSelectedForDeletion;
        private string recordedAtDisplay;
        private string durationDisplay;

        public string DisplayName
        {
            get
            {
                return File.ReadAllText($"{nameFile}");
            }
            set
            {
                OnPropertyChanged(nameof(DisplayName));
                File.WriteAllText($"{nameFile}", value);
            }
        }

        public string RecordedAtDisplay
        {
            get
            {
                // Split the filename by underscore
                string[] parts = audioFilePath.Split('_', '.');

                if (parts.Length < 3)
                {
                    return "Invalid filename format";
                }

                // Extract date (YYYYMMDD) and time (HHmmss)
                string date = parts[1];
                string time = parts[2];

                // Validate the length of date and time
                if (date.Length == 8 && time.Length >= 6)
                {
                    // Parse date and time
                    DateTime parsedDateTime = DateTime.ParseExact(
                        date + time.Substring(0, 6),
                        "yyyyMMddHHmmss",
                        CultureInfo.InvariantCulture
                    );

                    // Convert to desired format (e.g., "January 2, 2025 at 07:19:43 PM")
                    return parsedDateTime.ToString("MMMM d, yyyy 'at' hh:mm:ss tt", CultureInfo.InvariantCulture);
                }

                return "Invalid date/time format";
            }
        }


        public ScribeRecording(string audioFile, string transcriptFile, string noteFile, string metaData)
        {

            // Move/Copy files to the new structure while keeping original filenames
            this.audioFilePath = audioFile;
            this.transcriptFile = transcriptFile;
            this.noteFile = noteFile;
            this.nameFile = metaData;// We assume only metadata is displayhName
        }

        private string MoveFileToFolder(string sourceFile, string targetFolder)
        {
            // Keep the original filename
            string fileName = Path.GetFileName(sourceFile);
            string targetPath = Path.Combine(targetFolder, fileName);

            // If source file exists, move/copy it to the new location
            if (File.Exists(sourceFile))
            {
                // If files are different, copy/move the file
                if (sourceFile != targetPath)
                {
                    // Copy the file (use Move if you want to move instead of copy)
                    File.Copy(sourceFile, targetPath, true);

                    // Optionally delete the original file if you want to move instead of copy
                    // File.Delete(sourceFile);
                }
            }

            return targetPath;
        }

        public static async Task<ScribeRecording> CreateScribeAsync(string audioFilePath)
        {
            string mp3AudioFilePath = audioFilePath.Replace("wav", "mp3");
            ConvertWavToMp3(audioFilePath, audioFilePath.Replace("wav", "mp3"), 128);
            var outputTranscriptFile = await TranscriptionService_Whisper.TranscribeAudio(audioFilePath);
            var outputNoteFile = await NoteGenerator_OpenAI.GenerateNotes(outputTranscriptFile);
            var name = await NoteGenerator_OpenAI.ExtractName(outputTranscriptFile);
            return new ScribeRecording(mp3AudioFilePath, outputTranscriptFile, outputNoteFile, name);
        }

        public static void ConvertWavToMp3(string wavPath, string mp3Path, int bitRate = 128)
        {
            using (var reader = new WaveFileReader(wavPath))
            {
                using (var writer = new LameMP3FileWriter(mp3Path, reader.WaveFormat, bitRate))
                {         {
            reader.CopyTo(writer);

        }}
            }

        }


        public async Task<ScribeRecording> Regenerate()
        {

            await TranscriptionService_Whisper.TranscribeAudio(AudioFilePath);

            await NoteGenerator_OpenAI.GenerateNotes(transcriptFile);

            OnPropertyChanged(nameof(Note));
            OnPropertyChanged(nameof(Transcript));
            return this;
        }

        public bool IsSelectedForDeletion
        {
            get => isSelectedForDeletion;
            set
            {
                if (isSelectedForDeletion != value)
                {
                    isSelectedForDeletion = value;
                    OnPropertyChanged(nameof(IsSelectedForDeletion));
                }
            }
        }

        public string AudioFilePath
        {
            get
            {
                return audioFilePath;
            }
            private set 
            { 
                audioFilePath = value;
 
            }
        }
        public string Note
        {
            get
            {
                return File.ReadAllText($"{noteFile}");
            }
            set
            {
                OnPropertyChanged(nameof(Note));
                File.WriteAllText($"{noteFile}", value);
            }
        }
        public string Transcript
        {
            get
            {
                return File.ReadAllText($"{transcriptFile}");
            }
            set
            {
                OnPropertyChanged(nameof(Transcript));
                File.WriteAllText($"{transcriptFile}", value);
            }
        }

        public void DeleteRecording()
        {
            try
            {

                // Get the directory containing these files
                string recordingFolder = Path.GetDirectoryName(audioFilePath);

                // If the directory exists and is empty, delete it
                if (Directory.Exists(recordingFolder))
                {
                    Directory.Delete(recordingFolder, recursive: true);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting recording files: {ex.Message}", ex);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

public class WavSplitter
    {
        public List<string> SplitWavFile(string inputPath, string tempFolderPath)
        {
            List<string> outputFiles = new List<string>();
            Directory.CreateDirectory(tempFolderPath); // Ensure the temp folder exists

            byte[] header = new byte[44];
            using (FileStream fs = new FileStream(inputPath, FileMode.Open))
            {
                // Read WAV header
                if (fs.Read(header, 0, 44) != 44)
                    throw new InvalidDataException("Invalid WAV file header");

                // Parse header information
                int dataSize = BitConverter.ToInt32(header, 40);
                short blockAlign = BitConverter.ToInt16(header, 32);
                long twentyFiveMB = 25 * 1024 * 1024;
                long originalFileSize = 44 + dataSize;

                // Handle files smaller than 25MB
                if (originalFileSize <= twentyFiveMB)
                {
                    string tempFilePath = Path.Combine(tempFolderPath, Path.GetFileName(inputPath));
                    File.Copy(inputPath, tempFilePath, true);
                    outputFiles.Add(tempFilePath);
                    return outputFiles;
                }

                // Calculate maximum data per chunk
                long maxDataPerChunk = (twentyFiveMB - 44) / blockAlign * blockAlign;
                if (maxDataPerChunk <= 0)
                    throw new InvalidOperationException("Block align is too large for 25MB chunks");

                // Read audio data
                byte[] data = new byte[dataSize];
                fs.Seek(44, SeekOrigin.Begin);
                fs.Read(data, 0, dataSize);

                // Split into chunks
                int numberOfChunks = (int)((dataSize + maxDataPerChunk - 1) / maxDataPerChunk);
                for (int i = 0; i < numberOfChunks; i++)
                {
                    int start = i * (int)maxDataPerChunk;
                    int chunkDataSize = (int)Math.Min(maxDataPerChunk, dataSize - start);

                    // Create new header
                    byte[] newHeader = (byte[])header.Clone();
                    BitConverter.GetBytes(36 + chunkDataSize).CopyTo(newHeader, 4); // RIFF size
                    BitConverter.GetBytes(chunkDataSize).CopyTo(newHeader, 40);     // Data chunk size

                    // Write chunk to temporary folder
                    string chunkPath = Path.Combine(tempFolderPath, $"chunk_{i}.wav");
                    using (FileStream outFs = new FileStream(chunkPath, FileMode.Create))
                    {
                        outFs.Write(newHeader, 0, 44);
                        outFs.Write(data, start, chunkDataSize);
                    }
                    outputFiles.Add(chunkPath);
                }
            }
            return outputFiles;
        }
    }
}