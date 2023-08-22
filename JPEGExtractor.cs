using System;
using System.Linq;

namespace Steganography
{
    class JPEGExtractor
    {   
        const int BlockSize = 8;
        private int dataLength = 0;
        private int extractedFileNameLength = 0;
        private bool done = false;
        string extractedFileName = "";
        private byte dataBufferMask = 1;
        private byte dataBuffer = 0;
        private BinaryReader reader;
        private byte buffer = 0;
        private int bufferLength = 0;
        List<byte> data = new List<byte>();
       
        private Dictionary<int, int>[] huffmanLUT = new Dictionary<int, int>[4];

        public JPEGExtractor(string stegImageName)
        {
            reader = new BinaryReader(File.Open(stegImageName, FileMode.Open));
        }

        private void CompileHuffmanSpecs(HuffmanSpec[] huffmanSpecs)
        {
            for (int i = 0; i < huffmanSpecs.Length; i++)
            {
                huffmanLUT[i] = new Dictionary<int, int>();
                int code = 0;
                int k = 0;
                for (int j = 0; j < huffmanSpecs[i].count.Length; j++)
                {
                    int nBits = (j + 1) << 24;
                    for (int l = 0; l < huffmanSpecs[i].count[j]; l++)
                    {
                        huffmanLUT[i][nBits | code] = huffmanSpecs[i].symbol[k];
                        code++;
                        k++;
                    }
                    code <<= 1;
                }
            }
        }

        private int ReadBit()
        {
            if (bufferLength == 0)
            {
                buffer = reader.ReadByte();
                bufferLength = 8;
                if (buffer == 0xFF)
                {
                    if (reader.ReadByte() != 0x00)
                    {
                        throw new ArgumentException("Invalid file format. Expected 0x00 after 0xFF");
                    }
                }
            }
            bufferLength--;
            return (buffer >> bufferLength) & 1;
        }

        private int ReadBits(int nBits)
        {
            int result = 0;
            for (int i = 0; i < nBits; i++)
            {
                result <<= 1;
                result |= ReadBit();
            }
            return result;
        }

        private bool ChcekData()
        {
            if (done) return true;
            // check if equals to magic number
            if (data.Count == 13)
            {
                for (int i = 0; i < 13; i++)
                {
                    if (data[i] != HiddenFile.magicNumber[i])
                    {
                        throw new ArgumentException("Invalid file format");
                    }
                }
                System.Console.WriteLine("magic number ok");
                return false;
            }

            // length of filename
            if (data.Count == 15)
            {
                extractedFileNameLength = data[14];
                System.Console.WriteLine("filename length: " + extractedFileNameLength);
                return false;
            }

            // get filename string
            if (data.Count == 15 + extractedFileNameLength)
            {
                for (int i = 0; i < extractedFileNameLength; i += 2)
                {
                    extractedFileName += (char)((data[15 + i] << 8) | data[15 + i + 1]);
                }
                System.Console.WriteLine("filename: " + extractedFileName);
                return false;
            }

            // get file data length
            if (data.Count == 15 + extractedFileNameLength + 4)
            {
                // length of file data
                for (int i = 0; i < 4; i++)
                {
                    dataLength |= (data[15 + extractedFileNameLength + i] << (8*i));
                }
                System.Console.WriteLine("file data length: " + dataLength);
                return false;
            }

            // get file data is extracted
            if (data.Count == 15 + extractedFileNameLength + 4 + dataLength)
            {
                // file data
                // for (int i = 0; i < dataLength; i++)
                // {
                //     System.Console.Write((char)data[15 + extractedFileNameLength + 4 + i]);
                // }
                System.Console.WriteLine("all file data extracted");
                return true;
            }
            return false;
        }

        private void WriteBit(int bit)
        {
            if (done) 
            {
                return;
            }

            if (bit == 1)
            {
                dataBuffer |= (byte)dataBufferMask;
            }
            dataBufferMask <<= 1;
            if (dataBufferMask == 0)
            {
                data.Add(dataBuffer);
                done = ChcekData();
                dataBuffer = 0;
                dataBufferMask = 1;
            }
        }

