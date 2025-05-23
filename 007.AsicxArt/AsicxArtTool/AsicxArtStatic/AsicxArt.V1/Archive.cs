﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace AsicxArt.V1
{
    /// <summary>
    /// 资源类型
    /// </summary>
    public enum ArchiveTypeV1
    {
        /// <summary>
        /// CG
        /// </summary>
        Gallery = 0,
        /// <summary>
        /// L2D贴图
        /// </summary>
        Live2DTexture = 1
    }

    public class ArchiveV1
    {
        private readonly byte[] mKey;        //游戏数据库key

        /// <summary>
        /// 提取资源
        /// </summary>
        /// <param name="resDirPath">资源路径</param>
        public void Extract(string resDirPath)
        {
            this.Extract(resDirPath, ArchiveTypeV1.Gallery);
            this.Extract(resDirPath, ArchiveTypeV1.Live2DTexture);
        }

        /// <summary>
        /// 提取资源
        /// </summary>
        /// <param name="resDirPath">资源路径</param>
        /// <param name="arcType">资源类型</param>
        public void Extract(string resDirPath, ArchiveTypeV1 arcType)
        {
            List<string> dbNameList = this.GetDBNameList(arcType);
            List<string> tableNameList = this.GetTableNameList(arcType);

            //遍历数据库
            foreach (string dbName in dbNameList)
            {
                string dbPath = Path.Combine(resDirPath, dbName);
                //检查文件存在
                if (File.Exists(dbPath))
                {
                    //打开数据库
                    IntPtr hDB = SQLite3Command.OpenDBWithKey(dbPath, SQLite3.SQLiteOpenFlags.ReadOnly, this.mKey);
                    //遍历表
                    foreach (string tableName in tableNameList)
                    {
                        int rowCount = SQLite3Command.GetTableItemCount(hDB, tableName);  //获得表项数

                        if (rowCount == -1)
                        {
                            continue;
                        }

                        //导出目录
                        string outDirPath = Path.Combine(resDirPath, "Extract_Static", Path.GetFileNameWithoutExtension(dbName));

                        //遍历id
                        for (int id = 0; id < rowCount; id++)
                        {
                            //准备sql执行语句
                            string sql = $"select * from {tableName} where id={id}";

                            IntPtr statementPtr = SQLite3.Prepare2(hDB, sql);
                            //执行
                            SQLite3.Step(statementPtr);

                            //获取文件全路径
                            string extractFileFullPath = Path.Combine(outDirPath, this.GetResourceRelativePath(statementPtr, arcType));
                            //获取资源数据
                            byte[] data = this.GetResourceData(statementPtr, arcType);

                            //释放
                            SQLite3.Finalize(statementPtr);

                            {
                                if(Path.GetDirectoryName(extractFileFullPath) is string dir && !Directory.Exists(dir))
                                {
                                    Directory.CreateDirectory(dir);
                                }
                            }
                            File.WriteAllBytes(extractFileFullPath, data);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 获取资源对应存放的数据库文件名
        /// </summary>
        /// <param name="arcType">资源类型</param>
        /// <returns></returns>
        private List<string> GetDBNameList(ArchiveTypeV1 arcType)
        {
            List<string> dbNameList = new(32);
            switch (arcType)
            {
                case ArchiveTypeV1.Gallery:
                {
                    dbNameList.Add("rsinfo.db");
                    break;
                }
                case ArchiveTypeV1.Live2DTexture:
                {
                    dbNameList.Add("rsinfo2.db");
                    break;
                }
            }
            return dbNameList;
        }

        /// <summary>
        /// 获取资源对应存放的数据库表名
        /// </summary>
        /// <param name="arcType">资源类型</param>
        /// <returns></returns>
        private List<string> GetTableNameList(ArchiveTypeV1 arcType)
        {
            List<string> tableNameList = new(32);
            switch (arcType)
            {
                case ArchiveTypeV1.Gallery:
                {
                    tableNameList.Add("RsBGInfo");
                    tableNameList.Add("RsCGInfo");
                    break;
                }
                case ArchiveTypeV1.Live2DTexture:
                {
                    tableNameList.Add("RsLive2DInfo");
                    break;
                }
            }
            return tableNameList;
        }

        /// <summary>
        /// 获取资源相对路径
        /// </summary>
        /// <param name="statementPtr">sqlite二进制执行语句</param>
        /// <param name="arcType">资源类型</param>
        /// <returns></returns>
        private string GetResourceRelativePath(IntPtr statementPtr, ArchiveTypeV1 arcType)
        {
            string filePath = string.Empty;
            switch (arcType)
            {
                case ArchiveTypeV1.Gallery:
                {
                    filePath = SQLite3.GetColumnString(statementPtr, 1);   //Name=1
                    filePath += ".png";
                    break;
                } 
                case ArchiveTypeV1.Live2DTexture:
                {
                    filePath = SQLite3.GetColumnString(statementPtr, 1);   //Path=1
                    filePath += SQLite3.GetColumnString(statementPtr, 2);  //TextureName=2
                    filePath += ".png";
                    break;
                }
            }
            return filePath;
        }

        /// <summary>
        /// 获取资源数据
        /// </summary>
        /// <param name="statementPtr">sqlite二进制执行语句</param>
        /// <param name="arcType">资源类型</param>
        /// <returns></returns>
        private byte[] GetResourceData(IntPtr statementPtr, ArchiveTypeV1 arcType)
        {
            byte[] data = Array.Empty<byte>();
            switch (arcType)
            {
                case ArchiveTypeV1.Gallery:
                {
                    data = SQLite3.GetColumnByteArray(statementPtr, 4);   //blob=4
                    break;
                }
                case ArchiveTypeV1.Live2DTexture:
                {
                    data = SQLite3.GetColumnByteArray(statementPtr, 5);   //blob=5
                    break;
                }
            }
            return data;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="key">解密Key</param>
        public ArchiveV1(byte[] key)
        {
            this.mKey = key;
        }
    }
}
