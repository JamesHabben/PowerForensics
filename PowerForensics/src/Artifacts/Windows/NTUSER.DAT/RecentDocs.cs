﻿using System;
using System.Text;
using System.Collections.Generic;
using PowerForensics.Registry;

namespace PowerForensics.Artifacts
{
    public class RecentDocs
    {
        #region Properties

        public readonly string User;
        public readonly string Path;
        public readonly DateTime LastWriteTime;

        #endregion Properties

        #region Constructors

        private RecentDocs(string user, string path)
        {
            User = user;
            Path = path;
        }

        private RecentDocs(string user, string path, DateTime lastWriteTime)
        {
            User = user;
            Path = path;
            LastWriteTime = lastWriteTime;
        }

        #endregion Constructors

        #region StaticMethods

        public static RecentDocs[] Get(string hivePath)
        {
            if (RegistryHelper.isCorrectHive(hivePath, "NTUSER.DAT"))
            {
                string user = RegistryHelper.GetUserHiveOwner(hivePath);
                byte[] bytes = RegistryHelper.GetHiveBytes(hivePath);
                string key = @"Software\Microsoft\Windows\CurrentVersion\Explorer\RecentDocs";

                NamedKey RecentDocsKey = NamedKey.Get(bytes, hivePath, @"Software\Microsoft\Windows\CurrentVersion\Explorer\RecentDocs");
                ValueKey MRUListEx = ValueKey.Get(bytes, hivePath, key, "MRUListEx");
                byte[] MRUListBytes = (byte[])MRUListEx.GetData(bytes);
                RecentDocs[] docs = new RecentDocs[MRUListBytes.Length / 4];

                for (int i = 0; i < MRUListBytes.Length - 4; i += 4)
                {
                    if(i == 0)
                    {
                        docs[i / 4] = new RecentDocs(user, Encoding.Unicode.GetString((byte[])ValueKey.Get(bytes, hivePath, key, BitConverter.ToInt32(MRUListBytes, i).ToString()).GetData(bytes)).Split('\0')[0], RecentDocsKey.WriteTime);
                    }
                    else
                    {
                        docs[i / 4] = new RecentDocs(user, Encoding.Unicode.GetString((byte[])ValueKey.Get(bytes, hivePath, key, BitConverter.ToInt32(MRUListBytes, i).ToString()).GetData(bytes)).Split('\0')[0]);
                    }
                }

                return docs;
            }
            else
            {
                throw new Exception("Invalid NTUSER.DAT hive provided to the -HivePath parameter.");
            }
        }

        public static RecentDocs[] GetInstances(string volume)
        {
            Helper.getVolumeName(ref volume);

            List<RecentDocs> list = new List<RecentDocs>();

            foreach (string hivePath in RegistryHelper.GetUserHiveInstances(volume))
            {
                try
                {
                    list.AddRange(Get(hivePath));
                }
                catch
                {

                }
            }

            return list.ToArray();
        }

        #endregion StaticMethods
    }
}
