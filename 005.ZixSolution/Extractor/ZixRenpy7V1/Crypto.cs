﻿using System;
using System.Buffers;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Extractor.ZixRenpy7V1.Crypto
{
    public class Crypto128
    {
        /// <summary>
        /// 解密信息
        /// </summary>
        public struct DecryptInfo
        {
            /// <summary>
            /// 解密长度
            /// </summary>
            public int DecryptLength;
            /// <summary>
            /// 存放生成的Key表
            /// </summary>
            public byte[] DecryptTable;
            /// <summary>
            /// Key表长度
            /// </summary>
            public int DecryptTableLength;
            /// <summary>
            /// 轮解密次数
            /// </summary>
            public int DecryptRound;
            /// <summary>
            /// 表块起始点(块大小)
            /// </summary>
            public int StartBlock;
        }

        public bool IsInitialized { get; set; }

        private byte[] mKey;
        private DecryptInfo mDecryptInfo;

        private byte[] mSubstitutionBox1;
        private byte[] mSubstitutionBox2;
        private byte[] mSubstitutionBox3;
        private byte[] mSubstitutionBox4;
        private byte[] mSubstitutionBox5;
        private byte[] mSubstitutionBox6;
        private byte[] mSubstitutionBox7;
        private byte[] mSubstitutionBox8;

        private byte[] mXorVector;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="key">解密Key</param>
        /// <param name="xorVector">异或向量</param>
        public Crypto128(byte[] key, byte[] xorVector)
        {
            mKey = new byte[16];
            key.CopyTo(mKey, 0);
            mXorVector = xorVector;
        }

        /// <summary>
        /// 初始化Key表与解密S盒
        /// </summary>
        /// <param name="substitutionBox1">S盒1</param>
        /// <param name="substitutionBox2">S盒2</param>
        /// <param name="substitutionBox3">S盒3</param>
        /// <param name="substitutionBox4">S盒4</param>
        /// <param name="substitutionBox5">S盒5</param>
        /// <param name="substitutionBox6">S盒6</param>
        /// <param name="substitutionBox7">S盒7</param>
        /// <param name="substitutionBox8">S盒8</param>
        public void Initialize(byte[] substitutionBox1, byte[] substitutionBox2, byte[] substitutionBox3,
                               byte[] substitutionBox4, byte[] substitutionBox5, byte[] substitutionBox6,
                               byte[] substitutionBox7, byte[] substitutionBox8)
        {

            mSubstitutionBox1 = substitutionBox1;
            mSubstitutionBox2 = substitutionBox2;
            mSubstitutionBox3 = substitutionBox3;
            mSubstitutionBox4 = substitutionBox4;
            mSubstitutionBox5 = substitutionBox5;
            mSubstitutionBox6 = substitutionBox6;
            mSubstitutionBox7 = substitutionBox7;
            mSubstitutionBox8 = substitutionBox8;

            //Key初始化(解密)
            for (int index = 0; index < 16; index++)
            {
                mKey[index] ^= (byte)(39 - index);
            }

            //解密信息初始化
            mDecryptInfo = new();
            mDecryptInfo.DecryptLength = 16;   //解密长度
            mDecryptInfo.StartBlock = mDecryptInfo.DecryptLength / 4;     //设置当前块位置  (一个块为4字节)
            mDecryptInfo.DecryptRound = mDecryptInfo.StartBlock + 7;   //设置解密轮数
            mDecryptInfo.DecryptTableLength = mDecryptInfo.DecryptRound * 16; //设置解密表长度

            mDecryptInfo.DecryptTable = new byte[mDecryptInfo.DecryptTableLength];

            //复制key进解密表
            mKey.CopyTo(mDecryptInfo.DecryptTable, 0);

            //解密信息初始化完成生成key表
            CreateKeyTable();

            IsInitialized = true;
        }

        /// <summary>
        /// 生成Key表
        /// </summary>
        /// <remarks>使用S盒8</remarks>
        private void CreateKeyTable()
        {
            int blockIndex = mDecryptInfo.StartBlock; //当前块位置
            int blockSize = mDecryptInfo.StartBlock;  //块大小
            int maxBlockIndex = mDecryptInfo.DecryptRound * 4;     //最大块大小

            //最后生成的key
            Span<byte> lastKeyBytes = stackalloc byte[4];
            //循环生成
            while (maxBlockIndex > blockIndex)
            {
                uint lastKey = BitConverter.ToUInt32(mDecryptInfo.DecryptTable, (blockIndex - 1) * 4); //获取上一次最后4字节作为key

                lastKey = BitOperations.RotateRight(lastKey, 8);   //循环右移

                BitConverter.TryWriteBytes(lastKeyBytes, lastKey);      //回写栈缓存(最后一次key)

                //查表取S盒
                for (int index = 0; index < 4; index++)
                {
                    lastKeyBytes[index] = mSubstitutionBox8[lastKeyBytes[index]];
                }

                //异或向量
                lastKeyBytes[0] ^= mXorVector[blockIndex / 4 - 1];

                //每4块生成key表
                for (int blockLoop = 0; blockLoop < blockSize; blockLoop++)
                {
                    //生成Key表(4*4字节)
                    for (int index = 0; index < 4; index++)
                    {
                        mDecryptInfo.DecryptTable[blockIndex * 4 + index] = (byte)(lastKeyBytes[index] ^ mDecryptInfo.DecryptTable[(blockIndex - 4) * 4 + index]);
                    }

                    blockIndex++;       //块索引自增
                    //检查块是否超过最大数量
                    if (maxBlockIndex <= blockIndex)
                    {
                        break;
                    }

                    lastKey = BitConverter.ToUInt32(mDecryptInfo.DecryptTable, (blockIndex - 1) * 4); //获取上一次最后4字节作为key
                    BitConverter.TryWriteBytes(lastKeyBytes, lastKey);  //回写栈缓存(最后一次key)
                }
            }
        }

        /// <summary>
        /// 变换16字节
        /// </summary>
        /// <param name="data">数据</param>
        public bool Transform16Bytes(Span<byte> data)
        {
            //检查数据有效性
            if (data == null || data.Length != 16)
            {
                return false;
            }

            //暂存解密结果
            Span<byte> temp = stackalloc byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            //解密
            for (int index = 0; index < 16; index++)
            {
                temp[index] = data[(4 * (16 - index) + index) % 16];
            }
            //回写覆盖原数据
            temp.CopyTo(data);

            return true;
        }
        /// <summary>
        /// 变换1字节
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="order">序号</param>
        /// <returns>表数据</returns>
        /// <remarks>使用S盒1-6</remarks>
        public byte Transform1Byte(byte data, int order)
        {
            //跳转取key
            switch (order)
            {
                case 0:
                case 1:
                    return data;
                case 2:
                    return mSubstitutionBox6[data];
                case 3:
                    return mSubstitutionBox5[data];
                case 4:
                case 5:
                case 6:
                case 7:
                case 8:
                    return data;
                case 9:
                    return mSubstitutionBox4[data];
                case 10:
                    return data;
                case 11:
                    return mSubstitutionBox3[data];
                case 12:
                    return data;
                case 13:
                    return mSubstitutionBox2[data];
                case 14:
                    return mSubstitutionBox1[data];
                default:
                    return data;
            }
        }
        /// <summary>
        /// 缓缓4字节数据
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="offset">偏移</param>
        public void Transform4Bytes(Span<byte> data, int offset)
        {
            //暂存解密结果
            Span<byte> temp8 = stackalloc byte[4] { 0, 0, 0, 0 };
            //跳转表序号
            Span<int> orderList = stackalloc int[4] { 0xE, 0xB, 0xD, 0x9 };

            //待写入目标地址   4字节一组
            Span<byte> destData = data.Slice(offset, 4);

            //解密
            for (int index = 0; index < 4; index++)
            {
                temp8[index] = (byte)(Transform1Byte(destData[0], orderList[(4 - index + 0) % 4]) ^
                                      Transform1Byte(destData[1], orderList[(4 - index + 1) % 4]) ^
                                      Transform1Byte(destData[2], orderList[(4 - index + 2) % 4]) ^
                                      Transform1Byte(destData[3], orderList[(4 - index + 3) % 4]));
            }
            //回写覆盖原数据
            temp8.CopyTo(destData);
        }
        /// <summary>
        /// 解密16字节数据
        /// </summary>
        /// <param name="data">数据</param>
        /// <returns></returns>
        /// <remarks>使用S盒7</remarks>
        public bool Decrypt16BytesData(Span<byte> data)
        {
            //数据检查
            if (data == null || data.Length != 16)
            {
                return false;
            }

            //获取轮解密次数
            int round = mDecryptInfo.DecryptRound;
            //获取解密表
            byte[] key = mDecryptInfo.DecryptTable;


            //第一轮解密
            for (int index = 0; index < 16; index++)
            {
                data[index] ^= key[(round - 1) * 16 + index];
            }

            //第二轮解密
            round -= 2;
            while (round >= 0)
            {
                //16字节解密1
                Transform16Bytes(data);
                //取S盒表
                for (int index = 0; index < 16; index++)
                {
                    data[index] = mSubstitutionBox7[data[index]];
                }
                //2-1轮解密
                for (int index = 0; index < 16; index++)
                {
                    data[index] ^= key[round * 16 + index];
                }

                //最后一次解密不执行4*4字节解密操作
                if (round == 0)
                {
                    break;
                }

                //2-2解密4*4字节解密
                for (int index = 0; index < 16; index += 4)
                {
                    Transform4Bytes(data, index);
                }
                round--;    //轮解密循环-1
            }
            return true;
        }

        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="path">文件路径(全路径)</param>
        /// <param name="extractpath">导出路径(全路径)</param>
        /// <returns></returns>
        public bool Decrypt(string path, string extractpath)
        {
            byte[] buffer = File.ReadAllBytes(path);

            //16字节对齐
            if (buffer.Length % 16 != 0)
            {
                return false;
            }

            Span<byte> data = buffer.AsSpan();
            int dataLen = data.Length;
            //每16字节解密
            for (int pos = 0; pos < dataLen; pos += 16)
            {
                this.Decrypt16BytesData(data.Slice(pos, 16));
            }

            //移除对齐部分 PKCS7
            int alignSize = data[dataLen - 1];
            dataLen -= alignSize;

            {
                string dir = Path.GetDirectoryName(extractpath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }

            using FileStream mFs = new(extractpath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            mFs.Write(data.Slice(0, dataLen));
            mFs.Flush();

            return true;
        }
    }
}
