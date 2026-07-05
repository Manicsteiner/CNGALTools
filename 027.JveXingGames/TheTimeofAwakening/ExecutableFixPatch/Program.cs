using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace ExecutableFixPatch
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            using OpenFileDialog ofd = new()
            {
                AddExtension = true,
                AutoUpgradeEnabled = true,
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = ".exe",
                Filter = "可执行文件(*.exe)|*.exe",
                Multiselect = false,
                RestoreDirectory = true,
                ShowHelp = false,
                Title = "觉醒之刻 - 补丁",
            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                List<FilePatch> patchVerList = new()
                {
                    new TheTimeofAwakening_Exe_V20191211(),
                };

                FileInfo fileInfo = new(ofd.FileName);
                foreach(FilePatch filePatch in patchVerList)
                {
                    if(fileInfo.Length == filePatch.Size)
                    {
                        using FileStream inFs = fileInfo.OpenRead();
                        byte[] data = new byte[inFs.Length];
                        inFs.Read(data);

                        byte[] md5 = MD5.HashData(data);
                        if (md5.SequenceEqual(filePatch.MD5))
                        {
                            foreach(PatchEntry entry in filePatch.Patches)
                            {
                                Array.Copy(entry.Bytes, 0L, data, entry.Offset, entry.Bytes.LongLength);
                            }

                            string outputPath = Path.Combine(fileInfo.DirectoryName!, Path.GetFileNameWithoutExtension(fileInfo.Name) + "_Patch.exe");
                            using FileStream outFs = File.Create(outputPath);
                            outFs.Write(data);
                            outFs.Flush();

                            Console.WriteLine($"已修补: {fileInfo.Name}");
                            Console.WriteLine($"输出到: {outputPath}");
                            break;
                        }
                    }
                }
            }

            Console.WriteLine("\r\n============ 请按任意键退出 =============\r\n");
            Console.Read();
        }
    }
}