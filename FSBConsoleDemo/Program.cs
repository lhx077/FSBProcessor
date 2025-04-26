using System;
using System.Collections.Generic;
using System.IO;
using FSBLib;
using FSBLib.Examples;

namespace FSBConsoleDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("FSB处理器 - 命令行工具");
            Console.WriteLine("====================\n");

            if (args.Length == 0)
            {
                ShowHelp();
                return;
            }

            string command = args[0].ToLower();

            try
            {
                switch (command)
                {
                    case "info":
                    case "parse":
                        if (args.Length < 2)
                        {
                            Console.WriteLine("错误: 缺少FSB文件路径");
                            ShowCommandHelp(command);
                            return;
                        }
                        ParseFSB(args[1]);
                        break;

                    case "extract":
                    case "decode":
                        if (args.Length < 2)
                        {
                            Console.WriteLine("错误: 缺少FSB文件路径");
                            ShowCommandHelp(command);
                            return;
                        }
                        string outputDir = args.Length > 2 ? args[2] : "output";
                        ExtractFSB(args[1], outputDir);
                        break;

                    case "pack":
                    case "encode":
                        if (args.Length < 3)
                        {
                            Console.WriteLine("错误: 参数不足");
                            ShowCommandHelp(command);
                            return;
                        }
                        string outputFsb = args[1];
                        List<string> inputFiles = new List<string>();
                        for (int i = 2; i < args.Length; i++)
                        {
                            inputFiles.Add(args[i]);
                        }
                        PackFSB(outputFsb, inputFiles);
                        break;

                    case "examples":
                        RunExamples();
                        break;

                    case "help":
                        if (args.Length > 1)
                        {
                            ShowCommandHelp(args[1].ToLower());
                        }
                        else
                        {
                            ShowHelp();
                        }
                        break;

                    default:
                        Console.WriteLine($"错误: 未知命令 '{command}'");
                        ShowHelp();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine("用法: FSBConsoleDemo <命令> [参数...]");
            Console.WriteLine("\n可用命令:");
            Console.WriteLine("  info <fsb文件>              - 显示FSB文件信息");
            Console.WriteLine("  extract <fsb文件> [输出目录] - 将FSB文件解码为WAV文件");
            Console.WriteLine("  pack <输出fsb> <音频文件...> - 将音频文件打包为FSB文件");
            Console.WriteLine("  examples                    - 运行示例代码");
            Console.WriteLine("  help [命令]                 - 显示帮助信息");
            Console.WriteLine("\n别名:");
            Console.WriteLine("  parse   - 等同于 info");
            Console.WriteLine("  decode  - 等同于 extract");
            Console.WriteLine("  encode  - 等同于 pack");
        }

        static void ShowCommandHelp(string command)
        {
            switch (command)
            {
                case "info":
                case "parse":
                    Console.WriteLine("用法: FSBConsoleDemo info <fsb文件>");
                    Console.WriteLine("描述: 解析并显示FSB文件中包含的音频信息");
                    break;

                case "extract":
                case "decode":
                    Console.WriteLine("用法: FSBConsoleDemo extract <fsb文件> [输出目录]");
                    Console.WriteLine("描述: 将FSB文件中的音频解码为WAV文件");
                    Console.WriteLine("参数:");
                    Console.WriteLine("  输出目录 - 可选，默认为'output'");
                    break;

                case "pack":
                case "encode":
                    Console.WriteLine("用法: FSBConsoleDemo pack <输出fsb> <音频文件...>");
                    Console.WriteLine("描述: 将一个或多个音频文件打包为FSB文件");
                    Console.WriteLine("参数:");
                    Console.WriteLine("  输出fsb    - 要创建的FSB文件路径");
                    Console.WriteLine("  音频文件... - 要打包的一个或多个音频文件");
                    break;

                case "examples":
                    Console.WriteLine("用法: FSBConsoleDemo examples");
                    Console.WriteLine("描述: 运行FSBLib库的示例代码");
                    break;

                default:
                    ShowHelp();
                    break;
            }
        }

        static void ParseFSB(string fsbFilePath)
        {
            if (!File.Exists(fsbFilePath))
            {
                Console.WriteLine($"错误: 文件不存在 '{fsbFilePath}'");
                return;
            }

            Console.WriteLine($"解析FSB文件: {fsbFilePath}");
            var processor = new FSBProcessor();
            var samples = processor.ParseFSB(fsbFilePath);

            Console.WriteLine($"\n找到 {samples.Count} 个音频样本:");
            for (int i = 0; i < samples.Count; i++)
            {
                var sample = samples[i];
                Console.WriteLine($"\n样本 #{i+1}:");
                Console.WriteLine($"  名称: {sample.Name}");
                Console.WriteLine($"  格式: {sample.Format}");
                Console.WriteLine($"  通道数: {sample.Channels}");
                Console.WriteLine($"  采样率: {sample.SampleRate} Hz");
                Console.WriteLine($"  长度: {sample.Length} 字节");
                if (sample.LoopStart != 0 || sample.LoopEnd != 0)
                {
                    Console.WriteLine($"  循环点: {sample.LoopStart} - {sample.LoopEnd}");
                }
            }
        }

        static void ExtractFSB(string fsbFilePath, string outputDirectory)
        {
            if (!File.Exists(fsbFilePath))
            {
                Console.WriteLine($"错误: 文件不存在 '{fsbFilePath}'");
                return;
            }

            Console.WriteLine($"将FSB文件解码为WAV: {fsbFilePath}");
            Console.WriteLine($"输出目录: {outputDirectory}");

            var processor = new FSBProcessor();
            var outputFiles = processor.ExtractToWav(fsbFilePath, outputDirectory);

            Console.WriteLine($"\n已成功解码 {outputFiles.Count} 个文件:");
            foreach (var file in outputFiles)
            {
                Console.WriteLine($"  {file}");
            }
        }

        static void PackFSB(string outputFsbPath, List<string> audioFilePaths)
        {
            // 检查所有输入文件是否存在
            foreach (var filePath in audioFilePaths)
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"错误: 文件不存在 '{filePath}'");
                    return;
                }
            }

            Console.WriteLine($"将 {audioFilePaths.Count} 个音频文件打包为FSB: {outputFsbPath}");
            Console.WriteLine("输入文件:");
            foreach (var file in audioFilePaths)
            {
                Console.WriteLine($"  {file}");
            }

            var processor = new FSBProcessor();
            processor.PackToFSB(audioFilePaths, outputFsbPath);

            Console.WriteLine($"\n打包完成: {outputFsbPath}");
        }

        static void RunExamples()
        {
            Console.WriteLine("运行FSBLib库示例...");
            Examples.RunAllExamples();
        }
    }
}