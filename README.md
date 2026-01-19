# QrCoder

![.NET](https://img.shields.io/badge/.NET-8.0-blueviolet)
![WPF](https://img.shields.io/badge/UI-WPF-blue)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)
![License](https://img.shields.io/badge/license-MIT-green)
![Status](https://img.shields.io/badge/status-stable-success)

QrCoder is a lightweight WPF desktop application that generates QR codes in bulk from a CSV file.  
Each valid record in the CSV produces a PNG QR code image, saved automatically to the user’s Desktop.

---

## Features

- Select and process CSV files via a graphical interface
- Asynchronous processing to keep the UI responsive
- Automatic QR code generation using the `UNID` column
- Progress tracking with counter and progress bar
- Output files saved as PNG images
- Automatic creation of an output folder on the Desktop
- Cross-language Windows compatibility

---

## How It Works

1. Select a CSV file.
2. The application scans and counts valid records.
3. Each row with a non-empty `UNID` value is processed.
4. A QR code is generated per record.
5. Files are saved to:


Each QR code is named after its corresponding `UNID` value.

---

## CSV Requirements

The CSV file **must**:

- Use `,` (comma) as delimiter
- Contain a header row
- Include a column named:


### Example

```csv
UNID,Name,Description
ABC123,Item One,Example
XYZ789,Item Two,Example
,,
```

## Technologies Used

- **.NET (WPF)** – Desktop application framework
- **CsvHelper** – Robust CSV parsing and mapping
- **ZXing.Net** – QR code generation library
- **System.Drawing** – Image creation and PNG export

---

## Dependencies

The following NuGet packages are required:

- CsvHelper
- ZXing.Net


---

## Output

- QR codes are generated as **PNG images**
- Image size: **300 × 300 pixels**
- Margin size: **1**
- Files are named after the corresponding `UNID` value
- Existing files with the same name are overwritten
- Output directory is created automatically if it does not exist

---

## Error Handling

- An error message is displayed if no valid `UNID` records are found
- Invalid or malformed CSV rows are ignored safely
- UI interaction is disabled during processing to prevent conflicts

---

## Windows Language Compatibility

The application resolves system paths using:

```csharp
Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
```