﻿using System;
using System.Text;
using System.Collections.Generic;

namespace PowerForensics.Registry
{
    //TODO: Determine if Data is Resident or NonResident
    //TODO: Get Data Buffer
    //TODO: Interpret Data based on Data Type
    #region ValueKeyClass

    public class ValueKey : Cell
    {
        #region Enums

        public enum VALUE_KEY_DATA_TYPES
        {
            REG_NONE = 0x00000000,
            REG_SZ = 0x00000001,
            REG_EXPAND_SZ = 0x00000002,
            REG_BINARY = 0x00000003,
            REG_DWORD = 0x00000004,
            REG_DWORD_BIG_ENDIAN = 0x00000005,
            REG_LINK = 0x00000006,
            REG_MULTI_SZ = 0x00000007,
            REG_RESOURCE_LIST = 0x00000008,
            REG_FULL_RESOURCE_DESCRIPTOR = 0x00000009,
            REG_RESOURCE_REQUIREMENTS_LIST = 0x0000000A,
            REG_QWORD = 0x0000000B
        }

        [FlagsAttribute]
        public enum VALUE_KEY_FLAGS
        {
            NameIsUnicode = 0x0000,
            NameIsAscii = 0x0001,
        }

        #endregion Enums

        #region Properties

        public readonly string HivePath;
        public readonly string Key;
        public readonly ushort NameLength;
        public readonly uint DataLength;
        public readonly bool ResidentData;
        public readonly uint DataOffset;
        public readonly VALUE_KEY_DATA_TYPES DataType;
        public readonly VALUE_KEY_FLAGS Flags;
        public readonly string Name;

        #endregion Properties

        #region Constructors

        internal ValueKey(byte[] bytes, string path, string key)
        {
            Signature = Encoding.ASCII.GetString(bytes, 0x04, 0x02);
            
            if (Signature == "vk")
            {
                HivePath = path;
                Key = key;
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
                NameLength = BitConverter.ToUInt16(bytes, 0x06);
                #region DataLength

                uint dataLengthRaw = BitConverter.ToUInt32(bytes, 0x08);

                if (dataLengthRaw > 0x80000000)
                {
                    DataLength = dataLengthRaw - 0x80000000;
                    ResidentData = true;
                }
                else
                {
                    DataLength = dataLengthRaw;
                    ResidentData = false;
                }
                
                #endregion DataLength
                DataOffset = BitConverter.ToUInt32(bytes, 0x0C) + RegistryHeader.HBINOFFSET;
                DataType = (VALUE_KEY_DATA_TYPES)BitConverter.ToUInt32(bytes, 0x10);
                Flags = (VALUE_KEY_FLAGS)BitConverter.ToUInt16(bytes, 0x14);
                #region ValueName

                if (NameLength == 0)
                {
                    Name = "(Default)";
                }
                else
                {
                    if (Flags == VALUE_KEY_FLAGS.NameIsAscii)
                    {
                        Name = Encoding.ASCII.GetString(bytes, 0x18, NameLength);
                    }
                    else
                    {
                        Name = Encoding.Unicode.GetString(bytes, 0x18, NameLength);
                    }
                }

                #endregion ValueName
            }
            else
            {
                throw new Exception("Cell is not a valid Value Key");
            }
        }

        #endregion Constructors

        #region StaticMethods

        public static ValueKey Get(string path, string key, string val)
        {
            byte[] bytes = RegistryHelper.GetHiveBytes(path);

            NamedKey hiveroot = RegistryHelper.GetRootKey(bytes, path);

            NamedKey nk = hiveroot;

            if (key != null)
            {
                foreach (string k in key.Split('\\'))
                {
                    foreach (NamedKey n in nk.GetSubKeys(bytes))
                    {
                        if (n.Name.ToUpper() == k.ToUpper())
                        {
                            nk = n;
                        }
                    }
                }
                if (nk == hiveroot)
                {
                    throw new Exception(string.Format("Cannot find key '{0}' in the '{1}' hive because it does not exist.", key, path));
                }
            }

            ValueKey[] values = nk.GetValues(bytes);

            foreach (ValueKey v in values)
            {
                if (v.Name.ToUpper() == val.ToUpper())
                {
                    return v;
                }
            }

            throw new Exception(string.Format("Cannot find value '{0}' as a value of '{1}' in the '{2}' hive because it does not exist.", val, key, path));
        }

        internal static ValueKey Get(byte[] bytes, string path, string key, string val)
        {
            NamedKey hiveroot = RegistryHelper.GetRootKey(bytes, path);

            NamedKey nk = hiveroot;

            if (key != null)
            {
                foreach (string k in key.Split('\\'))
                {
                    foreach (NamedKey n in nk.GetSubKeys(bytes))
                    {
                        if (n.Name.ToUpper() == k.ToUpper())
                        {
                            nk = n;
                        }
                    }
                }
            }

            ValueKey[] values = nk.GetValues(bytes);

            foreach (ValueKey v in values)
            {
                if (v.Name.ToUpper() == val.ToUpper())
                {
                    return v;
                }
            }

            return null;
        }

        public static ValueKey[] GetInstances(string path, string key)
        {
            byte[] bytes = RegistryHelper.GetHiveBytes(path);

            NamedKey hiveroot = RegistryHelper.GetRootKey(bytes, path);

            NamedKey nk = hiveroot;

            if (key != null)
            {
                foreach (string k in key.Split('\\'))
                {
                    foreach (NamedKey n in nk.GetSubKeys(bytes))
                    {
                        if (n.Name.ToUpper() == k.ToUpper())
                        {
                            nk = n;
                        }
                    }
                }
            }

            return nk.GetValues(bytes);
        }

