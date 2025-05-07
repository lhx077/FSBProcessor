> **⚠️ Warning:** This project is currently in Alpha stage and may not be fully functional. Please use with caution in production environments!

# FSBProcessor Library (FSBLib)

## Introduction

FSBProcessor(FSBLib) is a C# library for processing FMOD's FSB (FSBank) format audio files. The library provides functionality to decode FSB files into common audio formats (such as WAV) and to package regular audio into FSB format, making it convenient for Unity/UE game modding.

## Features

- Parse FSB file structure and extract audio information
- Decode FSB files to WAV format
- Package common audio files (such as WAV, MP3, etc.) into FSB format
- Support direct packaging of WAV files into FSB format
- Support complete parsing of multiple FSB versions (FSB1-FSB5)
- Support multiple audio formats (PCM, MP3, Vorbis, etc.)
- Precisely extract sample rate information from metadata

## System Requirements

- .NET Standard 2.0 or higher
- Depends on NAudio library for audio processing

## Installation

1. Clone or download this repository
2. Open the FSBProcessor.sln solution file with Visual Studio
3. Compile the project to generate FSBLib.dll
4. Reference the generated DLL file in your project

## Usage Examples

### Parsing FSB Files

```csharp
using FSBLib;

// Create FSBProcessor instance
var processor = new FSBProcessor();

// Parse FSB file to get audio information
var samples = processor.ParseFSB("path/to/your/file.fsb");

// Display audio information
foreach (var sample in samples)
{
    Console.WriteLine($"Name: {sample.Name}");
    Console.WriteLine($"Format: {sample.Format}");
    Console.WriteLine($"Channels: {sample.Channels}");
    Console.WriteLine($"Sample Rate: {sample.SampleRate} Hz");
    Console.WriteLine($"Length: {sample.Length} bytes");
    Console.WriteLine();
}
```

### Decoding FSB Files to WAV Format

```csharp
using FSBLib;

// Create FSBProcessor instance
var processor = new FSBProcessor();

// Decode FSB file to WAV files
var outputFiles = processor.ExtractToWav("path/to/your/file.fsb", "output/directory");

// Display output file paths
Console.WriteLine("Decoded files:");
foreach (var file in outputFiles)
{
    Console.WriteLine(file);
}
```

### Packaging Regular Audio Files into FSB Format

```csharp
using FSBLib;
using System.Collections.Generic;

// Create FSBProcessor instance
var processor = new FSBProcessor();

// Prepare a list of audio files to package
var audioFiles = new List<string>
{
    "path/to/audio1.wav",
    "path/to/audio2.mp3",
    "path/to/audio3.wav"
};

// Package audio files into an FSB file
processor.PackToFSB(audioFiles, "output.fsb", FSBProcessor.FSBVersion.FSB5);

Console.WriteLine("Packaging complete: output.fsb");
```

### Packaging WAV Files into FSB Format

```csharp
using FSBLib;
using System.Collections.Generic;

// Create FSBProcessor instance
var processor = new FSBProcessor();

// Prepare a list of WAV files to package
var wavFiles = new List<string>
{
    "path/to/audio1.wav",
    "path/to/audio2.wav"
};

// Package WAV files into an FSB file (preserving original format)
processor.PackWavToFSB(wavFiles, "output.fsb", FSBProcessor.FSBVersion.FSB5);

Console.WriteLine("Packaging complete: output.fsb");
```

## Notes

- The library now supports multiple audio formats, including PCM, MP3, and Vorbis formats
- Added direct packaging support for WAV files, preserving original audio format and quality
- FSB5 format is now fully supported, including precise extraction of sample rate from metadata
- For some special FSB files, further format adaptation may be required
- Using the PackWavToFSB method provides better audio quality and more precise format control

## License

This project is licensed under the Apache License 2.0

## Contribution

Issue reports and improvement suggestions are welcome!
