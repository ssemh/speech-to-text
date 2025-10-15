using System;
using System.Drawing;
using System.Linq;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpeechToTextApp
{
    public partial class MainForm : Form
    {
        private SpeechRecognitionEngine speechRecognitionEngine;
        private SpeechSynthesizer speechSynthesizer;
        private bool isListening = false;

        private TextBox textOutput;
        private Button startButton;
        private Button stopButton;
        private Label statusLabel;
        private Label titleLabel;

        public MainForm()
        {
            InitializeComponent();
            InitializeSpeechRecognition();
            InitializeSpeechSynthesis();
        }

        private void InitializeComponent()
        {
            this.Text = "Speech to Text App";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 245, 245);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            titleLabel = new Label
            {
                Text = "🎤 Speech to Text Converter",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(50, 20),
                Size = new Size(700, 50)
            };
            this.Controls.Add(titleLabel);

            textOutput = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 14),
                Location = new Point(50, 90),
                Size = new Size(700, 350),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = "Metin burada görünecek...\n\nKonuşmaya başlamak için 'Başla' butonuna tıklayın.",
                ReadOnly = true
            };
            this.Controls.Add(textOutput);

            startButton = new Button
            {
                Text = "🎤 Konuşmaya Başla",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(52, 152, 219),
                Location = new Point(200, 470),
                Size = new Size(180, 50),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            startButton.FlatAppearance.BorderSize = 0;
            startButton.Click += StartButton_Click;
            this.Controls.Add(startButton);

            stopButton = new Button
            {
                Text = "⏹️ Durdur",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(231, 76, 60),
                Location = new Point(420, 470),
                Size = new Size(180, 50),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            stopButton.FlatAppearance.BorderSize = 0;
            stopButton.Click += StopButton_Click;
            this.Controls.Add(stopButton);

            statusLabel = new Label
            {
                Text = "Hazır",
                Font = new Font("Segoe UI", 12),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(50, 540),
                Size = new Size(700, 30),
                BackColor = Color.FromArgb(52, 73, 94),
                ForeColor = Color.White
            };
            this.Controls.Add(statusLabel);

            this.FormClosing += MainForm_FormClosing;
        }

        private void InitializeSpeechRecognition()
        {
            try
            {
                var recognizers = SpeechRecognitionEngine.InstalledRecognizers();
                
                if (recognizers.Count == 0)
                {
                    MessageBox.Show("Hiç konuşma tanıyıcı yüklü değil!\n\n" +
                        "Çözüm:\n" +
                        "1. Windows Ayarlar > Zaman ve Dil > Dil\n" +
                        "2. İngilizce dilini ekleyin (Türkçe desteklenmiyor)\n" +
                        "3. Konuşma paketini indirin\n" +
                        "4. Uygulamayı yeniden başlatın\n\n" +
                        "Not: Windows'ta Türkçe konuşma tanıma sınırlıdır.", 
                        "Konuşma Tanıyıcı Bulunamadı", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    UpdateStatus("Konuşma tanıyıcı yüklü değil");
                    return;
                }

                string availableLanguages = "Mevcut diller:\n";
                foreach (var recognizer in recognizers)
                {
                    availableLanguages += $"- {recognizer.Culture.DisplayName} ({recognizer.Culture.Name})\n";
                }

                var selectedRecognizer = recognizers.FirstOrDefault(r => r.Culture.Name.StartsWith("en")) ?? recognizers[0];
                
                speechRecognitionEngine = new SpeechRecognitionEngine(selectedRecognizer);
                
                var grammarBuilder = new GrammarBuilder();
                grammarBuilder.AppendDictation();
                
                var grammar = new Grammar(grammarBuilder);
                speechRecognitionEngine.LoadGrammar(grammar);

                speechRecognitionEngine.SpeechRecognized += SpeechRecognized;
                speechRecognitionEngine.SpeechDetected += SpeechDetected;
                speechRecognitionEngine.SpeechHypothesized += SpeechHypothesized;
                speechRecognitionEngine.SpeechRecognitionRejected += SpeechRecognitionRejected;

                string languageInfo = selectedRecognizer.Culture.DisplayName;
                if (selectedRecognizer.Culture.Name.StartsWith("en"))
                {
                    languageInfo += " (İngilizce konuşun)";
                }
                else
                {
                    languageInfo += " (Bu dilde konuşun)";
                }

                UpdateStatus($"Ses tanıma sistemi hazır - Dil: {languageInfo}");
                
                MessageBox.Show($"Konuşma tanıma sistemi başlatıldı!\n\n{availableLanguages}\n" +
                    $"Seçilen dil: {languageInfo}\n\n" +
                    "Not: En iyi sonuç için İngilizce konuşmanız önerilir.", 
                    "Ses Tanıma Hazır", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ses tanıma sistemi başlatılamadı: {ex.Message}\n\n" +
                    "Çözüm:\n" +
                    "1. Windows Ayarlar > Zaman ve Dil > Dil\n" +
                    "2. İngilizce dilini ekleyin\n" +
                    "3. Konuşma paketini indirin\n\n" +
                    "Not: Türkçe konuşma tanıma Windows'ta sınırlıdır.", 
                    "Hata", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("Ses tanıma sistemi başlatılamadı");
            }
        }

        private void InitializeSpeechSynthesis()
        {
            try
            {
                speechSynthesizer = new SpeechSynthesizer();
                speechSynthesizer.SetOutputToDefaultAudioDevice();
                speechSynthesizer.Rate = 0;
                speechSynthesizer.Volume = 100;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ses sentezi sistemi başlatılamadı: {ex.Message}", "Hata", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private RecognizerInfo GetRecognizerInfo(string culture)
        {
            var recognizers = SpeechRecognitionEngine.InstalledRecognizers();
            foreach (var recognizer in recognizers)
            {
                if (recognizer.Culture.Name.StartsWith(culture))
                {
                    return recognizer;
                }
            }
            return recognizers.Count > 0 ? recognizers[0] : null;
        }

        private async void StartButton_Click(object sender, EventArgs e)
        {
            if (!isListening)
            {
                try
                {
                    await StartListening();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Dinleme başlatılamadı: {ex.Message}", "Hata", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UpdateStatus("Dinleme başlatılamadı");
                }
            }
        }

        private async void StopButton_Click(object sender, EventArgs e)
        {
            await StopListening();
        }

        private async Task StartListening()
        {
            try
            {
                speechRecognitionEngine.SetInputToDefaultAudioDevice();
                speechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
                
                isListening = true;
                startButton.Enabled = false;
                stopButton.Enabled = true;
                UpdateStatus("Dinleniyor... Konuşmaya başlayabilirsiniz");
                
                if (speechSynthesizer != null)
                {
                    await Task.Run(() => speechSynthesizer.SpeakAsync("Dinleme başladı"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Dinleme başlatılırken hata: {ex.Message}", "Hata", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                await StopListening();
            }
        }

        private async Task StopListening()
        {
            try
            {
                if (speechRecognitionEngine != null && isListening)
                {
                    speechRecognitionEngine.RecognizeAsyncStop();
                }
                
                isListening = false;
                startButton.Enabled = true;
                stopButton.Enabled = false;
                UpdateStatus("Dinleme durduruldu");
                
                if (speechSynthesizer != null)
                {
                    await Task.Run(() => speechSynthesizer.SpeakAsync("Dinleme durduruldu"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Dinleme durdurulurken hata: {ex.Message}", "Hata", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            this.Invoke(new Action(() =>
            {
                string recognizedText = e.Result.Text;
                
                if (textOutput.Text == "Metin burada görünecek...\n\nKonuşmaya başlamak için 'Başla' butonuna tıklayın.")
                {
                    textOutput.Text = recognizedText;
                }
                else
                {
                    textOutput.Text += " " + recognizedText;
                }
                
                textOutput.SelectionStart = textOutput.Text.Length;
                textOutput.ScrollToCaret();
                
                UpdateStatus($"Tanınan: {recognizedText}");
            }));
        }

        private void SpeechDetected(object sender, SpeechDetectedEventArgs e)
        {
            this.Invoke(new Action(() =>
            {
                UpdateStatus("Konuşma algılandı...");
            }));
        }

        private void SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            this.Invoke(new Action(() =>
            {
                UpdateStatus($"Tahmin: {e.Result.Text}");
            }));
        }

        private void SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            this.Invoke(new Action(() =>
            {
                UpdateStatus("Konuşma tanınamadı, tekrar deneyin");
            }));
        }

        private void UpdateStatus(string message)
        {
            if (statusLabel.InvokeRequired)
            {
                statusLabel.Invoke(new Action(() => statusLabel.Text = message));
            }
            else
            {
                statusLabel.Text = message;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (speechRecognitionEngine != null)
                {
                    speechRecognitionEngine.Dispose();
                }
                if (speechSynthesizer != null)
                {
                    speechSynthesizer.Dispose();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Uygulama kapatılırken hata: {ex.Message}", "Hata", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
