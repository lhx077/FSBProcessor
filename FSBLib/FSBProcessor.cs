/*
 * FSBProcessor - FSB音频文件处理库
 * 
 * 作者: lhx077
 * 项目名称: FSBProcessor
 * 协议: Apache License 2.0
 * 日期: 2025-04-27
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NAudio.Wave;
using NAudio.Vorbis;

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

        /// <summary>
        /// 将WAV文件打包为FSB文件
        /// </summary>
        /// <param name="wavFilePaths">WAV文件路径列表</param>
        /// <param name="outputFsbPath">输出FSB文件路径</param>
        /// <param name="version">FSB版本</param>
        public void PackWavToFSB(List<string> wavFilePaths, string outputFsbPath, FSBVersion version = FSBVersion.FSB5)
        {
            if (wavFilePaths == null || wavFilePaths.Count == 0)
                throw new ArgumentException("必须提供至少一个WAV文件", nameof(wavFilePaths));

            // 检查所有文件是否存在且为WAV格式
            foreach (var filePath in wavFilePaths)
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"WAV文件不存在: {filePath}");
                
                if (!Path.GetExtension(filePath).Equals(".wav", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException($"文件不是WAV格式: {filePath}");
            }

            // 准备样本数据
            var samples = new List<SampleInfo>();
            var audioData = new List<byte[]>();

            foreach (var filePath in wavFilePaths)
            {
                using (var waveReader = new WaveFileReader(filePath))
                {
                    var waveFormat = waveReader.WaveFormat;
                    var sample = new SampleInfo
                    {
                        Name = Path.GetFileNameWithoutExtension(filePath),
                        SampleRate = (uint)waveFormat.SampleRate,
                        Channels = (ushort)waveFormat.Channels,
                        Format = GetAudioFormatFromWaveFormat(waveFormat)
                    };

                    // 读取WAV数据（跳过头部）
                    byte[] buffer = new byte[waveReader.Length];
                    waveReader.Read(buffer, 0, buffer.Length);
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
                    
                    // 从元数据中提取格式信息
                    sample.Format = (AudioFormat)(metadataInfo & 0x1F);
                    sample.Channels = (ushort)((metadataInfo >> 5) & 0x3);
                    
                    // 从元数据中提取采样率
                    uint freqIndex = (metadataInfo >> 7) & 0xF;
                    sample.SampleRate = GetSampleRateFromIndex(freqIndex);
                    
                    // 读取循环点信息（如果有）
                    if ((metadataInfo & (1 << 11)) != 0)
                    {
                        sample.LoopStart = reader.ReadUInt32();
                        sample.LoopEnd = reader.ReadUInt32();
                    }
                    break;

                case FSBVersion.FSB1:
                case FSBVersion.FSB2:
                    // FSB1和FSB2格式解析
                    sample.Length = reader.ReadUInt32();
                    sample.Offset = reader.ReadUInt32();
                    ushort flags1 = reader.ReadUInt16();
                    sample.Format = (AudioFormat)(flags1 & 0x7);
                    sample.Channels = (ushort)(((flags1 >> 3) & 0x3) + 1);
                    sample.SampleRate = reader.ReadUInt32();
                    sample.LoopStart = reader.ReadUInt32();
                    sample.LoopEnd = reader.ReadUInt32();
                    break;

                case FSBVersion.FSB3:
                case FSBVersion.FSB4:
                    // FSB3和FSB4格式解析
                    sample.Length = reader.ReadUInt32();
                    sample.Offset = reader.ReadUInt32();
                    ushort flags2 = reader.ReadUInt16();
                    sample.Format = (AudioFormat)(flags2 & 0x1F);
                    sample.Channels = (ushort)(((flags2 >> 5) & 0x3) + 1);
                    sample.SampleRate = reader.ReadUInt32();
                    sample.LoopStart = reader.ReadUInt32();
                    sample.LoopEnd = reader.ReadUInt32();
                    
                    // 读取额外的元数据（如果有）
                    if (version == FSBVersion.FSB4)
                    {
                        uint extraFlags = reader.ReadUInt32();
                        // 处理额外的标志信息
                    }
                    break;

                default:
                    throw new NotSupportedException($"不支持的FSB版本: {version}");
            }

            return sample;
        }

        // 从频率索引获取采样率
        private uint GetSampleRateFromIndex(uint freqIndex)
        {
            // FSB5中的采样率索引表
            uint[] sampleRates = new uint[]
            {
                4000, 8000, 11025, 12000, 16000, 22050, 24000, 32000,
                44100, 48000, 96000, 192000, 384000, 0, 0, 0
            };

            if (freqIndex < sampleRates.Length && sampleRates[freqIndex] != 0)
            {
                return sampleRates[freqIndex];
            }
            
            // 如果索引无效或未知，返回默认采样率
            return 44100;
        }

        // 从WAV格式获取FSB音频格式
        private AudioFormat GetAudioFormatFromWaveFormat(WaveFormat waveFormat)
        {
            if (waveFormat.Encoding == WaveFormatEncoding.Pcm)
            {
                switch (waveFormat.BitsPerSample)
                {
                    case 8: return AudioFormat.PCM8;
                    case 16: return AudioFormat.PCM16;
                    case 24: return AudioFormat.PCM24;
                    case 32: return AudioFormat.PCM32;
                    default: return AudioFormat.PCM16;
                }
            }
            else if (waveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                return AudioFormat.PCMFLOAT;
            }
            else if (waveFormat.Encoding == WaveFormatEncoding.Adpcm)
            {
                return AudioFormat.IMAADPCM;
            }
            else if (waveFormat.Encoding == WaveFormatEncoding.MpegLayer3)
            {
                return AudioFormat.MPEG;
            }
            
            // 默认返回PCM16
            return AudioFormat.PCM16;
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
                // 创建适当的WaveFormat
                WaveFormat waveFormat = CreateWaveFormat(sample);
                
                // 创建WAV文件
                using (var writer = new WaveFileWriter(outputPath, waveFormat))
                {
                    writer.Write(pcmData, 0, pcmData.Length);
                }
            }
        }

        // 创建适合样本的WaveFormat
        private WaveFormat CreateWaveFormat(SampleInfo sample)
        {
            int bitsPerSample = 16; // 默认为16位
            
            // 根据音频格式确定位深度
            switch (sample.Format)
            {
                case AudioFormat.PCM8:
                    bitsPerSample = 8;
                    break;
                case AudioFormat.PCM16:
                    bitsPerSample = 16;
                    break;
                case AudioFormat.PCM24:
                    bitsPerSample = 24;
                    break;
                case AudioFormat.PCM32:
                case AudioFormat.PCMFLOAT:
                    bitsPerSample = 32;
                    break;
                default:
                    bitsPerSample = 16; // 对于其他格式，默认使用16位
                    break;
            }
            
            // 创建WaveFormat
            if (sample.Format == AudioFormat.PCMFLOAT)
            {
                return WaveFormat.CreateIeeeFloatWaveFormat((int)sample.SampleRate, sample.Channels);
            }
            else
            {
                return new WaveFormat((int)sample.SampleRate, bitsPerSample, sample.Channels);
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

                case AudioFormat.IMAADPCM:
                    // 解码ADPCM格式
                    using (var adpcmStream = new MemoryStream(sampleData))
                    {
                        var adpcmFormat = new AdpcmWaveFormat((int)sample.SampleRate, sample.Channels);
                        using (var adpcmReader = new WaveFormatConversionStream(
                            new WaveFormat((int)sample.SampleRate, 16, sample.Channels), 
                            new RawSourceWaveStream(adpcmStream, adpcmFormat)))
                        using (var outStream = new MemoryStream())
                        {
                            var buffer = new byte[4096];
                            int read;
                            while ((read = adpcmReader.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                outStream.Write(buffer, 0, read);
                            }
                            return outStream.ToArray();
                        }
                    }

                case AudioFormat.VORBIS:
                    // 解码Vorbis格式
                    using (var vorbisStream = new MemoryStream(sampleData))
                    using (var vorbisReader = new VorbisWaveReader(vorbisStream))
                    using (var outStream = new MemoryStream())
                    {
                        var buffer = new byte[4096];
                        int read;
                        while ((read = vorbisReader.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            outStream.Write(buffer, 0, read);
                        }
                        return outStream.ToArray();
                    }

                default:
                    // 对于不支持的格式，返回原始数据
                    Console.WriteLine($"警告: 不支持的音频格式 {sample.Format}，返回原始数据");
                    return sampleData;
            }
        }

        // 写入FSB文件
        private void WriteFSBFile(BinaryWriter writer, List<SampleInfo> samples, List<byte[]> audioData, FSBVersion version)
        {
            // 计算名称表大小
            uint nameTableSize = 0;
            foreach (var sample in samples)
            {
                nameTableSize += (uint)(sample.Name.Length + 1); // +1 是为了包含结尾的 null 字符
            }

            // 计算样本头大小
            uint sampleHeaderSize = 0;
            switch (version)
            {
                case FSBVersion.FSB5:
                    sampleHeaderSize = (uint)(samples.Count * 16); // FSB5的样本头是固定大小
                    break;
                case FSBVersion.FSB1:
                case FSBVersion.FSB2:
                    sampleHeaderSize = (uint)(samples.Count * 24);
                    break;
                case FSBVersion.FSB3:
                case FSBVersion.FSB4:
                    sampleHeaderSize = (uint)(samples.Count * 28);
                    break;
                default:
                    throw new NotSupportedException($"不支持的FSB版本: {version}");
            }

            // 计算数据大小
            uint dataSize = 0;
            foreach (var data in audioData)
            {
                dataSize += (uint)data.Length;
            }

            // 写入FSB文件头
            writer.Write(FSB_HEADER_ID);
            writer.Write((uint)version);
            writer.Write((uint)samples.Count);
            writer.Write(sampleHeaderSize);
            writer.Write(nameTableSize);
            writer.Write(dataSize);
            writer.Write((uint)0); // 模式标志，默认为0

            // 计算数据偏移量
            uint currentOffset = (uint)(36 + sampleHeaderSize + nameTableSize); // 36是FSB文件头的大小

            // 写入样本头
            for (int i = 0; i < samples.Count; i++)
            {
                var sample = samples[i];
                sample.Offset = currentOffset;

                switch (version)
                {
                    case FSBVersion.FSB5:
                        // 写入FSB5格式的样本头
                        writer.Write(sample.Length);
                        
                        // 构建元数据信息
                        uint freqIndex = GetFrequencyIndex(sample.SampleRate);
                        uint metadataInfo = (uint)(
                            (int)sample.Format |
                            ((sample.Channels - 1) << 5) |
                            (freqIndex << 7)
                        );
                        
                        // 如果有循环点，设置循环标志
                        if (sample.LoopStart != 0 || sample.LoopEnd != 0)
                        {
                            metadataInfo |= (1 << 11);
                        }
                        
                        writer.Write(metadataInfo);
                        writer.Write(sample.Offset);
                        
                        // 如果有循环点，写入循环信息
                        if (sample.LoopStart != 0 || sample.LoopEnd != 0)
                        {
                            writer.Write(sample.LoopStart);
                            writer.Write(sample.LoopEnd);
                        }
                        break;

                    case FSBVersion.FSB1:
                    case FSBVersion.FSB2:
                        // 写入FSB1/FSB2格式的样本头
                        writer.Write(sample.Length);
                        writer.Write(sample.Offset);
                        
                        ushort flags1 = (ushort)(
                            (int)sample.Format |
                            ((sample.Channels - 1) << 3)
                        );
                        
                        writer.Write(flags1);
                        writer.Write(sample.SampleRate);
                        writer.Write(sample.LoopStart);
                        writer.Write(sample.LoopEnd);
                        break;

                    case FSBVersion.FSB3:
                    case FSBVersion.FSB4:
                        // 写入FSB3/FSB4格式的样本头
                        writer.Write(sample.Length);
                        writer.Write(sample.Offset);
                        
                        ushort flags2 = (ushort)(
                            (int)sample.Format |
                            ((sample.Channels - 1) << 5)
                        );
                        
                        writer.Write(flags2);
                        writer.Write(sample.SampleRate);
                        writer.Write(sample.LoopStart);
                        writer.Write(sample.LoopEnd);
                        
                        if (version == FSBVersion.FSB4)
                        {
                            writer.Write((uint)0); // 额外标志，默认为0
                        }
                        break;
                }

                currentOffset += sample.Length;
            }

            // 写入名称表
            foreach (var sample in samples)
            {
                byte[] nameBytes = Encoding.UTF8.GetBytes(sample.Name);
                writer.Write(nameBytes);
                writer.Write((byte)0); // 结尾的null字符
            }

            // 写入音频数据
            for (int i = 0; i < samples.Count; i++)
            {
                writer.Write(audioData[i]);
            }
        }

        // 获取频率索引
        private uint GetFrequencyIndex(uint sampleRate)
        {
            // FSB5中的采样率索引表
            uint[] sampleRates = new uint[]
            {
                4000, 8000, 11025, 12000, 16000, 22050, 24000, 32000,
                44100, 48000, 96000, 192000, 384000
            };

            for (uint i = 0; i < sampleRates.Length; i++)
            {
                if (sampleRate == sampleRates[i])
                {
                    return i;
                }
            }

            // 如果找不到匹配的采样率，返回44100Hz的索引
            return 8; // 44100Hz的索引
        }

        #endregion
    }
}