using System;
using System.Text;

namespace Steganography
{
    public class HiddenFile
    {
        public byte[] data { get; private set; }
        public int bitsPerByte { get; private set; }
        public string fileName { get; private set; }

        // spells out "steganography" in hexadecimal
        public static readonly byte[] magicNumber = {0x73, 0x74, 0x65, 0x67,
            0x61, 0x6e, 0x6f, 0x67, 0x72, 0x61, 0x70, 0x68, 0x79};

        // convert chars in a string to a byte array
        private static byte[] GetBytes(string s)
        {
            byte[] bytes = new byte[s.Length * 2];
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                bytes[2 * i] = (byte)(c >> 8);
                bytes[2 * i + 1] = (byte)c;
            }
            return bytes;
        }

        /// <summary>
        /// Class for preprocessing a file to be hidden in an image. Content of file is read as an array of bytes and prepended with
        /// specific metadata to store the file name, encoding parameters, etc.
        /// HiddenFile data structure:
        ///      bytes 0-12 - magic number
        ///      byte 13 - bitsPerByte - number of least-significant bits changed (1-8)
        ///      byte 14 - length of filename in bytes (max 255 UTF-16 characters, 1 char = 2 bytes)
        ///      bytes 15-x - filename
        ///      bytes (x+1)-(x+5) - length of file data in bytes (files up to 2^32 B = 2^22 kB = 2^12 MB = 4 GB)
        ///      bytes (x+6)-... - file data
        /// In case of LSB encoding, bytes 0-13 are be encoded using one bit per byte encoding (only change one least significant bit of a byte),
        /// the rest will be hidden using bitsPerByte encoding, as specified in the metadata, so that we can decode it after
        /// finding out bitsPerByte. bitsPerByte parameter can be specified by the user.
        /// In case of jsteg bitsPerByte is irrelevant.
        /// </summary>
        /// <param name="path"> Path to the file to be hidden</param>
        /// <param name="bitsPerByte"> Number of LS bits to change. Defaults to 1 for jsteg. </param>
        public HiddenFile(string path, int bitsPerByte=1)
        {
            this.bitsPerByte = bitsPerByte;

            fileName = Path.GetFileName(path);
            byte[] fileNameBytes = GetBytes(fileName);
            byte[] fileData = File.ReadAllBytes(path);
            data = new byte[fileData.Length + fileNameBytes.Length + 19];

            // writing magic number
            Array.Copy(magicNumber, 0, data, 0, magicNumber.Length);

            data[13] = (byte)bitsPerByte;

            // writing file name length in bytes (1 char = 2 bytes)
            data[14] = (byte)fileNameBytes.Length;

            // actual filename bytes
            Array.Copy(fileNameBytes, 0, data, 15, fileNameBytes.Length);

            // size of file in bytes
            int fileSize = fileData.Length;
            for (int i = 0; i < 4; i++)
            {
                byte part = (byte)(fileSize >> (8 * i));
                data[15 + fileNameBytes.Length + i] = part;
            }

            // copy actual file data
            Array.Copy(fileData, 0, data, 19 + fileNameBytes.Length, fileData.Length);

        }
    }
}