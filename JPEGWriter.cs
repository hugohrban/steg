using System;
using System.IO;


namespace Steganography
{
    class JPEGWriter : IDisposable
    {
        # region static fields and methods
        public static readonly byte[] SOI = { 0xFF, 0xD8 };
        public static readonly byte[] DQT = { 0xFF, 0xDB };
        public static readonly byte[] DHT = { 0xFF, 0xC4 };
        public static readonly byte[] SOF0 = { 0xFF, 0xC0 };
        public static readonly byte[] SOS = { 0xFF, 0xDA };
        public static readonly byte[] EOI = { 0xFF, 0xD9 };
        const int BlockSize = 8;

        // Unscaled quantTables in zig-zag order. 
        // Each writer instance can set a quality value (1-100)
        public static readonly byte[][] QuantizationTablesUnscaled = new byte[][]
        {
            // Luminance
            new byte[]{
                16, 11, 12, 14, 12, 10, 16, 14,
                13, 14, 18, 17, 16, 19, 24, 40,
                26, 24, 22, 22, 24, 49, 35, 37,
                29, 40, 58, 51, 61, 60, 57, 51,
                56, 55, 64, 72, 92, 78, 64, 68,
                87, 69, 55, 56, 80, 109, 81, 87,
                95, 98, 103, 104, 103, 62, 77, 113,
                121, 112, 100, 120, 92, 101, 103, 99,
            },

            // Chrominance
            new byte[]{
                17, 18, 18, 24, 21, 24, 47, 26,
                26, 47, 99, 66, 56, 66, 99, 99,
                99, 99, 99, 99, 99, 99, 99, 99,
                99, 99, 99, 99, 99, 99, 99, 99,
                99, 99, 99, 99, 99, 99, 99, 99,
                99, 99, 99, 99, 99, 99, 99, 99,
                99, 99, 99, 99, 99, 99, 99, 99,
                99, 99, 99, 99, 99, 99, 99, 99,
            }
        };
        
        public static readonly HuffmanSpec[] huffmanSpecs = new HuffmanSpec[4]
        {
            // Luminance DC
            new HuffmanSpec()
            {
                count = new byte[] 
                {
                    0, 1, 5, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0
                },
                symbol = new byte[] 
                {
                    0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11
                }
            },
            // Luminance AC
            new HuffmanSpec()
            {
                count = new byte[] 
                {
                    0, 2, 1, 3, 3, 2, 4, 3, 5, 5, 4, 4, 0, 0, 1, 125
                },
                symbol = new byte[]
                {
                    0x01, 0x02, 0x03, 0x00, 0x04, 0x11, 0x05, 0x12,
                    0x21, 0x31, 0x41, 0x06, 0x13, 0x51, 0x61, 0x07,
                    0x22, 0x71, 0x14, 0x32, 0x81, 0x91, 0xA1, 0x08,
                    0x23, 0x42, 0xB1, 0xC1, 0x15, 0x52, 0xD1, 0xF0,
                    0x24, 0x33, 0x62, 0x72, 0x82, 0x09, 0x0A, 0x16,
                    0x17, 0x18, 0x19, 0x1A, 0x25, 0x26, 0x27, 0x28,
                    0x29, 0x2A, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
                    0x3A, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49,
                    0x4A, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59,
                    0x5A, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69,
                    0x6A, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79,
                    0x7A, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89,
                    0x8A, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98,
                    0x99, 0x9A, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7,
                    0xA8, 0xA9, 0xAA, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6,
                    0xB7, 0xB8, 0xB9, 0xBA, 0xC2, 0xC3, 0xC4, 0xC5,
                    0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xD2, 0xD3, 0xD4,
                    0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xE1, 0xE2,
                    0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA,
                    0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8,
                    0xF9, 0xFA,
                }
            },
            // Chrominance DC
            new HuffmanSpec()
            {
                count = new byte[] 
                {
                    0, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0  
                },
                symbol = new byte[] 
                {
                    0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11
                }
            },
            // Chrominance AC
            new HuffmanSpec()
            {
                count = new byte[] 
                {
                    0, 2, 1, 2, 4, 4, 3, 4, 7, 5, 4, 4, 0, 1, 2, 119
                },
                symbol = new byte[] 
                {
                    0x00, 0x01, 0x02, 0x03, 0x11, 0x04, 0x05, 0x21,
                    0x31, 0x06, 0x12, 0x41, 0x51, 0x07, 0x61, 0x71,
                    0x13, 0x22, 0x32, 0x81, 0x08, 0x14, 0x42, 0x91,
                    0xA1, 0xB1, 0xC1, 0x09, 0x23, 0x33, 0x52, 0xF0,
                    0x15, 0x62, 0x72, 0xD1, 0x0A, 0x16, 0x24, 0x34,
                    0xE1, 0x25, 0xF1, 0x17, 0x18, 0x19, 0x1A, 0x26,
                    0x27, 0x28, 0x29, 0x2A, 0x35, 0x36, 0x37, 0x38,
                    0x39, 0x3A, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                    0x49, 0x4A, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
                    0x59, 0x5A, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
                    0x69, 0x6A, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78,
                    0x79, 0x7A, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87,
                    0x88, 0x89, 0x8A, 0x92, 0x93, 0x94, 0x95, 0x96,
                    0x97, 0x98, 0x99, 0x9A, 0xA2, 0xA3, 0xA4, 0xA5,
                    0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xB2, 0xB3, 0xB4,
                    0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xC2, 0xC3,
                    0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xD2,
                    0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA,
                    0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9,
                    0xEA, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8,
                    0xF9, 0xFA,
                }
            },
        };
        
