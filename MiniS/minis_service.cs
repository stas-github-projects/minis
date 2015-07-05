using System;
using System.Text;
using System.Collections.Generic;

namespace MiniS
{
    /// <summary>
    /// Description of BeeSevice.
    /// </summary>
    internal static class minis_service
    {
        public static bool CompareArrays(this byte[] _src, byte[] _compare_with)
        {
            return CompareArrays(_src, ref _compare_with);
        }
        public static bool CompareArrays(this byte[] _src, ref byte[] _compare_with)
        {
            int ilen1 = _src.Length, ilen2 = _compare_with.Length;
            if (ilen1 != ilen2) { return false; } //arrays are not equal
            for (int i = 0; i < ilen1; i++)
            {
                if (_src[i] != _compare_with[i])
                { return false; } //not equal
            }
            return true;
        }


        public static void InsertBytes(this byte[] _src, byte[] _what, int _pos = 0, int _length = 0)
        {
            InsertBytes(_src, ref _what, _pos, _length);
        }
        public static void InsertBytes(this byte[] _src, byte _what, int _pos = 0, int _length = 0)
        {
            int i = _src.Length;
            if (_pos < 0) { _pos = 0; }
            if (_length == 0) { _length = 1;}// _what.Length; }
            if (_pos + _length > i) { return; }//out of dimensions
            //Buffer.BlockCopy(_what, 0, _src, _pos, _length);
            _src[_pos] = _what;
        }
        public static void InsertBytes(this byte[] _src, ref byte[] _what, int _pos = 0, int _length = 0)
        {
            int i = _src.Length;
            if (_pos < 0) { _pos = 0; }
            if (_length == 0) { _length = _what.Length; }
            if (_pos + _length > i) { return; }//out of dimensions
            Buffer.BlockCopy(_what, 0, _src, _pos, _length);
        }
        public static byte[] GetBytes(this byte[] _src, int _pos = 0, int _length = 0)
        {
            byte[] b_out;
            int ilen = _src.Length;
            if (_length < 1) { _length = ilen; }
            if (_pos < 0) { _pos = 0; }
            if (ilen >= (_pos + _length))
            {
                b_out = new byte[_length];
                Buffer.BlockCopy(_src, _pos, b_out, 0, _length);//copy piece
                return b_out;
            }
            else
            { return _src; }
        }

        public static byte returnTypeAndRawByteArray(ref object _data, int i_fixed_string, out byte[] out_bytes)
         {
            byte _data_type = new byte();
            _data_type = 0;

            if (_data == null) { out_bytes = new byte[0]; _data_type = 0; return 0; }

            Type _type = _data.GetType();
            if (_type == typeof(bool))// = 3
            {
                out_bytes = BitConverter.GetBytes((bool)_data);
                _data_type = 3;
            }
            else if (_type == typeof(int))// = 4
            {
                out_bytes = BitConverter.GetBytes((int)_data);
                _data_type = 4;
            }
            else if (_type == typeof(long))// = 5
            {
                out_bytes = BitConverter.GetBytes((long)_data);
                _data_type = 5;
            }
            else if (_type == typeof(double))// = 6
            {
                out_bytes = BitConverter.GetBytes((double)_data);
                _data_type = 6;
            }
            else if (_type == typeof(decimal))// = 7
            {
                out_bytes = DecimalToBytes((decimal)_data);
                _data_type = 7;
            }
            else if (_type == typeof(short))// = 8
            {
                out_bytes = BitConverter.GetBytes((short)_data);
                _data_type = 8;
            }
            else if (_type == typeof(float))// = 9
            {
                out_bytes = BitConverter.GetBytes((float)_data);
                _data_type = 9;
            }
            else if (_type == typeof(char))// = 10
            {
                Encoding _enc = Encoding.Unicode;
                out_bytes = BitConverter.GetBytes((char)_data); //Encoding.Default.GetBytes((char)_data);
                _data_type = 10;
            }
            else if (_type == typeof(char[]))// = 11
            {
                //Encoding _enc = Encoding.Default;
                out_bytes = Encoding.Unicode.GetBytes((char[])_data);
                _data_type = 11;
            }
            else if (_type == typeof(string))// = 12
            {
                if (i_fixed_string == 0)//not fixed_string
                {
                    out_bytes = Encoding.Unicode.GetBytes((string)_data);
                    _data_type = 12;
                }
                else
                {
                    out_bytes = Encoding.Unicode.GetBytes((string)_data);
                    if (out_bytes.Length <= i_fixed_string)//if less or equal that it could be
                    { _data_type = 18; }
                    else
                    { _data_type = 0; }//error: length is more than buffer
                }
            }
            else if (_type == typeof(byte))// = 13
            {
                out_bytes = new byte[1];
                out_bytes[0] = (byte)_data;
                _data_type = 13;
            }
            else if (_type == typeof(byte[]))// = 14
            {
                out_bytes = (byte[])_data;
                _data_type = 14;
            }
            else if (_type == typeof(ushort))// = 15
            {
                out_bytes = BitConverter.GetBytes((ushort)_data);
                _data_type = 15;
            }
            else if (_type == typeof(uint))// = 16
            {
                out_bytes = BitConverter.GetBytes((uint)_data);
                _data_type = 16;
            }
            else if (_type == typeof(ulong))// = 17
            {
                out_bytes = BitConverter.GetBytes((ulong)_data);
                _data_type = 17;
            }
            else
            {
                out_bytes = new byte[0];
                _data_type = 0;
            }
            return _data_type;
        }

