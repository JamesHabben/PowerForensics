﻿using System;
using System.Text;
using System.Collections.Generic;

namespace PowerForensics.Registry
{
    #region NamedKeyClass
    public class NamedKey : Cell
    {
        #region Enums

        [Flags]
        public enum NAMED_KEY_FLAGS
        {
            VolatileKey = 0x0001,
            MountPoint = 0x0002,
            RootKey = 0x0004,
            Immutable = 0x0008,
            SymbolicLink = 0x0010,
            NameIsASCII = 0x0020,
            PredefinedHandle = 0x0040
        }

        #endregion Enums

        #region Properties

        public readonly string HivePath;
        internal readonly NAMED_KEY_FLAGS Flags;
        public readonly DateTime WriteTime;
        internal readonly uint ParentKeyOffset;
        public readonly uint NumberOfSubKeys;
        public readonly uint NumberOfVolatileSubKeys;
        internal readonly int SubKeysListOffset;
        internal readonly int VolatileSubKeysListOffset;
        public readonly uint NumberOfValues;
        internal readonly int ValuesListOffset;
        internal readonly int SecurityKeyOffset;
        internal readonly int ClassNameOffset;
        internal readonly uint LargestSubKeyNameSize;
        internal readonly uint LargestSubKeyClassNameSize;
        internal readonly uint LargestValueNameSize;
        internal readonly uint LargestValueDataSize;
        internal readonly ushort KeyNameSize;
        internal readonly ushort ClassNameSize;
        public readonly string FullName;
        public readonly string Name;

        #endregion Properties

        #region Constructors

        internal NamedKey(byte[] bytes, string hivePath, string key)
        {
            Signature = Encoding.ASCII.GetString(bytes, 0x04, 0x02);
            
            if (Signature == "nk")
            {
                HivePath = hivePath;

                #region CellHeader

                Size = BitConverter.ToInt32(bytes, 0x00);

                if (Size >= 0)
                {
                    Allocated = false;
                }
                else
                {
                    Allocated = true;
                }

                #endregion CellHeader

                Flags = (NAMED_KEY_FLAGS)BitConverter.ToUInt16(bytes, 0x06);
                WriteTime = DateTime.FromFileTimeUtc(BitConverter.ToInt64(bytes, 0x08));
                ParentKeyOffset = BitConverter.ToUInt32(bytes, 0x14) + RegistryHeader.HBINOFFSET;
                NumberOfSubKeys = BitConverter.ToUInt32(bytes, 0x18);
                NumberOfVolatileSubKeys = BitConverter.ToUInt32(bytes, 0x1C);
                SubKeysListOffset = BitConverter.ToInt32(bytes, 0x20) + RegistryHeader.HBINOFFSET;
                VolatileSubKeysListOffset = BitConverter.ToInt32(bytes, 0x24) + RegistryHeader.HBINOFFSET;
                NumberOfValues = BitConverter.ToUInt32(bytes, 0x28);
                ValuesListOffset = BitConverter.ToInt32(bytes, 0x2C) + RegistryHeader.HBINOFFSET;
                SecurityKeyOffset = BitConverter.ToInt32(bytes, 0x30) + RegistryHeader.HBINOFFSET;
                ClassNameOffset = BitConverter.ToInt32(bytes, 0x34) + RegistryHeader.HBINOFFSET;
                LargestSubKeyNameSize = BitConverter.ToUInt32(bytes, 0x38);
                LargestSubKeyClassNameSize = BitConverter.ToUInt32(bytes, 0x3C);
                LargestValueNameSize = BitConverter.ToUInt32(bytes, 0x40);
                LargestValueDataSize = BitConverter.ToUInt32(bytes, 0x44);
                KeyNameSize = BitConverter.ToUInt16(bytes, 0x4C);
                ClassNameSize = BitConverter.ToUInt16(bytes, 0x4E);

                #region KeyNameString

                if ((0x50 + KeyNameSize) <= bytes.Length)
                {
                    Name = Encoding.ASCII.GetString(bytes, 0x50, Math.Abs(KeyNameSize));
                }

                #endregion KeyNameString

                #region FullName

                string[] hivesplit = hivePath.Split('\\');
                string hive = hivesplit[hivesplit.Length - 1];

                string fullname = null;

                if (!(key.Contains(Name)))
                {
                    fullname = (key + "\\" + Name).TrimStart('\\');
                }
                else
                {
                    fullname = key.TrimStart('\\');
                }

                FullName = fullname.Replace("CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}", hive + ':');

                #endregion FullName
            }
            else
            {
                throw new Exception("Cell is not a valid Named Key");
            }
        }

        #endregion Constructors

        #region StaticMethods

        public static NamedKey Get(string path, string key)
        {
            return NamedKey.Get(RegistryHelper.GetHiveBytes(path), path, key.TrimEnd('\\'));
        }

        internal static NamedKey Get(byte[] bytes, string path, string key)
        {
            NamedKey hiveroot = RegistryHelper.GetRootKey(bytes, path);

            NamedKey nk = hiveroot;

            if (key != null)
            {
                foreach (string k in key.Split('\\'))
                {
                    NamedKey startingkey = nk;
                    foreach (NamedKey n in nk.GetSubKeys(bytes))
                    {
                        if (n.Name.ToUpper() == k.ToUpper())
                        {
                            nk = n;
                        }
                    }
                    if (nk == startingkey)
                    {
                        throw new Exception(string.Format("Cannot find key '{0}' in the '{1}' hive because it does not exist.", key, path));
                    }
                }
                if (nk == hiveroot)
                {
                    throw new Exception(string.Format("Cannot find key '{0}' in the '{1}' hive because it does not exist.", key, path));
                }
            }

            return nk;
        }

