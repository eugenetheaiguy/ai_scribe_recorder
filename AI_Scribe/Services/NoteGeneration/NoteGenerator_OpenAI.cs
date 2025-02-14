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
    internal class NoteGenerator_OpenAI_Original
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
                            content = "Generate a detailed, structured, and clinically accurate SOAP note based on the following patient encounter transcript. The output must strictly follow the SOAP format below, ensuring clarity, consistency, and adherence to medical documentation standards.\r\n\r\nDo not include any extra text, explanations, or commentary.\r\nRespond only with the SOAP note in the structured format provided below.\r\nEnsure concise, medically relevant phrasing with a professional clinical tone.\r\nUse bullet points where appropriate to enhance clarity.\r\nMaintain logical flow between sections, ensuring a complete and coherent patient narrative.\r\nFollow this exact SOAP format:\r\n\r\nSOAP Note Template\r\nSubjective:\r\nChief Complaint (CC): [Brief statement summarizing the patient’s primary concern.]\r\nHistory of Present Illness (HPI):\r\nOnset: [When symptoms started]\r\nDuration: [How long symptoms have persisted]\r\nSeverity: [Mild, moderate, severe, or scale (e.g., 7/10)]\r\nQuality: [Sharp, dull, throbbing, etc.]\r\nLocation/Radiation: [Where the symptom is felt and if it spreads]\r\nAggravating/Relieving Factors: [What makes it worse or better]\r\nAssociated Symptoms: [Any other symptoms present]\r\nPast Medical History (PMH): [Relevant diagnoses, conditions]\r\nPast Psychiatric History (if applicable): [History of mental health conditions, hospitalizations]\r\nMedications: [List of current medications with dosages]\r\nAllergies: [Drug, food, or environmental allergies with reactions]\r\nFamily History (FH): [Relevant conditions in first-degree relatives]\r\nSocial History (SH): [Lifestyle factors, substance use, living situation, employment]\r\nReview of Systems (ROS): [Pertinent positives and negatives]\r\nObjective:\r\nVital Signs: [BP, HR, RR, Temp, SpO₂, Weight, BMI]\r\nGeneral Appearance: [Patient’s overall presentation]\r\nPhysical Exam Findings: (Only relevant systems based on complaint)\r\nHEENT: [Findings related to head, eyes, ears, nose, throat]\r\nCardiovascular: [Heart sounds, murmurs, peripheral pulses]\r\nRespiratory: [Breath sounds, effort]\r\nAbdomen: [Tenderness, masses, bowel sounds]\r\nNeurologic: [Reflexes, strength, sensation]\r\nPsychiatric (if applicable): [Mood, affect, thought process, insight, judgment]\r\nDiagnostic Tests: [Relevant lab results, imaging, EKG findings]\r\nAssessment:\r\nPrimary Diagnosis: [Most likely diagnosis]\r\nDifferential Diagnoses: [List of alternative possibilities]\r\nClinical Rationale: [Brief explanation supporting the diagnosis]\r\nPlan:\r\nMedications: [Any new prescriptions, adjustments, discontinuations]\r\nDiagnostics: [Labs, imaging, or further testing ordered]\r\nReferrals: [Specialists or services patient is referred to]\r\nProcedures: [Any in-office procedures performed or planned]\r\nPatient Education & Counseling: [Discussion points about condition, medications, lifestyle changes]\r\nFollow-Up: [Next appointment, monitoring plan]\r\nEnsure the SOAP note is strictly formatted as shown above. No additional text or explanations should be included in the response—only the SOAP note in its structured format. Here is the transcript:" +
                            transcriptContent
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