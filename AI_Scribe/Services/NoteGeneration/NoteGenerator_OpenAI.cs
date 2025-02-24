

using Newtonsoft.Json.Linq;
using AI_Scribe.Interfaces;
using RestSharp;
using System;
using System.IO;
using System.Threading.Tasks;
using AI_Scribe.Types;
using System.Text.Json;
using DotNetEnv;

namespace AI_Scribe.Services
{
    /// <summary>
    /// Implementation of the INoteGenerator interface using OpenAI for generating structured SOAP notes.
    /// </summary>
    internal class NoteGenerator_OpenAI
    {
        private const string OpenAiEndpoint = "https://api.openai.com/v1/chat/completions";
        private const string ConfigFilePath = "configPrompt.json";
        /// <summary>
        /// Generates structured notes from the transcription text using OpenAI's language model.
        /// </summary>
        /// <param name="transcriptFile">Path to the cleaned transcription text file.</param>
        /// <returns>Generated notes file path as a string.</returns>
        public static async Task<string> GenerateNotes(string transcriptFile)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(transcriptFile))
                throw new ArgumentException("Transcript file path cannot be null or empty.", nameof(transcriptFile));

            if (!File.Exists(transcriptFile))
                throw new FileNotFoundException("The specified transcript file does not exist.", transcriptFile);

            string transcriptContent = await File.ReadAllTextAsync(transcriptFile);
            if (string.IsNullOrWhiteSpace(transcriptContent))
            {
                Console.WriteLine("The transcript file is empty. No note will be created.");
                return string.Empty;
            }

            try
            {

                transcriptContent = await TranslateTranscript(transcriptContent);
                var precursorMessages =await LoadMessagesFromJson();
                precursorMessages.Add(new Message("user", transcriptContent));
                string noteContent = await SendOpenAiRequest( precursorMessages);

                // Define the output note path
                string outputNotePath = Path.Combine(
                    Path.GetDirectoryName(transcriptFile),
                    Path.GetFileNameWithoutExtension(transcriptFile).Replace("transcript", "note") + ".txt"
                );
                // Save generated SOAP note to file
                await File.WriteAllTextAsync(outputNotePath, noteContent);
                Console.WriteLine($"Note File saved to: {outputNotePath}");

                return outputNotePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while creating the note: {ex.Message}");
                return string.Empty;
            }
        }
        /// <summary>
        /// Loads system and user messages from the JSON configuration file.
        /// </summary>
        private static async Task<List<Message>> LoadMessagesFromJson()
        {
            if (!File.Exists(ConfigFilePath))
                throw new FileNotFoundException("Configuration file not found.", ConfigFilePath);

            string jsonContent = await File.ReadAllTextAsync(ConfigFilePath);
            var configData = JsonSerializer.Deserialize<ConfigData>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return configData?.PrecursorMessages ?? new List<Message>();
        }

        /// <summary>
