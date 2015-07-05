using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MiniS
{
    public partial class Engine
    {
        internal globals _GLOBALS = new globals();

        //
        // OPEN STORAGE
        //

        public bool OPEN_STORAGE(string storage_name)
        {
            if (storage_name.Length == 0) { return false; } //error: zero-length storage name

            int ipos = 0;
            byte[] b_header_buffer = new byte[_GLOBALS._global_header_length];
            b_header_buffer.InsertBytes(_GLOBALS._version, ipos); ipos += 7; //version = 7 //byte array
            b_header_buffer[ipos] = (byte)_GLOBALS._key_maxlen; ipos++; //key_name_max_len = 1 //byte
            b_header_buffer.InsertBytes(BitConverter.GetBytes(_GLOBALS._keys_page_size), ipos); ipos += 2; //keys_page_size = 2 //ushort
            b_header_buffer.InsertBytes(BitConverter.GetBytes(_GLOBALS._keys_page_last_pos), ipos); ipos += 8; //keys_page_last_pos = 8 //long
            b_header_buffer.InsertBytes(BitConverter.GetBytes(_GLOBALS.keys_page_freecells), ipos); ipos += 2; //keys_page_freecells = 2; //ushort
            b_header_buffer[ipos] = (byte)_GLOBALS._key_chains_query; ipos++; //keys_query_char = 1 //Encoding.ASCII
            b_header_buffer[ipos] = (byte)_GLOBALS._key_chains_delimeter; ipos++; //keys_chains_delimeter = 1 //Encoding.ASCII
            b_header_buffer.InsertBytes(BitConverter.GetBytes(_GLOBALS.unique_key_id), ipos); ipos += 8; //unique_key_id = 8 //ulong
            b_header_buffer.InsertBytes(BitConverter.GetBytes(DateTime.Now.Ticks), ipos); //created_at = 8 //long

            if (_GLOBALS.bool_stream == false) //create new storage
            {
                _GLOBALS._filestream = new FileStream(storage_name, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, true);
                _GLOBALS._filestream.Position = 0;
                _GLOBALS._filestream.Write(b_header_buffer, 0, _GLOBALS._global_header_length);
                _GLOBALS._filestream_length = _GLOBALS._global_header_length;
                _GLOBALS.bool_stream = true;
            }
            return true;
        }

        //
        // PUT to storage
        //

        public bool SET(string key, object value)
        {
            if (key.Length == 0) { return false; } //error: zero-length storage name
            if (_GLOBALS.bool_stream == false) { return false; } //error: storage not opened

            byte[] b_temp8 = new byte[8];
            byte[] b_global_buffer;
            byte[] b_buffer = new byte[_GLOBALS._global_keychain_length];
            string[] arr_chains = key.Split(new char[] { _GLOBALS._key_chains_delimeter }, StringSplitOptions.RemoveEmptyEntries);

            bool bool_create_new_page= false;
            long lvaluepos = 0, lposinstorage = 0;
            int ichainslen = arr_chains.Length, idatalen = 0, ipos = 0;
            ulong uprevhash = 0, ucurrenthash = 0;
            string skey = "";

            //get data type and convert data to byte array
            byte[] b_value;
            byte btype = minis_service.returnTypeAndRawByteArray(ref value, 0, out b_value);
            idatalen = b_value.Length;

            //set global buffer's size
            /**/
            if (_GLOBALS.keys_page_freecells >= ichainslen)
            {
                b_global_buffer = new byte[8 + (ichainslen * _GLOBALS._global_keychain_length)];
                _GLOBALS._filestream.Position = 10; //last keys page pos info
                byte[] b_temp = new byte[8];
                _GLOBALS._filestream.Read(b_temp, 0, 8);
                lposinstorage = BitConverter.ToInt64(b_temp, 0); //pos of last keys page
            }
            else
            { 
                b_global_buffer = new byte[8 + (_GLOBALS.keys_page_freecells * _GLOBALS._global_keychain_length)];
                bool_create_new_page = false; lposinstorage = _GLOBALS._filestream.Length;
            } //create new page in future
            //*/

            //go through all chains to check out if the key's chains exists
            for (int i = 0; i < ichainslen; i++)
            {
                skey = arr_chains[i];
                ucurrenthash = CreateHash64bit(Encoding.ASCII.GetBytes(skey));

                //proceed
                if (_GLOBALS.keys_page_freecells > 0 && _GLOBALS.keys_page_freecells != _GLOBALS._keys_page_size) //add to existing page
                {

                    //if it's last chain
                    if (i == ichainslen - 1)
                    { lvaluepos = _GLOBALS._filestream.Length; }

                    fillupkeybuffer(ref b_buffer, arr_chains[i], btype, uprevhash, ucurrenthash, lvaluepos, idatalen);

                    ipos = (_GLOBALS._keys_page_size - _GLOBALS.keys_page_freecells) * _GLOBALS._global_keychain_length + 8;
                    b_global_buffer.InsertBytes(b_buffer, ipos);

                    //save global buffer to storage
                    if (i == ichainslen - 1)
                    {
                        int itemplen = b_global_buffer.Length;
                        _GLOBALS._filestream.Position = lposinstorage;
                        _GLOBALS._filestream.Write(b_global_buffer, 0, itemplen);
                        lposinstorage += itemplen;
                        //write value
                        _GLOBALS._filestream.Position = lposinstorage;
                        _GLOBALS._filestream.Write(b_value, 0, idatalen);
                        lposinstorage += idatalen;
                        //save unique_id to header
                        _GLOBALS._filestream.Position = 22;
                        _GLOBALS._filestream.Write(BitConverter.GetBytes(_GLOBALS.unique_key_id), 0, 8);
                    }
                    bool_create_new_page = true;
                }
                else //create new page
                {
                    //save prev global buffer to storage
                    if (b_global_buffer != null && bool_create_new_page == true)
                    {
                        int itemplen = b_global_buffer.Length;
                        _GLOBALS._filestream.Position = lposinstorage;
                        _GLOBALS._filestream.Write(b_global_buffer, 0, itemplen);
                        
                        //save current pos to prev page
                        _GLOBALS._filestream.Position = 10; //last_page_pos = 10;
                        _GLOBALS._filestream.Read(b_temp8, 0, 8);
                        //save pos of the last page to the global header
                        _GLOBALS._filestream.Write(BitConverter.GetBytes(lposinstorage), 0, 8);
                        long l_lastpagepos = BitConverter.ToInt64(b_temp8, 0);
                        //if 0 - means girst page in storage
                        if (l_lastpagepos != 0) //save pos of current page to previous page's header
                        {
                            _GLOBALS._filestream.Position = l_lastpagepos;
                            _GLOBALS._filestream.Write(BitConverter.GetBytes(lposinstorage), 0, 8);
                        }                        

                        lposinstorage += itemplen;
                        //save unique_id to header
                        //_GLOBALS._filestream.Position = 22;
                        //_GLOBALS._filestream.Write(BitConverter.GetBytes(_GLOBALS.unique_key_id), 0, 8);
                    }
                    //create new one
                    //{
                        _GLOBALS.keys_page_freecells = _GLOBALS._keys_page_size;
                        b_global_buffer = new byte[8 + (_GLOBALS.keys_page_freecells * _GLOBALS._global_keychain_length)];
                        bool_create_new_page = true; lposinstorage = _GLOBALS._filestream.Length;
                    //}
                    //if it's last chain
                    b_buffer = new byte[_GLOBALS._global_keychain_length];
                    if (i == ichainslen - 1)
                    { lvaluepos = _GLOBALS._filestream.Length;}// +b_global_buffer.Length; }
                    fillupkeybuffer(ref b_buffer, arr_chains[i], btype, uprevhash, ucurrenthash, lvaluepos, idatalen);
                    ipos = (_GLOBALS._keys_page_size - _GLOBALS.keys_page_freecells) * _GLOBALS._global_keychain_length + 8;
                    b_global_buffer.InsertBytes(b_buffer, ipos);

                    //save global buffer to storage
                    if (i == ichainslen - 1)
                    {
                        int itemplen = b_global_buffer.Length;
                        _GLOBALS._filestream.Position = lposinstorage;
                        _GLOBALS._filestream.Write(b_global_buffer, 0, itemplen);
                        lposinstorage += itemplen;
                        //write value
                        _GLOBALS._filestream.Position = lposinstorage;
                        _GLOBALS._filestream.Write(b_value, 0, idatalen);
                        lposinstorage += idatalen;
                        //save unique_id to header
                        _GLOBALS._filestream.Position = 22;
                        _GLOBALS._filestream.Write(BitConverter.GetBytes(_GLOBALS.unique_key_id), 0, 8);
                    }
                }
                //updates
                uprevhash = ucurrenthash;
                _GLOBALS.keys_page_freecells--;
            }


            return true;
        }

        internal void fillupkeybuffer(ref byte[] _buffer, string _key, byte btype, ulong parenthash, ulong currenthash, long valuepos, int valuelen)
        {
            int ipos = 0;
            _buffer[ipos] = 1; ipos++; //visiblity
            _buffer[ipos] = btype; ipos++; //data type
            _buffer.InsertBytes(BitConverter.GetBytes(parenthash), ipos); ipos += 8; //parent hash
            _buffer.InsertBytes(Encoding.ASCII.GetBytes(_key), ipos); ipos += 8;
            //_buffer.InsertBytes(BitConverter.GetBytes(currenthash), ipos); ipos += 8; //current hash
            _buffer.InsertBytes(BitConverter.GetBytes(DateTime.Now.Ticks), ipos); ipos += 8; //created at
            if (valuepos > 0)
            {
                _buffer.InsertBytes(BitConverter.GetBytes(valuepos), ipos); ipos += 8; //value pos
                _buffer.InsertBytes(BitConverter.GetBytes(valuelen), ipos); ipos += 4; //value len
            }
        }

        //check for hash inside the keys pages
        internal bool iskeyinstorage()
        {

            return true;
        }


    }

    internal class globals
    {
        internal bool bool_stream = false;
        internal FileStream _filestream;
        internal long _filestream_length = 0;

        //buffer sizes
        internal int _global_header_length = 48;
        internal int _global_keychain_length = 48;

        //global header
        internal byte[] _version = Encoding.ASCII.GetBytes(new char[] { 'M', 'I', 'N', 'I', 'S', '0', '1' });
        internal int _key_maxlen_temp;
        internal int _key_maxlen
        {
            set
            { if (value > 255) { value = 254; } 
                _key_maxlen_temp = value; }
            get
            { return _key_maxlen_temp; }
        }
        internal ushort _keys_page_size = 2;
        internal long _keys_page_last_pos = 0;
        internal ushort keys_page_freecells = 2;
        internal long unique_key_id = 0;
        internal long created_at = 0;

        //delimeters
        internal char _key_chains_delimeter = '/';
        internal char _key_chains_query = '*';
        
    }


}
