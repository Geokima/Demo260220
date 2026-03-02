using System;
using System.IO;
using System.Text;

namespace Framework.Utils
{
    public class ByteBuffer : MemoryStream
    {
        public ByteBuffer() { }

        public ByteBuffer(byte[] buffer) : base(buffer) { }

        #region Short
        public short ReadShort()
        {
            byte[] arr = new byte[2];
            base.Read(arr, 0, 2);
            return BitConverter.ToInt16(arr, 0);
        }

        public void WriteShort(short value)
        {
            byte[] arr = BitConverter.GetBytes(value);
            base.Write(arr, 0, arr.Length);
        }
        #endregion

        #region UShort
        public ushort ReadUShort()
        {
            byte[] arr = new byte[2];
            base.Read(arr, 0, 2);
            return BitConverter.ToUInt16(arr, 0);
        }

        public void WriteUShort(ushort value)
        {
            byte[] arr = BitConverter.GetBytes(value);
            base.Write(arr, 0, arr.Length);
        }
        #endregion

        #region Int
        public int ReadInt()
        {
            byte[] arr = new byte[4];
            base.Read(arr, 0, 4);
            return BitConverter.ToInt32(arr, 0);
        }

        public void WriteInt(int value)
        {
            byte[] arr = BitConverter.GetBytes(value);
            base.Write(arr, 0, arr.Length);
        }
        #endregion

        #region UInt
        public uint ReadUInt()
        {
            byte[] arr = new byte[4];
            base.Read(arr, 0, 4);
            return BitConverter.ToUInt32(arr, 0);
        }

        public void WriteUInt(uint value)
        {
            byte[] arr = BitConverter.GetBytes(value);
            base.Write(arr, 0, arr.Length);
        }
        #endregion

        #region Long
        public long ReadLong()
        {
            byte[] arr = new byte[8];
            base.Read(arr, 0, 8);
            return BitConverter.ToInt64(arr, 0);
        }

        public void WriteLong(long value)
        {
            byte[] arr = BitConverter.GetBytes(value);
            base.Write(arr, 0, arr.Length);
        }
        #endregion

        #region ULong
        public ulong ReadULong()
        {
            byte[] arr = new byte[8];
            base.Read(arr, 0, 8);
            return BitConverter.ToUInt64(arr, 0);
        }

        public void WriteULong(ulong value)
        {
            byte[] arr = BitConverter.GetBytes(value);
            base.Write(arr, 0, arr.Length);
        }
        #endregion

        #region Float
        public float ReadFloat()
        {
            byte[] arr = new byte[4];
            base.Read(arr, 0, 4);
            return BitConverter.ToSingle(arr, 0);
        }

        public void WriteFloat(float value)
        {
            byte[] arr = BitConverter.GetBytes(value);
            base.Write(arr, 0, arr.Length);
        }
        #endregion

        #region Double
        public double ReadDouble()
        {
            byte[] arr = new byte[8];
            base.Read(arr, 0, 8);
            return BitConverter.ToDouble(arr, 0);
        }

        public void WriteDouble(double value)
        {
            byte[] arr = BitConverter.GetBytes(value);
            base.Write(arr, 0, arr.Length);
        }
        #endregion

        #region Bool
        public bool ReadBool()
        {
            return base.ReadByte() == 1;
        }

        public void WriteBool(bool value)
        {
            base.WriteByte((byte)(value ? 1 : 0));
        }
        #endregion

        #region UTF8String
        public string ReadUTF8String()
        {
            ushort len = ReadUShort();
            if (len == 0) return string.Empty;

            byte[] arr = new byte[len];
            base.Read(arr, 0, len);
            return Encoding.UTF8.GetString(arr);
        }

        public void WriteUTF8String(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                WriteUShort(0);
                return;
            }

            byte[] arr = Encoding.UTF8.GetBytes(str);
            if (arr.Length > ushort.MaxValue)
            {
                throw new ArgumentException($"String length {arr.Length} exceeds maximum {ushort.MaxValue}");
            }

            WriteUShort((ushort)arr.Length);
            base.Write(arr, 0, arr.Length);
        }
        #endregion
    }
}
