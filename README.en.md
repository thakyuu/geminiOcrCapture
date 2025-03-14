# Gemini OCR Capture

Gemini OCR Capture is a simple and user-friendly OCR tool that extracts text from screen captures using Google Gemini 2.0 Flash API.

## Features

- **Screen Capture**: Full screen capture or selection area capture
- **OCR Processing**: High-precision text extraction using Google Gemini 2.0 Flash API
- **Multi-language Support**: Supports multiple languages including Japanese
- **Simple UI**: Intuitive and easy-to-use interface

## Requirements

- Windows 10/11
- .NET 9.0 or higher
- Google Cloud Platform account
- Gemini API key

## Installation

1. Download the latest version from the release page
2. Extract the downloaded ZIP file
3. Run `GeminiOcrCapture.exe` in the extracted folder

## Initial Setup

### Getting an API Key from Google Cloud Platform

1. Access the [Google Cloud Console](https://console.cloud.google.com/)
2. Create or select a project
3. Select "APIs & Services" → "Library"
4. Search for "Gemini API" and enable it
5. Select "APIs & Services" → "Credentials"
6. Select "Create Credentials" → "API Key"
7. Copy the created API key

### Application Settings

1. Launch the application
2. Open the settings screen
3. Enter the copied API key
4. Change language settings and shortcut keys as needed
5. Save the settings

## How to Use

### Full Screen Capture

1. Click the "Full Screen Capture" button on the main screen of the application (or press the shortcut key you set)
2. The entire screen will be captured, and OCR processing will start automatically
3. The extracted text will be displayed

### Selection Area Capture

1. Click the "Selection Area Capture" button on the main screen of the application
2. Drag to select the area you want to capture
3. The selected area will be captured, and OCR processing will start automatically
4. The extracted text will be displayed

## Troubleshooting

### API Key Errors

- Check if the API key is set correctly
- Check if Gemini API is enabled in Google Cloud Console
- Check if the API key has appropriate permissions
- Check if billing is enabled

### OCR Processing Errors

- Check your internet connection
- If the image size is too large, select a smaller area
- If you have exceeded the API quota, check the billing settings in Google Cloud Console

### Other Errors

- Restart the application
- Update to the latest version
- Check the error log (`error.log` in the application folder)

## Developer Information

### Project Structure

- **GeminiOcrCapture**: Main application (UI)
- **GeminiOcrCapture.Core**: Core library (OCR processing, settings management, etc.)
- **GeminiOcrCapture.Tests**: Test project

### Build Instructions

```powershell
# Clone the repository
git clone https://github.com/yourusername/GeminiOcrCapture.git
cd GeminiOcrCapture

# Build
dotnet build --configuration Release

# Run
dotnet run --project src\GeminiOcrCapture
```

### Running Tests

```powershell
dotnet test
```

## License

This project is released under the MIT License. See the [LICENSE](LICENSE) file for details.

## Acknowledgements

- [Google Gemini API](https://ai.google.dev/gemini-api)
- [.NET](https://dotnet.microsoft.com/)
- All others who have contributed to this project

## Contact

Please report bugs or feature requests on the GitHub Issues page. 