        // zigzagmap[i] is the index of the i'th element in zig-zag order
        // - from natural order (line by line) to zigzag
        public static readonly int[] ZigZagMap = new int[]
        {
            0, 1, 8, 16, 9, 2, 3, 10,
            17, 24, 32, 25, 18, 11, 4, 5,
            12, 19, 26, 33, 40, 48, 41, 34,
            27, 20, 13, 6, 7, 14, 21, 28,
            35, 42, 49, 56, 57, 50, 43, 36,
            29, 22, 15, 23, 30, 37, 44, 51,
            58, 59, 52, 45, 38, 31, 39, 46,
            53, 60, 61, 54, 47, 55, 62, 63,
        };

        public static int GetBitCount(int n)
        {
            int bc = 0;
            while (n != 0)
            {
                bc++;
                n >>= 1;
            }
            return bc;
        }
        
        # endregion

        private BinaryWriter writer;
        // jpeg quality of compression (1-100), 1 being lowest quality = highest compression ratio
        private int quality;
        // bits to be written to the stream
        private uint bits;
        // number of bits in bits
        private int nBits;
        // hidden file data to be hidden
        public byte[]? data;
        private int dataIdx;
        private byte dataByteMask = 1;
        // number of non-zero-or-one AC coefficients = number of bits that can be hidden
        public int capacityCounter { get; private set; } = 0;
        private HuffmanLookup[] huffmanLookups = new HuffmanLookup[4];

        // scaled quantization tables in zig-zag order
        private byte[][] QTables = new byte[2][]
        {
            new byte[64],
            new byte[64]
        };

