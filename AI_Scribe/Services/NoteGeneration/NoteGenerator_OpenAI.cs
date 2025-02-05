using Newtonsoft.Json.Linq;
using AI_Scribe.Interfaces;
using RestSharp;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;

namespace AI_Scribe.Services
{
    /// <summary>
    /// Implementation of the INoteGenerator interface using OpenAI for generating structured notes.
    /// </summary>
    internal class NoteGenerator_OpenAI
    {

        /// <summary>
        /// Generates structured notes from the transcription text using OpenAI's language model.
        /// </summary>
        /// <param name="transcriptFile">Cleaned transcription text.</param>
        /// <returns>Generated notes as a string.</returns>
        static public async Task<string> GenerateNotes(string transcriptFile)
        {
            if (string.IsNullOrWhiteSpace(transcriptFile))
            {
                throw new ArgumentException("Transcription text cannot be null or empty.", nameof(transcriptFile));
            }

            string outputNotePath = "";

            if (string.IsNullOrWhiteSpace(transcriptFile))
            {
                throw new ArgumentException("Transcript file path cannot be null or empty.", nameof(transcriptFile));
            }
            if (!File.Exists(transcriptFile))
            {
                throw new FileNotFoundException("The specified transcript file does not exist.", transcriptFile);
            }
            try
            {
                // Read the transcript content asynchronously
                string transcriptContent = await File.ReadAllTextAsync(transcriptFile);

                if (string.IsNullOrWhiteSpace(transcriptContent))
                {
                    Console.WriteLine("The transcript file is empty. No note will be created.");
                    return outputNotePath;
                }

                // GPT-4 API endpoint
                var client = new RestClient("https://api.openai.com/v1/chat/completions");
                var request = new RestRequest();
                request.Method = Method.Post;
                request.AddHeader("Authorization", "Bearer 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");
                // Prepare the input for GPT-4
                var body = new
                {
                    model = "gpt-4o",
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = "You are a medical assistant specializing in soap note creation."
                        },
                        new
                        {
                            role = "user",
                            content = transcriptContent
                        }
                        },
                    temperature = 0.7 // Adjust temperature for creativity
                };


               
                // Add body to the request
                request.AddJsonBody(body);

                // Send the request and get the response
                RestResponse response = await client.ExecuteAsync(request);
                if (response.IsSuccessful)
                {
                    var note = response.Content; // Adjust according to the API response structure
                    var jsonObject = JObject.Parse(note);
                    string extractedText = jsonObject["choices"]?[0]?["message"]?["content"]?.ToString();
                    // Write the transcribed text to a file
                    // Create a "generated" folder within the same directory as audioFilePath
                    string directoryPath = Path.GetDirectoryName(transcriptFile);

                    // Define the transcript file path in the "generated" folder
                    outputNotePath = Path.Combine(transcriptFile.Replace("transcript", "note"));

                    // Write the transcribed text to the file
                    System.IO.File.WriteAllText(outputNotePath, extractedText);
                    Console.WriteLine($"Note File saved to: {outputNotePath}");

                }
                else
                {
                    throw new Exception(response.Content);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while creating the note: {ex.Message}");
            }
            return outputNotePath;
        }
    }
}