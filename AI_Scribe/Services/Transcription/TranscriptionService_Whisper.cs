using Newtonsoft.Json.Linq;
using AI_Scribe.Interfaces;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI_Scribe.Services
{
    /// <summary>
    /// A mock implementation of the ITranscriptionService interface.
    /// Transcribes the provided audio file to text.
    /// </summary>
    public class TranscriptionService_Whisper
    {
        /// <summary>
        /// Transcribes the provided audio file to text (mock implementation).
        /// </summary>
        /// <param name="audioFilePath">Path to the audio file.</param>
        /// <returns>Transcribed text as a string.</returns>
        static public async Task<string> TranscribeAudio(string audioFilePath)
        {   
            string transcriptFile = "";
            string wavFilePath = "";
            try
            {

                var client = new RestClient("https://api.openai.com/v1/audio");
                var request = new RestRequest("transcriptions", Method.Post);
                request.AddHeader("Authorization", "PUTTOKENHERE");
                request.AddFile("file", audioFilePath);
                request.AddParameter("model", "whisper-1");

                // Escape special characters for SendKeys
                RestResponse response = await client.ExecuteAsync(request);

                if (response.IsSuccessful)
                {
                    var transcription = response.Content; // Adjust according to the API response structure
                    var jsonObject = JObject.Parse(transcription);
                    string extractedText = jsonObject["text"]?.ToString();
                    // Write the transcribed text to a file
                    // Create a "generated" folder within the same directory as audioFilePath
                    string directoryPath = Path.GetDirectoryName(audioFilePath);

                    // Define the transcript file path in the "generated" folder
                    transcriptFile = Path.Combine(directoryPath, Path.GetFileNameWithoutExtension(audioFilePath) + ".transcript.txt");

                    // Write the transcribed text to the file
                    System.IO.File.WriteAllText(transcriptFile, extractedText);
                    Console.WriteLine($"Transcription saved to: {transcriptFile}");
    


                }
                else
                {
                    throw new Exception(response.Content);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return transcriptFile;
        }



    }
}