        /// <summary>
        /// creates an instance of JPEGWriter which is used to perform jpeg huffman compression on 
        /// a 2D array of dct coefficients and hide a file in the image
        /// </summary>
        /// <param name="path"></param>
        /// <param name="quality"></param>
        public JPEGWriter(string? path=null, int quality=50)
        {
            if (path == null)
            {
                writer = new BinaryWriter(Console.OpenStandardOutput());
            }
            else
            {
                writer = new BinaryWriter(File.Open(path, FileMode.Create));
            }

            bits = 0;
            nBits = 0;
            // initialize all huffman lookups
            for (int i = 0; i < 4; i++)
            {
                huffmanLookups[i] = new HuffmanLookup(huffmanSpecs[i]);
            }

            // scale quantization tables according to (provided) quality parameter (1-100)
            if (quality < 1)
            {
                quality = 1;
            }
            else if (quality > 100)
            {
                quality = 100;
            }
            this.quality = quality;

            double scale = (quality < 50) ? (50.0 / quality) : ((100 - quality) / 50.0);
            for (int i = 0; i < QTables.Length; i++)
            {
                for (int j = 0; j < QTables[i].Length; j++)
                {
                    int q = (int)Math.Round(QuantizationTablesUnscaled[i][j] * scale);
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

        /// write the nBits least-significant of bits to the stream. Assuming nBits <= 16
        private void Emit(uint bits, int nBits)
        {
            this.nBits += nBits;
            bits <<= (32 - this.nBits);
            this.bits |= bits;
            while (this.nBits >= 8)
            {
                byte b = (byte)(this.bits >> 24);
                writer.Write(b);
                if (b == 0xFF)
                {
                    // escape 0xFF byte
                    writer.Write((byte)0x00);
                }
                this.bits <<= 8;
                this.nBits -= 8;
            }
        }

        // write the huffman code for a given symbol to the stream using compiled huffman lookup table
        private void EmitHuff(HuffmanLookup lookup, int symbol)
        {
            int code = lookup.huffmanCodes[symbol];
            int nBits = code >> 24;
            Emit((uint)code, nBits);
        }

        // write the huffman code for a run of `runLength` zeros and the number of bits of the next symbol and the symbol itself
        private void EmitHuffRLE(HuffmanLookup lookup, int symbol, int runLength)
        {
            int a = symbol;
            int b = symbol;
            if (symbol < 0)
            {
                a = -symbol;
                b = symbol - 1;
            }
            int nBits = GetBitCount(a);
            EmitHuff(lookup, runLength << 4 | nBits);
            if (nBits > 0)
            {
                Emit((uint)(b & ((1 << nBits) - 1)), nBits);
            }
        }

        /// <summary>
        /// perform quantization, zig-zag reordering and huffman encoding of a block of dct coefficients and write it to the stream.
        /// <br/>Is also used as a mock writer when calculating capacity
        /// </summary>
        /// <param name="block">2D array of unquantized  coefficients</param>
        /// <param name="component">luminance or chrominance</param>
        /// <param name="prevDC">DC coefficient of prevois block. We code their difference.</param>
        /// <param name="writingMode">if False, we are calculating capacity</param>
        /// <returns>DC coefficient of the block which is used in the next block</returns>
        private int WriteBlock(int[,] block, int component, int prevDC, bool writingMode=true)
        {
            int dc = (int)Math.Round(block[0, 0] / (QTables[component][0] * 1.0));
            if (writingMode)
                EmitHuffRLE(huffmanLookups[component * 2], dc - prevDC, 0);
            
            int runLength = 0;
            for (int zig = 1; zig < 64; zig++)
            {
                int z = ZigZagMap[zig];
                int ac = block[z / 8, z % 8];
                ac = (int)Math.Round(ac / (QTables[component][zig] * 1.0));

                // capacity estimation
                if (!writingMode)
                {
                    if (ac < -1 || ac > 1)
                    {
                        capacityCounter++;
                    }
                    continue;
                }
                
                // this is where the magic happens
                if ((ac < -1 || ac > 1))
                {
                    if (dataIdx < data!.Length)
                    {
                        bool neg = ac < 0;
                        bool bit = (data[dataIdx] & dataByteMask) != 0;
                        if (neg) 
                            ac = -ac;
                       
                        // set last bit to 1 if bit is True, 0 if bit is False
                        if (bit)
                            ac |= 1;
                        else
                            ac &= ~1;

                        if (neg) 
                            ac = -ac;

                        dataByteMask <<= 1;
                        if (dataByteMask == 0)
                        {
                            dataIdx++;
                            dataByteMask = 1;
                        }
                    }
                }

                if (ac == 0)
                {
                    runLength++;
                }
                else
                {
                    while (runLength > 15)
                    {
                        // 16 zeros special code
                        EmitHuff(huffmanLookups[component * 2 + 1], 0xF0);
                        runLength -= 16;
                    }
                    EmitHuffRLE(huffmanLookups[component * 2 + 1], ac, runLength);
                    runLength = 0;
                }
            }
            if (runLength > 0)
            {
                // End of block special huffman code
                EmitHuff(huffmanLookups[component * 2 + 1], 0x00);
            }
            return dc;
        }

        public void WriteSOI()
        {
            writer.Write(SOI);
        }

        public void WriteDQT()
        {
            writer.Write(DQT);
            
            ushort lengthDQT = 2 + 2 * (64 + 1);
            WriteLengthOfMarker(lengthDQT);

            // Luminance
            writer.Write((byte)0);
            writer.Write(QTables[0]);
            
            // Chrominance
            writer.Write((byte)1);
            writer.Write(QTables[1]);
        }

        public void WriteSOF0(int height, int width)
        {
            writer.Write(SOF0);
            int length = 8 + 3 * 3;
            WriteLengthOfMarker(length);
            writer.Write((byte)8); // 8-bit precision
            WriteLengthOfMarker(height);
            WriteLengthOfMarker(width);
            writer.Write((byte)3); // 3 components
            // Y
            writer.Write((byte)1);
            writer.Write((byte)0x11); // horizontal and vertical sampling factor = 1
            writer.Write((byte)0); // quantization table 0
            // Cb
            writer.Write((byte)2);
            writer.Write((byte)0x11); // horizontal and vertical sampling factor = 1
            writer.Write((byte)1); // quantization table 1
            // Cr
            writer.Write((byte)3);
            writer.Write((byte)0x11); // horizontal and vertical sampling factor = 1
            writer.Write((byte)1); // quantization table 1

            // // greyscale
            // writer.Write(SOF0);
            // WriteLengthOfMarker(8 + 3);
            // writer.Write((byte)8); // 8-bit precision
            // WriteLengthOfMarker(height);
            // WriteLengthOfMarker(width);
            // writer.Write((byte)1); // 1 component
            // writer.Write((byte)1); // Y
            // writer.Write((byte)0x11); // horizontal and vertical sampling factor = 1
            // writer.Write((byte)0); // quantization table 0

        }

        // write the define huffman table marker and the huffman tables
        public void WriteDHT()
        {
            // 0 L  DC -> 00
            // 1 L  AC -> 10
            // 2 Ch DC -> 01
            // 3 Ch AC -> 11
            writer.Write(DHT);
            int length = 2;
            foreach (var ht in huffmanSpecs)
            {
                length += 1 + 16 + ht.symbol.Length;
            }
            WriteLengthOfMarker(length);
            // Th - table destination identifier - 0 = luminance, 1 = chrominance
            for (int i = 0; i < 2; i++)
            {
                // Tc - table class - 0 = DC, 1 = AC
                for (int j = 0; j < 2; j++)
                {
                    writer.Write((byte)(j << 4 | i));
                    writer.Write(huffmanSpecs[i * 2 + j].count);
                    writer.Write(huffmanSpecs[i * 2 + j].symbol);
                }
            }
        }

        public void WriteSOSHeader()
        {
            writer.Write(SOS);
            int length = 6 + 2 * 3;
            WriteLengthOfMarker(length);
            writer.Write((byte)3); // 3 components
            // Y
            writer.Write((byte)1);
            writer.Write((byte)0x00); // DC = 0, AC = 0
            // Cb
            writer.Write((byte)2);
            writer.Write((byte)0x11); // DC = 1, AC = 1
            // Cr
            writer.Write((byte)3);
            writer.Write((byte)0x11); // DC = 1, AC = 1
            writer.Write((byte)0); // first spectral coefficient
            writer.Write((byte)63); // last spectral coefficient
            writer.Write((byte)0); // successive approximation bit position
        }

        /// <summary>
        /// Write the huffman coded data to stream. <br/>
        /// Sequentially write the Y, Cb, Cr block of all 8x8 blocks of pixels.<br/>
        /// If writingMode is False, we are calculating capacity.
        /// </summary>
        /// <param name="dctCoefficients">unquantized DCT coefficients of the whole image</param>
        /// <param name="writingMode">False if calculating capacity</param>
        /// <exception cref="Exception">thrown if capacity of image is too small to hide the file</exception>
        public void WriteSOSScanData(dctCoeffs[,] dctCoefficients, bool writingMode=true)
        {
            dctCoeffs prevDC = new dctCoeffs();
            for (int j = 0; j < dctCoefficients.GetLength(1); j += BlockSize)
            {
                for (int i = 0; i < dctCoefficients.GetLength(0); i += BlockSize)
                {
                    var dctCoefficientsY = new int[BlockSize, BlockSize];
                    var dctCoefficientsCb = new int[BlockSize, BlockSize];
                    var dctCoefficientsCr = new int[BlockSize, BlockSize];
                    for (int k = 0; k < BlockSize; k++)
                    {
                        for (int l = 0; l < BlockSize; l++)
                        {
                            dctCoefficientsY[k, l] = dctCoefficients[i + k, j + l].Y;
                            dctCoefficientsCb[k, l] = dctCoefficients[i + k, j + l].Cb;
                            dctCoefficientsCr[k, l] = dctCoefficients[i + k, j + l].Cr;
                        }
                    }
                    prevDC.Y  = WriteBlock(dctCoefficientsY, 0, prevDC.Y, writingMode);
                    prevDC.Cb = WriteBlock(dctCoefficientsCb, 1, prevDC.Cb, writingMode);
                    prevDC.Cr = WriteBlock(dctCoefficientsCr, 1, prevDC.Cr, writingMode);
                }
            }
            if (writingMode)
            {
                if (dataIdx < data!.Length)
                {
                    throw new Exception("Not enough capacity to hide the file. Try using a larger image or a smaller file.");
                }
                Emit(0xFF, 7);
            }
        }
        
        public void WriteEOI()
        {
            writer.Write(EOI);
        }

        public void FlushAndClose()
        {
            writer.Flush();
            writer.Close();
        }

        public void Dispose()
        {
            writer.Dispose();
        }

        private void WriteLengthOfMarker(int length)
        {
            // write the int in big endian as bytes
            writer.Write((byte)(length >> 8));
            writer.Write((byte)length);
        }
    }
}
