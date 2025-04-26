using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace FSBLib
{
    /// <summary>
    /// FSB文件处理类，提供FSB文件的解码和打包功能
    /// </summary>
    public class FSBProcessor
    {
        #region FSB文件结构定义

        // FSB文件头标识
        private const uint FSB_HEADER_ID = 0x42534646; // "FSBF"

        // FSB版本
        public enum FSBVersion
        {
            FSB1 = 1,
            FSB2 = 2,
            FSB3 = 3,
            FSB4 = 4,
            FSB5 = 5
        }

        // 音频格式
        public enum AudioFormat
        {
            PCM8 = 1,
            PCM16 = 2,
            PCM24 = 3,
            PCM32 = 4,
            PCMFLOAT = 5,
            GCADPCM = 6,
            IMAADPCM = 7,
            VAG = 8,
            HEVAG = 9,
            XMA = 10,
            MPEG = 11,
            CELT = 12,
            AT9 = 13,
            XWMA = 14,
            VORBIS = 15
        }

        // FSB文件头
        private class FSBHeader
        {
            public uint HeaderId { get; set; }        // 文件标识 "FSBF"
            public uint Version { get; set; }         // FSB版本
            public uint NumSamples { get; set; }      // 样本数量
            public uint SampleHeaderSize { get; set; } // 样本头大小
            public uint NameTableSize { get; set; }   // 名称表大小
            public uint DataSize { get; set; }        // 数据大小
            public uint Mode { get; set; }            // 模式标志
        }

        // 样本信息
        public class SampleInfo
        {
            public string Name { get; set; }          // 样本名称
            public uint Length { get; set; }          // 样本长度（字节）
            public uint LoopStart { get; set; }       // 循环开始点
            public uint LoopEnd { get; set; }         // 循环结束点
            public uint SampleRate { get; set; }      // 采样率
            public ushort Channels { get; set; }      // 通道数
            public AudioFormat Format { get; set; }    // 音频格式
            public uint Offset { get; set; }          // 数据偏移量
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 解析FSB文件，提取音频信息
        /// </summary>
        /// <param name="fsbFilePath">FSB文件路径</param>
        /// <returns>提取的样本信息列表</returns>
        public List<SampleInfo> ParseFSB(string fsbFilePath)
        {
            if (!File.Exists(fsbFilePath))
                throw new FileNotFoundException("FSB文件不存在", fsbFilePath);

            using (var stream = File.OpenRead(fsbFilePath))
            using (var reader = new BinaryReader(stream))
            {
                // 读取FSB文件头
                var header = ReadFSBHeader(reader);

                // 验证文件头标识
                if (header.HeaderId != FSB_HEADER_ID)
                    throw new InvalidDataException("无效的FSB文件格式");

                // 读取样本信息
                var samples = new List<SampleInfo>();
                for (int i = 0; i < header.NumSamples; i++)
                {
                    samples.Add(ReadSampleInfo(reader, (FSBVersion)header.Version));
                }

                // 读取名称表
                if (header.NameTableSize > 0)
                {
                    ReadNameTable(reader, samples, header.NameTableSize);
                }

                return samples;
            }
        }

        /// <summary>
        /// 将FSB文件解码为WAV文件
        /// </summary>
        /// <param name="fsbFilePath">FSB文件路径</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <returns>解码的WAV文件路径列表</returns>
        public List<string> ExtractToWav(string fsbFilePath, string outputDirectory)
        {
            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            var samples = ParseFSB(fsbFilePath);
            var outputFiles = new List<string>();

            using (var fsbStream = File.OpenRead(fsbFilePath))
            {
                foreach (var sample in samples)
                {
                    string outputPath = Path.Combine(outputDirectory, $"{sample.Name}.wav");
                    ExtractSampleToWav(fsbStream, sample, outputPath);
                    outputFiles.Add(outputPath);
                }
            }

            return outputFiles;
        }

        /// <summary>
        /// 将普通音频文件打包为FSB文件
        /// </summary>
        /// <param name="audioFilePaths">音频文件路径列表</param>
        /// <param name="outputFsbPath">输出FSB文件路径</param>
        /// <param name="version">FSB版本</param>
        public void PackToFSB(List<string> audioFilePaths, string outputFsbPath, FSBVersion version = FSBVersion.FSB5)
        {
            if (audioFilePaths == null || audioFilePaths.Count == 0)
                throw new ArgumentException("必须提供至少一个音频文件", nameof(audioFilePaths));

            // 检查所有文件是否存在
            foreach (var filePath in audioFilePaths)
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"音频文件不存在: {filePath}");
            }

            // 准备样本数据
            var samples = new List<SampleInfo>();
            var audioData = new List<byte[]>();

            foreach (var filePath in audioFilePaths)
            {
                // 读取音频文件并转换为PCM格式
                using (var reader = new AudioFileReader(filePath))
                {
                    var waveFormat = reader.WaveFormat;
                    var sample = new SampleInfo
                    {
                        Name = Path.GetFileNameWithoutExtension(filePath),
                        SampleRate = (uint)waveFormat.SampleRate,
                        Channels = (ushort)waveFormat.Channels,
                        Format = AudioFormat.PCM16 // 默认使用PCM16格式
                    };

                    // 读取音频数据
                    byte[] buffer = new byte[reader.Length];
                    reader.Read(buffer, 0, buffer.Length);
                    audioData.Add(buffer);

                    sample.Length = (uint)buffer.Length;
                    samples.Add(sample);
                }
            }

            // 写入FSB文件
            using (var stream = File.Create(outputFsbPath))
            using (var writer = new BinaryWriter(stream))
            {
                WriteFSBFile(writer, samples, audioData, version);
            }
        }

        #endregion

        #region 私有辅助方法

        // 读取FSB文件头
        private FSBHeader ReadFSBHeader(BinaryReader reader)
        {
            var header = new FSBHeader
            {
                HeaderId = reader.ReadUInt32(),
                Version = reader.ReadUInt32(),
                NumSamples = reader.ReadUInt32(),
                SampleHeaderSize = reader.ReadUInt32(),
                NameTableSize = reader.ReadUInt32(),
                DataSize = reader.ReadUInt32(),
                Mode = reader.ReadUInt32()
            };

            return header;
        }

        // 读取样本信息
        private SampleInfo ReadSampleInfo(BinaryReader reader, FSBVersion version)
        {
            var sample = new SampleInfo();

            // 根据不同的FSB版本读取样本信息
            switch (version)
            {
                case FSBVersion.FSB5:
                    // FSB5格式的样本头解析
                    sample.Length = reader.ReadUInt32();
                    uint metadataInfo = reader.ReadUInt32();
                    sample.Offset = reader.ReadUInt32();
                    sample.Format = (AudioFormat)(metadataInfo & 0x1F);
                    sample.Channels = (ushort)((metadataInfo >> 5) & 0x3);
                    sample.SampleRate = 44100; // 默认采样率，实际应从元数据中提取
                    break;

                default:
                    // 其他版本的FSB格式解析（简化版）
                    sample.Length = reader.ReadUInt32();
                    sample.Offset = reader.ReadUInt32();
                    ushort flags = reader.ReadUInt16();
                    sample.Format = (AudioFormat)(flags & 0x1F);
                    sample.Channels = (ushort)((flags >> 5) & 0x3);
                    sample.SampleRate = reader.ReadUInt32();
                    sample.LoopStart = reader.ReadUInt32();
                    sample.LoopEnd = reader.ReadUInt32();
                    break;
            }

            return sample;
        }

        // 读取名称表
        private void ReadNameTable(BinaryReader reader, List<SampleInfo> samples, uint nameTableSize)
        {
            long startPos = reader.BaseStream.Position;
            long endPos = startPos + nameTableSize;

            for (int i = 0; i < samples.Count && reader.BaseStream.Position < endPos; i++)
            {
                StringBuilder nameBuilder = new StringBuilder();
                char c;
                while ((c = reader.ReadChar()) != '\0' && reader.BaseStream.Position < endPos)
                {
                    nameBuilder.Append(c);
                }

                samples[i].Name = nameBuilder.ToString();
                if (string.IsNullOrEmpty(samples[i].Name))
                {
                    samples[i].Name = $"sample_{i}";
                }
            }
        }

        // 提取样本到WAV文件
        private void ExtractSampleToWav(FileStream fsbStream, SampleInfo sample, string outputPath)
        {
            // 定位到样本数据
            fsbStream.Seek(sample.Offset, SeekOrigin.Begin);

            // 读取样本数据
            byte[] sampleData = new byte[sample.Length];
            fsbStream.Read(sampleData, 0, (int)sample.Length);

            // 根据音频格式解码
            byte[] pcmData = DecodeSampleData(sampleData, sample);

            // 创建WAV文件
            using (var waveStream = new MemoryStream())
            {
                var waveFormat = new WaveFormat((int)sample.SampleRate, 16, sample.Channels);
                using (var writer = new WaveFileWriter(outputPath, waveFormat))
                {
                    writer.Write(pcmData, 0, pcmData.Length);
                }
            }
        }

        // 解码样本数据
        private byte[] DecodeSampleData(byte[] sampleData, SampleInfo sample)
        {
            // 根据不同的音频格式进行解码
            switch (sample.Format)
            {
                case AudioFormat.PCM8:
                case AudioFormat.PCM16:
                case AudioFormat.PCM24:
                case AudioFormat.PCM32:
                case AudioFormat.PCMFLOAT:
                    // PCM格式可以直接使用
                    return sampleData;

                case AudioFormat.MPEG:
                    // 对于MP3格式，需要使用MP3解码器
                    using (var mp3Stream = new MemoryStream(sampleData))
                    using (var mp3Reader = new Mp3FileReader(mp3Stream))
                    using (var outStream = new MemoryStream())
                    {
                        var buffer = new byte[4096];
                        int read;
                        while ((read = mp3Reader.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            outStream.Write(buffer, 0, read);
                        }
                        return outStream.ToArray();
                    }

                // 其他格式的解码实现...
                default:
                    // 对于不支持的格式，返回原始数据
                    Console.WriteLine($"警告: 不支持的音频格式 {sample.Format}，返回原始数据");
                    return sampleData;
            }
        }

        // 写入FSB文件
        private void WriteFSBFile(BinaryWriter writer, List<SampleInfo> samples, List<byte[]> audioData, FSBVersion version)
        {
            // 计算样本头大小
            uint sampleHeaderSize = (uint)(samples.Count * 24); // 简化的样本头大小计算

            // 计算名称表大小
            uint nameTableSize = 0;
            foreach (var sample in samples)
            {
                nameTableSize += (uint)(sample.Name.Length + 1); // +1 for null terminator
            }

            // 计算数据大小
            uint dataSize = 0;
            foreach (var data in audioData)
            {
                dataSize += (uint)data.Length;
            }

            // 写入文件头
            writer.Write(FSB_HEADER_ID); // HeaderId
            writer.Write((uint)version);  // Version
            writer.Write((uint)samples.Count); // NumSamples
            writer.Write(sampleHeaderSize); // SampleHeaderSize
            writer.Write(nameTableSize); // NameTableSize
            writer.Write(dataSize); // DataSize
            writer.Write((uint)0); // Mode

            // 计算数据偏移量
            uint currentOffset = (uint)(28 + sampleHeaderSize + nameTableSize); // 28是文件头大小

            // 写入样本头
            for (int i = 0; i < samples.Count; i++)
            {
                var sample = samples[i];
                sample.Offset = currentOffset;

                // 写入样本信息
                writer.Write(sample.Length);
                
                if (version == FSBVersion.FSB5)
                {
                    // FSB5格式的元数据信息
                    uint metadataInfo = (uint)sample.Format | ((uint)sample.Channels << 5);
                    writer.Write(metadataInfo);
                    writer.Write(sample.Offset);
                }
                else
                {
                    // 其他版本的FSB格式
                    writer.Write(sample.Offset);
                    ushort flags = (ushort)((ushort)sample.Format | (sample.Channels << 5));
                    writer.Write(flags);
                    writer.Write(sample.SampleRate);
                    writer.Write(sample.LoopStart);
                    writer.Write(sample.LoopEnd);
                }

                currentOffset += (uint)audioData[i].Length;
            }

            // 写入名称表
            foreach (var sample in samples)
            {
                byte[] nameBytes = Encoding.ASCII.GetBytes(sample.Name);
                writer.Write(nameBytes);
                writer.Write((byte)0); // null terminator
            }

            // 写入音频数据
            foreach (var data in audioData)
            {
                writer.Write(data);
            }
        }

        #endregion
    }
}