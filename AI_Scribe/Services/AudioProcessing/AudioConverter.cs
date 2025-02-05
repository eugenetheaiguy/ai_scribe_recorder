using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace AI_Scribe
{
    class AudioConverter
    {
        public static void ConvertAACToWAV(string inputFile, string outputFile)
        {
            // Ensure the input file exists
            if (!File.Exists(inputFile))
            {
                throw new FileNotFoundException("Input file not found", inputFile);
            }

            try
            {
                // Initialize the Media Foundation reader for AAC
                using (var reader = new MediaFoundationReader(inputFile))
                {
                    // Create a WAV file writer with the same format as the reader
                    using (var waveWriter = new WaveFileWriter(outputFile, reader.WaveFormat))
                    {
                        // Copy data from the AAC file to the WAV file
                        reader.CopyTo(waveWriter);
                    }
                }
                Console.WriteLine($"Conversion completed: {outputFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during conversion: {ex.Message}");
            }
        }
    }
}


