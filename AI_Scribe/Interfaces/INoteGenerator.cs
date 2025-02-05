namespace AI_Scribe.Interfaces
{
    public interface INoteGenerator
    {
        /// <summary>
        /// Generates structured notes from the transcription text.
        /// </summary>
        /// <param name="transcription">Cleaned transcription text.</param>
        /// <returns>Generated notes as a string.</returns>
        Task<string> GenerateNotes(string transcription);
    }
}