        public static object returnObjectFromByteArray(ref byte[] b_output, byte _type)
        {
            switch (_type)
            {
                case 3://bool
                    bool _bool = BitConverter.ToBoolean(b_output, 0);
                    return _bool;
                case 4://int
                    int _int = BitConverter.ToInt32(b_output, 0);
                    return _int;
                case 5://long
                    long _long = BitConverter.ToInt64(b_output, 0);
                    return _long;
                case 6://double
                    double _dbl = BitConverter.ToDouble(b_output, 0);
                    return _dbl;
                case 7://decimal
                    decimal _dec = ByteArrayToDecimal(b_output, 0);//Convert.ToDecimal(BitConverter.ToDouble(b_output, 0));
                    return _dec;
                case 8://short
                    short _short = BitConverter.ToInt16(b_output, 0);
                    return _short;
                case 9://float
                    float _float = BitConverter.ToSingle(b_output, 0);
                    return _float;
                case 10://char
                    char[] _char = Encoding.Unicode.GetChars(b_output);
                    return _char[0];
                case 11://char[]
                    char[] _chararr = Encoding.Unicode.GetChars(b_output);
                    return _chararr;
                case 12://string
                    string _string = Encoding.Unicode.GetString(b_output);
                    return _string;
                case 13://byte
                    return b_output[0];
                case 14://byte[]
                    return b_output;
                case 15://ushort
                    ushort _ushort = (ushort)BitConverter.ToUInt16(b_output, 0);
                    return _ushort;
                case 16://uint
                    uint _uint = (uint)BitConverter.ToUInt32(b_output, 0);
                    return _uint;
                case 17://ulong
                    ulong _ulong = (ulong)BitConverter.ToUInt64(b_output, 0);
                    return _ulong;
                case 18://fixed_string
                    string _fixedstring = Encoding.Unicode.GetString(b_output);
                    return _fixedstring;
                case 19://fixed_byte_array
                    return b_output;
                default: return false;
            }//switch
        }

        public static byte[] DecimalToBytes(decimal dec)
        {
            //Load four 32 bit integers from the Decimal.GetBits function
            Int32[] bits = decimal.GetBits(dec);
            //Create a temporary list to hold the bytes
            List<byte> bytes = new List<byte>();
            //iterate each 32 bit integer
            foreach (Int32 i in bits)
            {
                //add the bytes of the current 32bit integer
                //to the bytes list
                bytes.AddRange(BitConverter.GetBytes(i));
            }
            //return the bytes list as an array
            return bytes.ToArray();
        }
        
        public static decimal ByteArrayToDecimal(byte[] src, int offset)
        {
            int i1 = BitConverter.ToInt32(src, offset);
            int i2 = BitConverter.ToInt32(src, offset + 4);
            int i3 = BitConverter.ToInt32(src, offset + 8);
            int i4 = BitConverter.ToInt32(src, offset + 12);
            
            return new decimal(new int[] { i1, i2, i3, i4 });
        }
        
    }
}