        public static NamedKey[] GetInstances(string path, string key)
        {
            if (key == null)
            {
                return NamedKey.GetInstances(RegistryHelper.GetHiveBytes(path), path);
            }
            else
            {
                return NamedKey.GetInstances(RegistryHelper.GetHiveBytes(path), path, key.TrimEnd('\\'));
            }
        }

        internal static NamedKey[] GetInstances(byte[] bytes, string path)
        {
            NamedKey hiveroot = RegistryHelper.GetRootKey(bytes, path);
            return hiveroot.GetSubKeys();
        }

        internal static NamedKey[] GetInstances(byte[] bytes, string path, string key)
        {
            NamedKey hiveroot = RegistryHelper.GetRootKey(bytes, path);

            NamedKey nk = hiveroot;

            if (key != null)
            {
                foreach (string k in key.Split('\\'))
                {
                    NamedKey startingkey = nk;
                    foreach (NamedKey n in nk.GetSubKeys(bytes))
                    {
                        if (n.Name.ToUpper() == k.ToUpper())
                        {
                            nk = n;
                        }
                    }
                    if(nk == startingkey)
                    {
                        throw new Exception(string.Format("Cannot find key '{0}' in the '{1}' hive because it does not exist.", key, path));
                    }
                }
                if (nk == hiveroot)
                {
                    throw new Exception(string.Format("Cannot find key '{0}' in the '{1}' hive because it does not exist.", key, path));
                }
            }

            return nk.GetSubKeys(bytes);
        }

        public static NamedKey[] GetInstancesRecurse(string path)
        {
            byte[] bytes = RegistryHelper.GetHiveBytes(path);

            NamedKey hiveroot = RegistryHelper.GetRootKey(path);

            return GetInstances(bytes, hiveroot, true);
        }

        private static NamedKey[] GetInstances(byte[] bytes, NamedKey nk, bool recurse)
        {
            List<NamedKey> keyList = new List<NamedKey>();

            foreach(NamedKey subkey in nk.GetSubKeys(bytes))
            {
                keyList.Add(subkey);
                
                if (subkey.NumberOfSubKeys > 0)
                {
                    keyList.AddRange(GetInstances(bytes, subkey, true));
                }
            }

            return keyList.ToArray();
        }

        #endregion StaticMethods

        #region InstanceMethods

        public ValueKey[] GetValues()
        {
            if (NumberOfValues > 0)
            {
                byte[] bytes = RegistryHelper.GetHiveBytes(this.HivePath);
                
                return GetValues(bytes);
            }

            throw new Exception(string.Format("The key '{0}' has no associated values", this.FullName));
        }

        internal ValueKey[] GetValues(byte[] bytes)
        {
            if (NumberOfValues > 0)
            {
                ValuesList list = new ValuesList(Helper.GetSubArray(bytes, ValuesListOffset, Math.Abs(BitConverter.ToInt32(bytes, ValuesListOffset))), NumberOfValues);

                ValueKey[] vkArray = new ValueKey[list.Offset.Length];

                for (int i = 0; i < list.Offset.Length; i++)
                {
                    int size = Math.Abs(BitConverter.ToInt32(bytes, (int)list.Offset[i]));
                    vkArray[i] = new ValueKey(Helper.GetSubArray(bytes, (int)list.Offset[i], size), HivePath, Name);
                }

                return vkArray;
            }

            throw new Exception(string.Format("The key '{0}' has no associated values", this.FullName));
        }

        public NamedKey[] GetSubKeys()
        {
            if (NumberOfSubKeys > 0)
            {
                byte[] bytes = RegistryHelper.GetHiveBytes(HivePath);
                return GetSubKeys(bytes);
            }
            else
            {
                return null;
            }
        }

        internal NamedKey[] GetSubKeys(byte[] bytes)
        {
            if (NumberOfSubKeys > 0)
            {
                byte[] subKeyListBytes = Helper.GetSubArray(bytes, SubKeysListOffset, Math.Abs(BitConverter.ToInt32(bytes, this.SubKeysListOffset)));
                string type = Encoding.ASCII.GetString(subKeyListBytes, 0x04, 0x02);

                List list = List.Factory(bytes, subKeyListBytes, type);

                NamedKey[] nkArray = new NamedKey[list.Count];

                for (int i = 0; i < list.Count; i++)
                {
                    int size = Math.Abs(BitConverter.ToInt32(bytes, (int)list.Offset[i]));
                    nkArray[i] = new NamedKey(Helper.GetSubArray(bytes, (int)list.Offset[i], size), HivePath, this.FullName);
                }

                return nkArray;
            }
            else
            {
                return null;
            }
        }

        public SecurityDescriptor GetSecurityKey()
        {
            byte[] bytes = RegistryHelper.GetHiveBytes(this.HivePath);
            return GetSecurityKey(bytes);
        }

        internal SecurityDescriptor GetSecurityKey(byte[] bytes)
        {
            return (new SecurityKey(Helper.GetSubArray(bytes, SecurityKeyOffset, Math.Abs(BitConverter.ToInt32(bytes, SecurityKeyOffset))))).Descriptor;
        }

        #endregion InstanceMethods

        #region OverrideMethods

        public override string ToString()
        {
            return string.Format("Registry key {0} last written to at {1} [{2}]", FullName, WriteTime, NumberOfValues);
        }

        #endregion OverrideMethods
    }

    #endregion NamedKeyClass
}
