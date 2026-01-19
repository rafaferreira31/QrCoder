using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Win32;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using ZXing;
using ZXing.Common;

namespace QrCoder
{
    public partial class MainWindow : Window
    {
        private string _outputFolder = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
        }


        
        /// <summary>
        /// Handles the Click event of the Select CSV button, allowing the user to choose a CSV file and initiating its
        /// processing asynchronously.
        /// </summary>
        /// <remarks>While the CSV file is being processed, the Select CSV button is disabled and a
        /// progress bar is displayed. If an error occurs during processing, an error message is shown to the
        /// user.</remarks>
        /// <param name="sender">The source of the event, typically the Select CSV button.</param>
        /// <param name="e">The event data associated with the Click event.</param>
        private async void BtnSelectCSV_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new()
            {
                Filter = "CSV Files (*.csv)|*.csv"
            };

            if (ofd.ShowDialog() != true)
                return;

            btnSelectCSV.IsEnabled = false;
            progressBar.Value = 0;
            lblDone.Visibility = Visibility.Collapsed;
            lblCounter.Content = "0 of 0";

            try
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                await ProcessCsvAsync(ofd.FileName);
                lblDone.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnSelectCSV.IsEnabled = true;
                Mouse.OverrideCursor = null;
            }
        }



        /// <summary>
        /// Processes a CSV file containing records and generates QR codes for each valid entry asynchronously.
        /// </summary>
        /// <remarks>The generated QR codes are saved to a 'QRCodes' folder on the user's desktop. The
        /// method updates progress indicators during processing. Only records with a non-empty 'UNID' field are
        /// processed.</remarks>
        /// <param name="csvPath">The full file path to the CSV file to process. The file must exist and be accessible for reading.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="Exception">Thrown if the CSV file does not contain any valid records.</exception>
        private async Task ProcessCsvAsync(string csvPath)
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            _outputFolder = Path.Combine(desktop, "QRCodes");
            Directory.CreateDirectory(_outputFolder);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ",",
                HasHeaderRecord = true,
                BadDataFound = null,
                MissingFieldFound = null,
                HeaderValidated = null
            };

            int total = await CountUnidsAsync(csvPath, config);
            if (total == 0)
                throw new Exception("No valid records found.");

            progressBar.Minimum = 0;
            progressBar.Maximum = total;
            progressBar.Value = 0;

            lblCounter.Content = $"0 of {total}";

            using var reader = new StreamReader(csvPath, Encoding.UTF8);
            using var csv = new CsvReader(reader, config);

            await csv.ReadAsync();
            csv.ReadHeader();

            int current = 0;

            while (await csv.ReadAsync())
            {
                var unid = csv.GetField("UNID")?.Trim();
                if (string.IsNullOrWhiteSpace(unid))
                    continue;

                await Task.Run(() => GenerateQrCode(unid, _outputFolder));

                current++;

                Dispatcher.Invoke(() =>
                {
                    progressBar.Value = current;
                    lblCounter.Content = $"{current} of {total}";
                });

                await Task.Yield();
            }

            progressBar.Value = progressBar.Maximum;
        }



        /// <summary>
        /// Generates a QR code image from the specified text and saves it as a PNG file in the given destination
        /// folder.
        /// </summary>
        /// <remarks>The generated PNG file will be named based on the sanitized input text. If a file
        /// with the same name already exists in the destination folder, it will be overwritten.</remarks>
        /// <param name="text">The text to encode in the QR code. Cannot be null or empty.</param>
        /// <param name="destinationFolder">The path to the folder where the generated PNG file will be saved. Must be a valid, existing directory.</param>
        private void GenerateQrCode(string text, string destinationFolder)
        {
            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions
                {
                    Height = 300,
                    Width = 300,
                    Margin = 1
                }
            };

            var pixelData = writer.Write(text);

            using var bitmap = new Bitmap(
                pixelData.Width,
                pixelData.Height,
                PixelFormat.Format32bppRgb);

            for (int y = 0; y < pixelData.Height; y++)
            {
                for (int x = 0; x < pixelData.Width; x++)
                {
                    int index = (y * pixelData.Width + x) * 4;
                    byte gray = pixelData.Pixels[index];
                    bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(gray, gray, gray));
                }
            }

            string finalPath = Path.Combine(
                destinationFolder,
                $"{SanitizeFileName(text)}.png");

            bitmap.Save(finalPath, ImageFormat.Png);
        }



        /// <summary>
        /// Sanitizes a string so that it can be safely used as a file name by replacing all invalid
        /// file system characters with an underscore.
        /// </summary>
        /// <param name="fileName">
        /// The original file name string to sanitize. This value may contain characters that are
        /// not permitted in file names.
        /// </param>
        /// <returns>
        /// A sanitized version of the input string, safe to use as a file name in the operating system.
        /// </returns>
        private string SanitizeFileName(string fileName)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(c, '_');
            return fileName;
        }



        /// <summary>
        /// Counts the number of valid records in a CSV file by evaluating the presence of a non-empty
        /// value in the 'UNID' column.
        /// </summary>
        /// <remarks>
        /// The CSV file is read sequentially using the provided CsvHelper configuration. Only rows
        /// where the 'UNID' field contains a non-null and non-whitespace value are counted.
        /// </remarks>
        /// <param name="csvPath">
        /// The full file path to the CSV file to analyze. The file must exist and be accessible for reading.
        /// </param>
        /// <param name="config">
        /// The CsvHelper configuration used to parse the CSV file, including delimiter and header settings.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the total number
        /// of valid 'UNID' records found in the CSV file.
        /// </returns>
        private async Task<int> CountUnidsAsync(string csvPath, CsvConfiguration config)
        {
            int count = 0;

            using var reader = new StreamReader(csvPath, Encoding.UTF8);
            using var csv = new CsvReader(reader, config);

            await csv.ReadAsync();
            csv.ReadHeader();

            while (await csv.ReadAsync())
            {
                var unid = csv.GetField("UNID")?.Trim();
                if (!string.IsNullOrWhiteSpace(unid))
                    count++;
            }

            return count;
        }
    }
}