        public void ReadFile()
        {
            byte[] SOI = reader.ReadBytes(2);
            if (SOI[0] != 0xFF || SOI[1] != 0xD8)
            {
                throw new ArgumentException("Invalid file format. SOI marker expected.");
            }
            ProcessMarker(JPEGWriter.DQT);
            ProcessMarker(JPEGWriter.SOF0);
            GetHuffmanTables();
            ProcessMarker(JPEGWriter.SOS);
            while (!done)
            {
                ProcessSOSBlock(0);
                ProcessSOSBlock(1);
                ProcessSOSBlock(1);
            }
            reader.Close();
            reader.Dispose();

            using (BinaryWriter writer = new BinaryWriter(File.Open("extr_" + extractedFileName, FileMode.Create)))
            {
                writer.Write(data.ToArray(), 15 + extractedFileNameLength + 4, dataLength);
            }
        }

        private void ProcessMarker(byte[] marker)
        {
            byte[] markerRead = reader.ReadBytes(2);
            if (markerRead[0] != marker[0] || markerRead[1] != marker[1])
            {
                throw new ArgumentException("Invalid file format. Expected marker " + marker[0] + " " + marker[1]);
            }
            // get length of marker
            byte[] markerLength = reader.ReadBytes(2);
            int markerLengthInt = (markerLength[0] << 8) | markerLength[1];
            // don't care about this marker contents
            reader.ReadBytes(markerLengthInt - 2);
        }
        
        private void GetHuffmanTables()
        {
            // check DHT marker
            byte[] DHT = reader.ReadBytes(2);
            if (DHT[0] != 0xFF || DHT[1] != 0xC4)
            {
                throw new ArgumentException("Invalid file format. Expected DHT marker");
            }
            
            // get length of DHT
            byte[] DHTLength = reader.ReadBytes(2);
            int DHTLengthInt = (DHTLength[0] << 8) | DHTLength[1] - 2;

            HuffmanSpec[] huffmanSpecs = new HuffmanSpec[4]
            {
                new HuffmanSpec(),
                new HuffmanSpec(),
                new HuffmanSpec(),
                new HuffmanSpec()
            };
            
            int bytesRead = 0;
            while (bytesRead < DHTLengthInt)
            {
                // read DHT info
                byte DHTinfo = reader.ReadByte();
                int tableClass = DHTinfo >> 4;
                int tableID = DHTinfo & 0xF;
                // read DHT counts (BITS)
                byte[] DHTcounts = reader.ReadBytes(16);
                bytesRead += 17;
                // read DHT symbols (HUFFVAL)
                byte[] DHTsymbols = reader.ReadBytes(DHTcounts.Sum());
                bytesRead += DHTcounts.Sum();
                // save huffman specification
                huffmanSpecs[tableID * 2 + tableClass].count = DHTcounts;
                huffmanSpecs[tableID * 2 + tableClass].symbol = DHTsymbols;
            }
            // make look-up tables
            CompileHuffmanSpecs(huffmanSpecs);
        }

        private void ProcessSOSBlock(int component)
        {
            int counter = 1;
            int DCcode = ReadBit();
            int nBits = 1;
            
            while (!huffmanLUT[component * 2].ContainsKey((nBits << 24) | DCcode))
            {
                DCcode <<= 1;
                DCcode |= ReadBit();
                nBits++;
            }
            int DC = huffmanLUT[component * 2][(nBits << 24) | DCcode];
            ReadBits(DC);

            int ACcode = 0;
            nBits = 0;
            while (counter < 64)
            {
                ACcode = ReadBit();
                nBits = 1;
                while (!huffmanLUT[component * 2 + 1].ContainsKey((nBits << 24) | ACcode))
                {
                    ACcode <<= 1;
                    ACcode |= ReadBit();
                    nBits++;
                }
                int AC = huffmanLUT[component * 2 + 1][(nBits << 24) | ACcode];
                
                // end of block special code
                if (AC == 0)
                {
                    break;
                }

                // skip 16 zeros
                if (AC == 0xF0)
                {
                    counter += 16;
                    continue;
                }

                int run = AC >> 4;
                int size = AC & 0xF;
                counter += run;
                if (size == 0)
                {
                    continue;
                }
                
                int value = ReadBits(size);
                
                // if negative, extend sign according to specifiaction
                if (value < (1 << (size - 1)))
                {
                    value += (-1 << size) + 1;
                }

                // if magnitude is greater than 1, there is a hidden bit
                if (value > 1 || value < -1)
                {
                    int bit = value & 1;
                    WriteBit(bit);
                }
                counter++;
            }
        }
    }
}