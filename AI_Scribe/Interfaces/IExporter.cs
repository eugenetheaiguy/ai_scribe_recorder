namespace AI_Scribe.Interfaces
{
    public interface IExporter
    {
        /// <summary>
        /// Exports the generated notes to a specified format.
        /// </summary>
        /// <param name="notes">The notes to export.</param>
        /// <param name="outputFormat">The desired file format (e.g., txt, pdf).</param>
        void ExportNotes(string notes, string outputFormat);
    }
}
