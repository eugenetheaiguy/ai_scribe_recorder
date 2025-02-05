using AI_Scribe.Interfaces;
using System.IO;
namespace AI_Scribe.Services
{
    /// <summary>
    /// Implementation of the IAudioProcessor interface for processing audio files.
    /// </summary>
    public class AudioProcessor : IAudioProcessor
    {
        /// <summary>
        /// Prepares the audio file for transcription by validating and processing it.
        /// </summary>
        /// <param name="audioFilePath">Path to the input audio file.</param>
        /// <returns>Path to the processed audio file, or the original path if no processing was required.</returns>
        /// <exception cref="ArgumentException">Thrown if the file path is invalid.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
        public string PrepareAudio(string audioFilePath)
        {
            ValidateAudioPath(audioFilePath);

            string processedFilePath = audioFilePath;

            if (Path.GetExtension(audioFilePath).Equals(".aac", StringComparison.OrdinalIgnoreCase))
            {
                processedFilePath = Path.ChangeExtension(audioFilePath, ".wav");

                try
                {
                    AudioConverter.ConvertAACToWAV(audioFilePath, processedFilePath);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Error occurred during audio conversion.", ex);
                }
            }

            // Replace Console.WriteLine with a logging framework in production
            Console.WriteLine($"Processed audio file: {processedFilePath}");
            return processedFilePath;
        }

        private void ValidateAudioPath(string audioFilePath)
        {
            if (string.IsNullOrWhiteSpace(audioFilePath))
            {
                throw new ArgumentException("Audio file path cannot be null or empty.", nameof(audioFilePath));
            }

            if (!File.Exists(audioFilePath))
            {
                throw new FileNotFoundException($"The file '{audioFilePath}' does not exist.");
            }
        }
    }
}
