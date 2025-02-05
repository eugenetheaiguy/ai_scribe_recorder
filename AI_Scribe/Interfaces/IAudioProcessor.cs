namespace AI_Scribe.Interfaces
{
    public interface IAudioProcessor
    {
        /// <summary>
        /// Prepares the audio file for transcription.
        /// </summary>
        /// <param name="audioFilePath">Path to the input audio file.</param>
        /// <returns>Path to the processed audio file.</returns>
        string PrepareAudio(string audioFilePath);
    }
}