/// Represents the JSON structure containing precursor messages.
/// </summary>
public class ConfigData
{
    public List<Message> PrecursorMessages { get; set; }
}

        /// <summary>
        /// Generates structured notes from the transcription text using OpenAI's language model.
        /// </summary>
        /// <param name="transcriptFile">Path to the cleaned transcription text file.</param>
        /// <returns>Generated notes file path as a string.</returns>
        /// <summary>
        /// Extracts the patient's name from the transcription text using OpenAI's language model.
        /// </summary>
        /// <param name="transcriptFile">Path to the cleaned transcription text file.</param>
        /// <returns>Extracted patient name in the format FirstName-LastName or Name.</returns>
        public static async Task<string> ExtractName(string transcriptFile)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(transcriptFile))
                throw new ArgumentException("Transcript file path cannot be null or empty.", nameof(transcriptFile));

            if (!File.Exists(transcriptFile))
                throw new FileNotFoundException("The specified transcript file does not exist.", transcriptFile);

            string transcriptContent = await File.ReadAllTextAsync(transcriptFile);
            if (string.IsNullOrWhiteSpace(transcriptContent))
            {
                Console.WriteLine("The transcript file is empty. No name found.");
                return string.Empty;
            }

            var precursorMessages = new List<Message>
            {
                  new Message( "system",  "You are a medical assistant specialized in extracting patient names from transcripts." ),
                  new Message( "user",  "Extract the patient's full name from the following transcript. Please return only the name and no other text. If only a first or last name exists, return just that. Format the name as FirstName-LastName or just Name if only one part is available. If no name is available just return 'Unknown Patient'. Here is the patient's name: \n\n" +  transcriptContent),
            };

            string name = await SendOpenAiRequest(precursorMessages);
            string nameFile  = transcriptFile.Replace("transcript", "name");

            try
            {
                // Write the name string into the new file
                await File.WriteAllTextAsync(nameFile, name);

                Console.WriteLine($"Name written to file: {nameFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return nameFile;

        }

        /// <summary>
        /// Translates the transcript to English if it's in a different language.
        /// </summary>
        /// <param name="transcriptFile">Path to the transcript file to be translated.</param>
        /// <returns>Path to the translated transcript file, or original file path if translation wasn't needed.</returns>
        public static async Task<string> TranslateTranscript(string transcriptContent)
        {
            // Validate input

            try
            {
                // First, detect the language
                var detectMessages = new List<Message>
                {
                    new Message("system", "You are a language detection specialist. Analyze the following text and respond with ONLY 'ENGLISH' if the text is primarily in English, or the ISO 639-1 language code (e.g., 'ES' for Spanish) if it's primarily in another language. Respond with only the language code and no other text."),
                    new Message("user", transcriptContent)
                };

                string detectedLanguage = await SendOpenAiRequest(detectMessages);

                // If the text is already in English, return the original file path
                if (detectedLanguage.Trim().ToUpper() == "ENGLISH")
                {
                    Console.WriteLine("Text is already in English. No translation needed.");
                    return transcriptContent;
                }

                // If not in English, proceed with translation
                var translateMessages = new List<Message>
                {
                    new Message("system", "You are a medical translator. Translate the following text to English, maintaining all medical terminology and formatting. Provide only the translated text without any additional comments or explanations."),
                    new Message("user", transcriptContent)
                };

                string translatedContent = await SendOpenAiRequest(translateMessages);

                return translatedContent;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during translation: {ex.Message}");
                return transcriptContent; // Return original file path in case of error
            }
        }

        /// <summary>
        /// Sends the patient transcript to OpenAI and retrieves a structured SOAP note.
        /// </summary>
        /// <param name="transcriptContent">Patient encounter transcript.</param>
        /// <param name="messages">List of previous messages for context.</param>
        /// <returns>Response content from OpenAI API.</returns>
        private static async Task<string> SendOpenAiRequest(List<Message> messages)
        {
            Env.Load();
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

            var client = new RestClient(OpenAiEndpoint);
            var request = new RestRequest();
            request.Method = Method.Post;
            request.AddHeader("Authorization", apiKey);
            request.AddHeader("Content-Type", "application/json");

            var requestBody = new
            {
                model = "gpt-4o",
                messages = messages,
                temperature = 0.7
            };

            request.AddJsonBody(requestBody);

            RestResponse response = await client.ExecuteAsync(request);

            if (!response.IsSuccessful)
            {
                Console.WriteLine($"OpenAI API request failed: {response.StatusCode} - {response.Content}");
                return string.Empty;
            }

            string responseContent = response.Content;
            if (string.IsNullOrEmpty(responseContent))
            {
                Console.WriteLine("Failed to retrieve a valid SOAP note from OpenAI.");
                return string.Empty;
            }

            // Extract SOAP note from JSON response
            var jsonObject = JObject.Parse(responseContent);
            string extractedText = jsonObject["choices"]?[0]?["message"]?["content"]?.ToString();

            return extractedText;
        }

    }
}
