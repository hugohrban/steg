using System;
using System.Linq;

namespace Steganography
{
    class JPEGExtractor
    {   
        const int BlockSize = 8;
        int dataCounter = 0;
        int dataLength = 0;
        int extractedFileNameLength = 0;
        bool done = false;
        string extractedFileName = "";
        private byte dataBufferMask = 1;
        private byte dataBuffer = 0;
        private BinaryReader reader;
        private byte buffer = 0;
        private int bufferLength = 0;
        private int counter = 0;
        List<byte> data = new List<byte>();
        BinaryWriter writer;

        //quantization tables in zig-zag order - same as saved in the jpeg file
        private byte[][] QTables = new byte[2][]
        {
            new byte[64],
            new byte[64]
        };

        private HuffmanSpec[] huffmanSpecs = new HuffmanSpec[4]
        {
            new HuffmanSpec(),
            new HuffmanSpec(),
            new HuffmanSpec(),
            new HuffmanSpec()
        };

        // private byte[][] huffmanLUT = new byte[4][];

        private Dictionary<int, int>[] huffmanLUT = new Dictionary<int, int>[4];

        public JPEGExtractor(string fileName)
        {
            // scale quantization tables according to (provided) quality parameter (1-100)
            // if (quality < 1)
            // {
            //     quality = 1;
            // }
            // else if (quality > 100)
            // {
            //     quality = 100;
            // }

            // int scale = (quality < 50) ? (50 / quality) : ((100 - quality) / 50);
            // for (int i = 0; i < QTables.Length; i++)
            // {
            //     for (int j = 0; j < QTables[i].Length; j++)
            //     {
            //         int q = (JPEGWriter.QuantizationTablesUnscaled[i][j] * scale);
            //         if (q < 1)
            //         {
            //             q = 1;
            //         }
            //         else if (q > 255)
            //         {
            //             q = 255;
            //         }
            //         QTables[i][j] = (byte)q;
            //     }
            // }
            reader = new BinaryReader(File.Open(fileName, FileMode.Open));
        }

        private void CompileHuffmanSpecs()
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

        // msg works as a queue of bits, actual message bits are after the first 1 bit
        private void ProcessMessage(ulong msg)
        {
            // empty message
            if (msg == 1)
            {
                return;
            }

            int sigBits = 0;
            ulong msgCopy = msg;
            while (msgCopy > 1)
            {
                msgCopy >>= 1;
                sigBits++;
            }

            ulong msgMask = (ulong)(1 << (sigBits - 1));
            while (msgMask > 0)
            {
                
                if ((msg & msgMask) != 0)
                {
                    dataBuffer |= (byte)dataBufferMask;
                }
                
                dataBufferMask <<= 1;
                msgMask >>= 1;
                if (dataBufferMask == 0)
                {
                    data.Add(dataBuffer);
                    // System.Console.WriteLine(buffer);
                    done = ChcekData();
                    if (done) 
                    {
                        return;
                    }
                    dataBuffer = 0;
                    dataBufferMask = 1;
                }
            }
        }
        public void RevealFile(dctCoeffs[,] quantized)
        {
            for (int j = 0; j < quantized.GetLength(1); j += BlockSize)
            {
                for (int i = 0; i < quantized.GetLength(0); i += BlockSize)
                {
                    var quantizedY = new int[BlockSize, BlockSize];
                    var quantizedCb = new int[BlockSize, BlockSize];
                    var quantizedCr = new int[BlockSize, BlockSize];
                    for (int k = 0; k < BlockSize; k++)
                    {
                        for (int l = 0; l < BlockSize; l++)
                        {
                            quantizedY[k, l] = quantized[i + k, j + l].Y;
                            quantizedCb[k, l] = quantized[i + k, j + l].Cb;
                            quantizedCr[k, l] = quantized[i + k, j + l].Cr;
                        }
                    }
                    ProcessMessage(RevealBlock(quantizedY, 0));
                    ProcessMessage(RevealBlock(quantizedCb, 1));
                    ProcessMessage(RevealBlock(quantizedCr, 1));
                    if (done) 
                    {
                        using (writer = new BinaryWriter(File.Open(extractedFileName, FileMode.OpenOrCreate)))
                        {
                            for (int k = 15 + extractedFileNameLength + 4; k < data.Count; k++)
                            {
                                writer.Write(data[k]);
                            }
                            writer.Flush();
                        }
                        System.Console.WriteLine("done. all file data saved in file: " + extractedFileName);
                        return;
                    }
                }
            }
            if (!done)
            {

                System.Console.WriteLine("not done. something went wrong. :(");
            }
        }
        private ulong RevealBlock(int[,] block, int component)
        {
            // message is a queue of bits. The first 1 bit is a delimiter, the rest is the message
            // bits are hidden as the least significant bits of the quantized DCT coefficients whose 
            // absolute value is greater than 1. We preocess the coefficients in zig-zag order, same as when encoding the jpeg

            ulong message = 1;
            for (int zig = 1; zig < 64; zig++)
            {
                int z = JPEGWriter.ZigZagMap[zig];
                int ac = block[z / 8, z % 8];
                ac = (int)Math.Round(ac / (QTables[component][zig] * 1.0));

                if ((ac < -1 || ac > 1))
                {
                    if (dataCounter % 8 == 0)
                    {
                        // System.Console.WriteLine();
                    }
                    // System.Console.WriteLine(((ac & 1) != 0) ? "1" : "0");
                    dataCounter++;   
                    message <<= 1;
                    
                    var temp = (ac & 1);
                    message |= (ulong)temp;

                    // System.Console.WriteLine(temp);
                }
            }
            return message;
        }
    
