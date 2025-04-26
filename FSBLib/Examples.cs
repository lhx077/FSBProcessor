using System;
using System.Collections.Generic;
using System.IO;

namespace FSBLib.Examples
{
    /// <summary>
    /// FSBLib库的使用示例
    /// </summary>
    public static class Examples
    {
        /// <summary>
        /// 解析FSB文件示例
        /// </summary>
        /// <param name="fsbFilePath">FSB文件路径</param>
        public static void ParseFSBExample(string fsbFilePath)
        {
            try
            {
                Console.WriteLine($"解析FSB文件: {fsbFilePath}");
                
                // 创建FSBProcessor实例
                var processor = new FSBProcessor();
                
                // 解析FSB文件，获取音频信息
                var samples = processor.ParseFSB(fsbFilePath);
                
                // 显示音频信息
                Console.WriteLine($"找到 {samples.Count} 个音频样本:");
                foreach (var sample in samples)
                {
                    Console.WriteLine($"名称: {sample.Name}");
                    Console.WriteLine($"格式: {sample.Format}");
                    Console.WriteLine($"通道数: {sample.Channels}");
                    Console.WriteLine($"采样率: {sample.SampleRate} Hz");
                    Console.WriteLine($"长度: {sample.Length} 字节");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析FSB文件时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 将FSB文件解码为WAV格式示例
        /// </summary>
        /// <param name="fsbFilePath">FSB文件路径</param>
        /// <param name="outputDirectory">输出目录</param>
        public static void ExtractToWavExample(string fsbFilePath, string outputDirectory)
        {
            try
            {
                Console.WriteLine($"将FSB文件解码为WAV: {fsbFilePath}");
                
                // 创建FSBProcessor实例
                var processor = new FSBProcessor();
                
                // 确保输出目录存在
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }
                
                // 将FSB文件解码为WAV文件
                var outputFiles = processor.ExtractToWav(fsbFilePath, outputDirectory);
                
                // 显示输出文件路径
                Console.WriteLine($"已解码 {outputFiles.Count} 个文件:");
                foreach (var file in outputFiles)
                {
                    Console.WriteLine(file);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解码FSB文件时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 将普通音频文件打包为FSB格式示例
        /// </summary>
        /// <param name="audioFilePaths">音频文件路径列表</param>
        /// <param name="outputFsbPath">输出FSB文件路径</param>
        /// <param name="version">FSB版本</param>
        public static void PackToFSBExample(List<string> audioFilePaths, string outputFsbPath, FSBProcessor.FSBVersion version = FSBProcessor.FSBVersion.FSB5)
        {
            try
            {
                Console.WriteLine($"将音频文件打包为FSB: {outputFsbPath}");
                
                // 创建FSBProcessor实例
                var processor = new FSBProcessor();
                
                // 显示要打包的文件
                Console.WriteLine("要打包的音频文件:");
                foreach (var file in audioFilePaths)
                {
                    Console.WriteLine(file);
                }
                
                // 将音频文件打包为FSB文件
                processor.PackToFSB(audioFilePaths, outputFsbPath, version);
                
                Console.WriteLine($"打包完成: {outputFsbPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"打包FSB文件时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 运行所有示例的主方法
        /// </summary>
        public static void RunAllExamples()
        {
            // 示例FSB文件路径
            string fsbFilePath = "example.fsb";
            
            // 示例输出目录
            string outputDirectory = "output";
            
            // 示例音频文件
            var audioFiles = new List<string>
            {
                "audio1.wav",
                "audio2.mp3"
            };
            
            // 示例输出FSB文件
            string outputFsbPath = "packed.fsb";
            
            // 注意：这些示例需要实际的文件才能运行
            // 这里只是展示API的使用方法
            
            Console.WriteLine("FSBLib 使用示例");
            Console.WriteLine("=================\n");
            
            // 检查文件是否存在
            if (File.Exists(fsbFilePath))
            {
                // 运行解析示例
                Console.WriteLine("\n1. 解析FSB文件示例\n");
                ParseFSBExample(fsbFilePath);
                
                // 运行解码示例
                Console.WriteLine("\n2. 解码FSB文件示例\n");
                ExtractToWavExample(fsbFilePath, outputDirectory);
            }
            else
            {
                Console.WriteLine($"示例FSB文件不存在: {fsbFilePath}");
            }
            
            // 检查音频文件是否存在
            bool allFilesExist = true;
            foreach (var file in audioFiles)
            {
                if (!File.Exists(file))
                {
                    allFilesExist = false;
                    Console.WriteLine($"示例音频文件不存在: {file}");
                }
            }
            
            if (allFilesExist)
            {
                // 运行打包示例
                Console.WriteLine("\n3. 打包FSB文件示例\n");
                PackToFSBExample(audioFiles, outputFsbPath);
            }
            
            Console.WriteLine("\n示例运行完成");
        }
    }
}