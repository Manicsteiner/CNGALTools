using System;
//using System.Collections.Generic;
//using System.Diagnostics;
using System.IO;
//using System.Linq;
//using System.Runtime.CompilerServices;
using EngineCoreStatic;

namespace ConsoleExecute
{
    internal class Program
    {
        unsafe static void Main(string[] args)
        {
            string filePath;
            if (args.Length == 1)
            {
                if (!File.Exists(args[0]))
                {
                    Console.WriteLine("请输入正确的文件位置");
                    // Console.Read();
                    return;
                }
                filePath = args[0];
            }
            else
            {
                Console.WriteLine("请输入文件位置");
                // Console.Read();
                return;
                // gameDir = "D:\\Decripted\\Separate Hearts\\PC";
            }

            //设置你的游戏路径
            string gameDir = Path.GetDirectoryName(filePath) + "\\";
            // string redir = Path.Combine(gameDir, "Re");
            string outDir = Path.Combine(gameDir, "Staric_Extract");

            using HACPackage pkg = new(filePath);
            if (pkg.IsVaild)
            {
                pkg.Extract(outDir);
            }

            /* string[] files = Directory.GetFiles(gameDir, "*.hac", SearchOption.TopDirectoryOnly);
            foreach(string file in files)
            {
                using HACPackage pkg = new(file);
                if (pkg.IsVaild)
                {
                    pkg.Extract(outDir);
                }
            }*/
            Console.WriteLine("提取完成");
            // Console.Read();
        }
    }
}