        internal static ValueKey[] GetInstances(byte[] bytes, string path, string key)
        {
            NamedKey hiveroot = RegistryHelper.GetRootKey(bytes, path);

            NamedKey nk = hiveroot;

            if (key != null)
            {
                foreach (string k in key.Split('\\'))
                {
                    foreach (NamedKey n in nk.GetSubKeys(bytes))
                    {
                        if (n.Name.ToUpper() == k.ToUpper())
                        {
                            nk = n;
                        }
                    }
                }
            }

            return nk.GetValues(bytes);
        }

        #endregion StaticMethods

        #region InstanceMethods

        public object GetData()
        {
            return this.GetData(RegistryHelper.GetHiveBytes(this.HivePath));
        }

        internal object GetData(byte[] bytes)
        {
            if (this.ResidentData)
            {
                return BitConverter.GetBytes(this.DataOffset - RegistryHeader.HBINOFFSET);
            }
            else if (Encoding.ASCII.GetString(bytes, (int)this.DataOffset + 0x04, 0x02) == "db")
            {
                return BigData.Get(bytes, this);
            }
            else
            {
                switch (this.DataType)
                {
                    case VALUE_KEY_DATA_TYPES.REG_NONE:
                        return Helper.GetSubArray(bytes, (int)this.DataOffset + 0x04, (int)this.DataLength);
                    case VALUE_KEY_DATA_TYPES.REG_SZ:
                        return Encoding.Unicode.GetString(bytes, (int)this.DataOffset + 0x04, (int)this.DataLength).TrimEnd('\0');
                    case VALUE_KEY_DATA_TYPES.REG_EXPAND_SZ:
                        return Helper.GetSubArray(bytes, (int)this.DataOffset + 0x04, (int)this.DataLength);
                    case VALUE_KEY_DATA_TYPES.REG_BINARY:
                        return Helper.GetSubArray(bytes, (int)this.DataOffset + 0x04, (int)this.DataLength);
                    case VALUE_KEY_DATA_TYPES.REG_DWORD:
                        return Helper.GetSubArray(bytes, (int)this.DataOffset + 0x04, (int)this.DataLength);
                    case VALUE_KEY_DATA_TYPES.REG_DWORD_BIG_ENDIAN:
                        return Helper.GetSubArray(bytes, (int)this.DataOffset + 0x04, (int)this.DataLength);
                    case VALUE_KEY_DATA_TYPES.REG_LINK:
                        return Helper.GetSubArray(bytes, (int)this.DataOffset + 0x04, (int)this.DataLength);
                    case VALUE_KEY_DATA_TYPES.REG_MULTI_SZ:
                        return Helper.GetSubArray(bytes, (int)this.DataOffset + 0x04, (int)this.DataLength);
                    case VALUE_KEY_DATA_TYPES.REG_RESOURCE_LIST:
                        return Helper.GetSubArray(bytes, (int)this.DataOffset + 0x04, (int)this.DataLength);
                    case VALUE_KEY_DATA_TYPES.REG_FULL_RESOURCE_DESCRIPTOR:
                        return Helper.GetSubArray(bytes, (int)this.DataOffset + 0x04, (int)this.DataLength);
                    case VALUE_KEY_DATA_TYPES.REG_RESOURCE_REQUIREMENTS_LIST:
                        return Helper.GetSubArray(bytes, (int)this.DataOffset + 0x04, (int)this.DataLength);
                    case VALUE_KEY_DATA_TYPES.REG_QWORD:
                        return Helper.GetSubArray(bytes, (int)this.DataOffset + 0x04, (int)this.DataLength);
                    default:
                        return Helper.GetSubArray(bytes, (int)this.DataOffset + 0x04, (int)this.DataLength);
                }
                
            }
        }

        #endregion InstanceMethods
    }

    #endregion ValueKeyClass

    class BigData
    {
        #region StaticMethods

        internal static byte[] Get(byte[] bytes, ValueKey vk)
        {
            List<byte> contents = new List<byte>();
            
            byte[] dataBytes = PowerForensics.Helper.GetSubArray(bytes, (int)vk.DataOffset, Math.Abs(BitConverter.ToInt32(bytes, (int)vk.DataOffset)));

            short offsetCount = BitConverter.ToInt16(dataBytes, 0x06);
            uint offsetOffset = BitConverter.ToUInt32(dataBytes, 0x08) + RegistryHeader.HBINOFFSET;

            byte[] offsetBytes = Helper.GetSubArray(bytes, (int)offsetOffset, Math.Abs(BitConverter.ToInt32(bytes, (int)offsetOffset)));

            for (short i = 1; i <= offsetCount; i++)
            {
                uint segmentOffset = BitConverter.ToUInt32(offsetBytes, i * 0x04) + RegistryHeader.HBINOFFSET;
                contents.AddRange(Helper.GetSubArray(bytes, (int)segmentOffset + 0x04, Math.Abs(BitConverter.ToInt32(bytes, (int)segmentOffset)) - 0x08));
            }

            byte[] b = contents.ToArray();
            return Helper.GetSubArray(b, 0x00, b.Length);
        }

        #endregion StaticMethods
    }
}