        public void ReadFile()
        {
            byte[] SOI = reader.ReadBytes(2);
            if (SOI[0] != 0xFF || SOI[1] != 0xD8)
            {
                throw new ArgumentException("Invalid file format. SOI marker expected.");
            }
            GetQuantizationTable();
            GetSOF0();
            GetDHT();
            CompileHuffmanSpecs();
            GetSOSHeader();
            while (!done)
            {
                ProcessSOSBlock(0);
                ProcessSOSBlock(1);
                ProcessSOSBlock(1);
            }

            // print all data
            for (int i = 15 + extractedFileNameLength + 4; i < data.Count; i++)
            {
                System.Console.Write((char)data[i]);
            }
        }
        private void GetQuantizationTable()
        {
            // check QT marker
            byte[] QT = reader.ReadBytes(2);
            if (QT[0] != 0xFF || QT[1] != 0xDB)
            {
                throw new ArgumentException("Invalid file format. Expected quantization table marker");
            }
            // get length of QT
            byte[] QTLength = reader.ReadBytes(2);
            int QTLengthInt = (QTLength[0] << 8) | QTLength[1];
            // read QT 0
            byte QTindex = reader.ReadByte();
            if (QTindex != 0)
            {
                throw new ArgumentException("Invalid file format. Expected quantization table index 0");
            }
            // read QT 0
            for (int i = 0; i < 64; i++)
            {
                QTables[0][i] = reader.ReadByte();
            }
            // read QT 1
            QTindex = reader.ReadByte();
            if (QTindex != 1)
            {
                throw new ArgumentException("Invalid file format. Expected quantization table index 1");
            }
            // read QT 1
            for (int i = 0; i < 64; i++)
            {
                QTables[1][i] = reader.ReadByte();
            }
        }

        private void GetSOF0()
        {
            // check SOF0 marker
            byte[] SOF0 = reader.ReadBytes(2);
            if (SOF0[0] != 0xFF || SOF0[1] != 0xC0)
            {
                throw new ArgumentException("Invalid file format. Expected SOF0 marker");
            }
            // get length of SOF0
            byte[] SOF0Length = reader.ReadBytes(2);
            int SOF0LengthInt = (SOF0Length[0] << 8) | SOF0Length[1];
            
            // don't care about this marker contents
            reader.ReadBytes(SOF0LengthInt - 2);
        }

        private void GetDHT()
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
            // read DHT
            int bytesRead = 0;
            while (bytesRead < DHTLengthInt)
            {
                // read DHT info
                byte DHTinfo = reader.ReadByte();
                int tableClass = DHTinfo >> 4;
                int tableID = DHTinfo & 0xF;
                // read DHT counts
                byte[] DHTcounts = reader.ReadBytes(16);
                bytesRead += 17;
                // read DHT symbols
                byte[] DHTsymbols = reader.ReadBytes(DHTcounts.Sum());
                bytesRead += DHTcounts.Sum();
                // save DHT
                huffmanSpecs[tableID * 2 + tableClass].count = DHTcounts;
                huffmanSpecs[tableID * 2 + tableClass].symbol = DHTsymbols;
            }
            //System.Console.WriteLine();
        }

        private void GetSOSHeader()
        {
            // check SOS marker
            byte[] SOS = reader.ReadBytes(2);
            if (SOS[0] != 0xFF || SOS[1] != 0xDA)
            {
                throw new ArgumentException("Invalid file format. Expected SOS marker");
            }
            // get length of SOS
            byte[] SOSLength = reader.ReadBytes(2);
            int SOSLengthInt = (SOSLength[0] << 8) | SOSLength[1];
            // don't care about this marker contents
            reader.ReadBytes(SOSLengthInt - 2);
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
            while (ACcode != 0xF0 && counter < 64)
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
                if (AC == 0)
                {
                    break;
                }
                int run = AC >> 4;
                int size = AC & 0xF;
                counter += run;
                if (size == 0)
                {
                    continue;
                }
                int value = ReadBits(size);
                if (value < (1 << (size - 1)))
                {
                    value += (-1 << size) + 1;
                }
                if (value > 1 || value < -1)
                {
                    //System.Console.Write(value + " ");
                    // System.Console.Write(value & 1);
                    this.counter++;
                    if (this.counter == 8)
                    {
                        //System.Console.WriteLine();
                        this.counter = 0;
                    }
                    int bit = value & 1;
                    ProcessMessage((ulong)(bit | 0b10));
                    // dataBuffer <<= 1;
                    // dataBuffer |= (byte)(value & 1);
                }
                // System.Console.Write((AC & 1) == 1 ? "1" : "0");
                //System.Console.WriteLine(AC);
                counter++;
            }
        }
       
       
    }

    public static class Extensions
    {
        //array sum
        public static int Sum(this byte[] array)
        {
            int sum = 0;
            for (int i = 0; i < array.Length; i++)
            {
                sum += array[i];
            }
            return sum;
        }
    }

}