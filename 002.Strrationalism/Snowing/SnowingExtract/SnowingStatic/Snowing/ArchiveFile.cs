﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
namespace Snowing
{
    /// <summary>
    /// 资源文件相关
    /// </summary>
    public class ArchiveFile
    {
        /// <summary>
        /// 获取或设置导出主路径文件夹
        /// </summary>
        public string ExtractOutputDir { get; set; }
        /// <summary>
        /// 获取或设置Aes128Key
        /// </summary>
        public byte[] Aes128Key { get; set; }
        /// <summary>
        /// 获取或设置Aes128IV
        /// </summary>
        public byte[] Aes128IV { get; set; }

        /// <summary>
        /// 提取资源
        /// </summary>
        /// <param name="upperDirName">上级文件夹</param>
        /// <param name="directoryInfo">文件夹信息</param>
        public void Extract(string upperDirName,DirectoryInfo directoryInfo)
        {
            //设置导出路径
            string extractDir = string.Concat(this.ExtractOutputDir, upperDirName, directoryInfo.Name, "/");
            //如果不存在则创建文件
            if (Directory.Exists(extractDir) == false)
            {
                Directory.CreateDirectory(extractDir);
            }

            //获取子文件
            List<FileInfo> archiveFiles = directoryInfo.EnumerateFiles().ToList();

            //循环解包
            archiveFiles.ForEach(archiveFile =>
            {
                
                if (archiveFile.Extension == ".ctx")
                {
                    Archive.DataInfo textureData = new Archive.DataInfo();

                    //读取图像资源
                    textureData.Data = File.ReadAllBytes(archiveFile.FullName);
                    textureData.FileName = archiveFile.Name;

                    //解密并转换文件
                    textureData = TextureArchive.ConvertTexture(textureData, this.Aes128Key, this.Aes128IV);

                    //打印Log
                    if (SystemConfig.ConsoleLogEnable)
                    {
                        Console.WriteLine(string.Concat(archiveFile.FullName, "    解密成功"));
                    }

                    //写入文件
                    File.WriteAllBytes(string.Concat(extractDir, textureData.FileName), textureData.Data);
                }
                else if (archiveFile.Extension == ".ykm" || archiveFile.Extension == ".json"|| archiveFile.Extension == ".moc3")
                {
                    Archive.DataInfo textData = new Archive.DataInfo();

                    //读取文本资源
                    textData.Data = File.ReadAllBytes(archiveFile.FullName);
                    textData.FileName = archiveFile.Name;

                    //解密文件
                    textData = ScenarioArchive.Decrypt(textData, this.Aes128Key, this.Aes128IV);

                    //打印Log
                    if (SystemConfig.ConsoleLogEnable)
                    {
                        Console.WriteLine(string.Concat(archiveFile.FullName, "    解密成功"));
                    }

                    //写入文件
                    File.WriteAllBytes(string.Concat(extractDir, textData.FileName), textData.Data);
                }
                else if (archiveFile.Extension == ".voc" || archiveFile.Extension == ".cv")
                {
                    Archive.DataInfo soundData = new Archive.DataInfo();

                    //读取音频资源
                    soundData.Data = File.ReadAllBytes(archiveFile.FullName);
                    soundData.FileName = string.Concat(archiveFile.Name.Split('.').ElementAt(0), ".ogg");

                    //解密文件
                    soundData = ScenarioArchive.Decrypt(soundData, this.Aes128Key, this.Aes128IV);

                    //打印Log
                    if (SystemConfig.ConsoleLogEnable)
                    {
                        Console.WriteLine(string.Concat(archiveFile.FullName, "    解密成功"));
                    }

                    //写入文件
                    File.WriteAllBytes(string.Concat(extractDir, soundData.FileName), soundData.Data);
                }

            });

            //获取子文件夹
            List<DirectoryInfo> subDirs = directoryInfo.EnumerateDirectories().ToList();

            //循环递归
            subDirs.ForEach(subdir =>
            {
                this.Extract(string.Concat(upperDirName,directoryInfo.Name, "/"), subdir);
            });

        } 

    }
}
