using System;
using System.Collections.Generic;

namespace ExecutableFixPatch
{
    /// <summary>
    /// 补丁项
    /// </summary>
    internal class PatchEntry
    {
        private readonly long mOffset;
        private readonly byte[] mBytes;

        /// <summary>
        /// 偏移
        /// </summary>
        public long Offset => this.mOffset;
        /// <summary>
        /// 补丁数据
        /// </summary>
        public byte[] Bytes => this.mBytes;

        public PatchEntry(long offset, byte[] bytes)
        {
            this.mOffset = offset;
            this.mBytes = bytes;
        }
    }

    /// <summary>
    /// 文件补丁
    /// </summary>
    internal abstract class FilePatch
    {
        /// <summary>
        /// 文件大小
        /// </summary>
        public abstract long Size { get; }
        /// <summary>
        /// MD5值
        /// </summary>
        public abstract byte[] MD5 { get; }
        /// <summary>
        /// 补丁列表
        /// </summary>
        public abstract List<PatchEntry> Patches { get; }
    }

    /// <summary>
    /// Steam 2019-12-11
    /// </summary>
    internal class TheTimeofAwakening_Exe_V20191211 : FilePatch
    {
        public override long Size { get; } = 0x3064000L;
        public override byte[] MD5 { get; } = new byte[] 
        { 
            0xDF, 0x84, 0x86, 0x45, 0x63, 0x00, 0xE6, 0x3E, 0x60, 0x6C, 0x45, 0x91, 0xFA, 0xE1, 0xE8, 0x1E
        };
        public override List<PatchEntry> Patches { get; } = new List<PatchEntry>()
        {
            //SHA1::HashData_SHA1SIMD 有bug, 回退到SHA1::HashData_AVX2
            //jne offset32 -> nop
            new PatchEntry(0x1803BL, new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 }),
        };
    }
}
