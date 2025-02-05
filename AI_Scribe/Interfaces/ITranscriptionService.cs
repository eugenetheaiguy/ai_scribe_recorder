namespace AI_Scribe.Interfaces
{
    public interface ITranscriptionService
    {
        /// <summary>
        /// Transcribes the provided audio file to text.
        /// </summary>
        /// <param name="audioFilePath">Path to the audio file.</param>
        /// <returns>Transcribed text as a string.</returns>
        Task<string> TranscribeAudio(string audioFilePath);
    }
}
