using System;

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
        private byte buffer = 0;
        private byte bufferMask = 1;
        List<byte> data = new List<byte>();
        BinaryWriter writer;
        private byte[][] QTables = new byte[2][]
        {
            new byte[64],
            new byte[64]
        };

        public JPEGExtractor(int quality=50)
        {
            // scale quantization tables according to (provided) quality parameter (1-100)
            if (quality < 1)
            {
                quality = 1;
            }
            else if (quality > 100)
            {
                quality = 100;
            }

            int scale = (quality < 50) ? (50 / quality) : ((100 - quality) / 50);
            for (int i = 0; i < QTables.Length; i++)
            {
                for (int j = 0; j < QTables[i].Length; j++)
                {
                    int q = (JPEGWriter.QuantizationTablesUnscaled[i][j] * scale);
                    if (q < 1)
                    {
                        q = 1;
                    }
                    else if (q > 255)
                    {
                        q = 255;
                    }
                    QTables[i][j] = (byte)q;
                }
            }
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
                    buffer |= (byte)bufferMask;
                }
                
                bufferMask <<= 1;
                msgMask >>= 1;
                if (bufferMask == 0)
                {
                    data.Add(buffer);
                    // System.Console.WriteLine(buffer);
                    done = ChcekData();
                    if (done) 
                    {
                        return;
                    }
                    buffer = 0;
                    bufferMask = 1;
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
    }
}