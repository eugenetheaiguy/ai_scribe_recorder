
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using NAudio.Wave;
using System.Collections.ObjectModel;
using IniParser;
using IniParser.Model;
using Path = System.IO.Path;  // Add this at the top of your file
using System.Windows.Input;
namespace AI_Scribe
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private bool isRecording = false;
        private WaveInEvent waveIn;
        private WaveFileWriter waveWriter;
        private string newScribeOutputPath;

        // Add this property
        private ObservableCollection<ScribeRecording> _recordings;
        public ObservableCollection<ScribeRecording> Recordings
        {
            get => _recordings;
            set
            {
                _recordings = value;
                OnPropertyChanged(nameof(Recordings));
            }
        }

        private ScribeRecording _selectedRecording;
        public ScribeRecording SelectedRecording
        {
            get => _selectedRecording;
            set
            {
                _selectedRecording = value;
                OnPropertyChanged(nameof(SelectedRecording));

            }
        }


        // Implement INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public MainWindow()
        {
            Recordings = new ObservableCollection<ScribeRecording>();
            InitializeComponent();

            DataContext = this;

            LoadExistingRecordings();
        }

        public void ShowProcessingOverlay()
        {
            ProcessingOverlay.Visibility = Visibility.Visible;
        }

        public void HideProcessingOverlay()
        {
            ProcessingOverlay.Visibility = Visibility.Collapsed;
        }
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void StartRecording()
        {
            try
            {
                // Create output directory if it doesn't exist
                var parser = new FileIniDataParser();
                var iniData = parser.ReadFile("config.ini");

                //Create Directory for Recordings if it does not exist
                Directory.CreateDirectory(iniData["Storage"]["RecordingsPath"]);

                //Create Directory for newest Recording
                var newRecordingFolderGuid = Guid.NewGuid().ToString();
                Directory.CreateDirectory(iniData["Storage"]["RecordingsPath"] + "\\" + newRecordingFolderGuid);

                // Generate unique filename using timestamp
                newScribeOutputPath = Path.Combine(iniData["Storage"]["RecordingsPath"] + "\\" + newRecordingFolderGuid,
                    $"Recording_{DateTime.Now:yyyyMMdd_HHmmss}.wav");

                // Initialize audio capture
                waveIn = new WaveInEvent
                {
                    WaveFormat = new WaveFormat(44100, 1), // 44.1kHz, mono
                    BufferMilliseconds = 50
                };

                // Initialize file writer
                waveWriter = new WaveFileWriter(newScribeOutputPath, waveIn.WaveFormat);

                // Wire up the data available event
                waveIn.DataAvailable += WaveIn_DataAvailable;
                waveIn.RecordingStopped += WaveIn_RecordingStopped;

                // Start recording
                waveIn.StartRecording();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                  $"Error starting recording: {ex.Message}",
                  "Recording Error",
                  MessageBoxButton.OK,
                  MessageBoxImage.Error);

                // Clean up any resources that might have been created
                if (waveWriter != null)
                {
                    waveWriter.Dispose();
                    waveWriter = null;
                }
                if (waveIn != null)
                {
                    waveIn.Dispose();
                    waveIn = null;
                }
            }

        }
        private void DeletedButton_Click(object sender, RoutedEventArgs e)
        {
            var recordingsToDelete = Recordings.Where(r => r.IsSelectedForDeletion).ToList();
            foreach (var recording in recordingsToDelete)
            {
                recording.DeleteRecording();
                Recordings.Remove(recording);
            }

            IsAllSelected = false;

            Recordings = new ObservableCollection<ScribeRecording>(Recordings.ToList());
            OnPropertyChanged(nameof(Recordings));
        }


        private bool _isAllSelected;
        public bool IsAllSelected
        {
            get => _isAllSelected;
            set
            {
                if (_isAllSelected != value)
                {
                    _isAllSelected = value;
                    OnPropertyChanged(nameof(IsAllSelected));

                    // If we have recordings, update their selection state
                    if (Recordings != null)
                    {
                        bool allCurrentlySelected = Recordings.All(r => r.IsSelectedForDeletion);

                        // Only update if there's a change needed
                        if (allCurrentlySelected != value)
                        {
                            foreach (var recording in Recordings)
                            {
                                recording.IsSelectedForDeletion = value;
                            }
                        }
                    }
                }
            }
        }

        private async void LoadExistingRecordings()
        {
            try
            {
                var parser = new FileIniDataParser();
                var iniData = parser.ReadFile("config.ini");
                string recordingsPath = iniData["Storage"]["RecordingsPath"];

                if (!Directory.Exists(recordingsPath))
                {
                    return;
                }

                // Get all GUID folders
                var guidFolders = Directory.GetDirectories(recordingsPath);

                foreach (var folder in guidFolders)
                {
                    try
                    {
                        // Get all .wav files in this folder
                        var wavFiles = Directory.GetFiles(folder, "*.mp3");

                        foreach (var wavFile in wavFiles)
                        {
                            // Get the base filename without extension
                            string baseFileName = Path.GetFileNameWithoutExtension(wavFile);
                            string folderPath = Path.GetDirectoryName(wavFile);

                            // Construct expected transcript and note file paths
                            string transcriptPath = Path.Combine(folderPath, $"{baseFileName}.transcript.txt");
                            string notePath = Path.Combine(folderPath, $"{baseFileName}.note.txt");

                            // Verify all required files exist
                            if (File.Exists(wavFile) && File.Exists(transcriptPath) && File.Exists(notePath))
                            {
                                // Create ScribeRecording object
                                var recording = new ScribeRecording(wavFile, transcriptPath, notePath);

                                // Add to collection on UI thread
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    Recordings.Add(recording);
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue processing other folders
                        MessageBox.Show(
                            $"Error processing folder {folder}: {ex.Message}",
                            "Loading Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading existing recordings: {ex.Message}",
                    "Loading Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void StopRecording()
        {
            waveIn?.StopRecording();
        }

        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {

            if (waveWriter != null)
            {
                waveWriter.Write(e.Buffer, 0, e.BytesRecorded);
                waveWriter.Flush();
            }
        }

        private async void WaveIn_RecordingStopped(object sender, StoppedEventArgs e)
        {
            try
            {
                if (waveWriter != null)
                {
                    waveWriter.Dispose();
                    waveWriter = null;
                }

                if (waveIn != null)
                {
                    waveIn.Dispose();
                    waveIn = null;
                }

                // Create output directory if it doesn't exist
                var parser = new FileIniDataParser();
                var iniData = parser.ReadFile("config.ini");


                ShowProcessingOverlay();
                // Create new ScribeRecording object
                var recording = await ScribeRecording.CreateScribeAsync(newScribeOutputPath);
                HideProcessingOverlay();



                // Add to collection on UI thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Recordings.Add(recording);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                   $"Error starting recording: {ex.Message}",
                   "Recording Error",
                   MessageBoxButton.OK,
                   MessageBoxImage.Error);

                // Clean up any resources that might have been created
                if (waveWriter != null)
                {
                    waveWriter.Dispose();
                    waveWriter = null;
                }
                if (waveIn != null)
                {
                    waveIn.Dispose();
                    waveIn = null;
                }
            }
        }

        private TimeSpan GetAudioDuration(string filePath)
        {
            using (var audioFile = new AudioFileReader(filePath))
            {
                return audioFile.TotalTime;
            }
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TabItem selectedTab = (TabItem)DocumentTabControl.SelectedItem;
                if (selectedTab!= null)
                {
                    TextBox textboxwithtext = (TextBox)selectedTab.Content;
                    
                    Clipboard.SetText(textboxwithtext.Text);
                    // Optional: Show success message or status
                }     

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy to clipboard: {ex.Message}", "Error");
            }
        }

        private async void RegenerateButton_Click(object sender, RoutedEventArgs e)
        {
            ShowProcessingOverlay();
            var recording = await SelectedRecording.Regenerate();
            HideProcessingOverlay();
        }
        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            isRecording = !isRecording;

            if (isRecording)
            {
                StartRecording();
                RecordButton.Style = (Style)FindResource("RecordingButtonActive");
            }
            else
            {
                StopRecording();
                RecordButton.Style = (Style)FindResource("RecordingButton");        
            }
        }

        // Clean up when window closes
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            StopRecording();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }

}
