<div style="background-color: #FFCCCC; padding: 10px; border-left: 6px solid #FF0000; margin-bottom: 15px">
<strong>⚠️ 警告：</strong> 本项目目前处于Alpha阶段，可能出现不可用情况。请谨慎使用于生产环境！
</div>

# FSBProcessor 库

## 简介

FSBProcessor 是一个用于处理 FMOD 的 FSB (FSBank) 格式音频文件的 C# 库。该库提供了将 FSB 文件解码为普通音频格式（如 WAV）以及将普通音频打包为 FSB 格式的功能，方便 Unity/UE 游戏制作 mod 使用。

## 功能特点

- 解析 FSB 文件结构，提取音频信息
- 将 FSB 文件解码为 WAV 格式
- 将普通音频文件（如 WAV、MP3 等）打包为 FSB 格式
- 支持 WAV 文件直接打包为 FSB 格式
- 支持多种 FSB 版本（FSB1-FSB5）的完整解析
- 支持多种音频格式（PCM、MP3、Vorbis 等）
- 精确提取元数据中的采样率信息

## 系统要求

- .NET Standard 2.0 或更高版本
- 依赖 NAudio 库进行音频处理

## 安装方法

1. 克隆或下载本仓库
2. 使用 Visual Studio 打开 FSBProcessor.sln 解决方案文件
3. 编译项目生成 FSBLib.dll
4. 在您的项目中引用生成的 DLL 文件

## 使用示例

### 解析 FSB 文件

```csharp
using FSBLib;

// 创建 FSBProcessor 实例
var processor = new FSBProcessor();

// 解析 FSB 文件，获取音频信息
var samples = processor.ParseFSB("path/to/your/file.fsb");

// 显示音频信息
foreach (var sample in samples)
{
    Console.WriteLine($"名称: {sample.Name}");
    Console.WriteLine($"格式: {sample.Format}");
    Console.WriteLine($"通道数: {sample.Channels}");
    Console.WriteLine($"采样率: {sample.SampleRate} Hz");
    Console.WriteLine($"长度: {sample.Length} 字节");
    Console.WriteLine();
}
```

### 将 FSB 文件解码为 WAV 格式

```csharp
using FSBLib;

// 创建 FSBProcessor 实例
var processor = new FSBProcessor();

// 将 FSB 文件解码为 WAV 文件
var outputFiles = processor.ExtractToWav("path/to/your/file.fsb", "output/directory");

// 显示输出文件路径
Console.WriteLine("已解码的文件:");
foreach (var file in outputFiles)
{
    Console.WriteLine(file);
}
```

### 将普通音频文件打包为 FSB 格式

```csharp
using FSBLib;
using System.Collections.Generic;

// 创建 FSBProcessor 实例
var processor = new FSBProcessor();

// 准备要打包的音频文件列表
var audioFiles = new List<string>
{
    "path/to/audio1.wav",
    "path/to/audio2.mp3",
    "path/to/audio3.wav"
};

// 将音频文件打包为 FSB 文件
processor.PackToFSB(audioFiles, "output.fsb", FSBProcessor.FSBVersion.FSB5);

Console.WriteLine("打包完成: output.fsb");
```

### 将 WAV 文件打包为 FSB 格式

```csharp
using FSBLib;
using System.Collections.Generic;

// 创建 FSBProcessor 实例
var processor = new FSBProcessor();

// 准备要打包的WAV文件列表
var wavFiles = new List<string>
{
    "path/to/audio1.wav",
    "path/to/audio2.wav"
};

// 将WAV文件打包为FSB文件（保留原始格式）
processor.PackWavToFSB(wavFiles, "output.fsb", FSBProcessor.FSBVersion.FSB5);

Console.WriteLine("打包完成: output.fsb");
```

## 注意事项

- 库现已支持多种音频格式，包括 PCM、MP3 和 Vorbis 格式
- 新增对 WAV 文件的直接打包支持，保留原始音频格式和质量
- FSB5 格式现已得到完整支持，包括元数据采样率的精确提取
- 对于某些特殊的 FSB 文件，可能需要进一步的格式适配
- 使用 PackWavToFSB 方法可以获得更好的音频质量和更精确的格式控制

## 许可证

本项目采用 Apache License 2.0

## 贡献

欢迎提交问题报告和改进建议！