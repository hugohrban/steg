using System;
using System.Drawing;

namespace Steganography
{
    public class JStegImage //: IStegImage
    {
        # region static stuff
        public void Extract()
        {
            //writer.RevealFile(quantized);
            JPEGExtractor extr = new JPEGExtractor(imagePath);
            extr.ReadFile();
            
        }
        public static int[] DCT2(int[] block)
        {
            // int[,] block = new int[8,8];
            // for (int i = 0; i < 8; i++)
            // {
            //     for (int j=0; j < 8; j++)
            //     {
            //         block[i,j] = b[i*8+j];
            //     }
            // }
            double alpha(int u)
            {
                if (u == 0)
                {
                    return 1 / Math.Sqrt(2);
                }
                else
                {
                    return 1;
                }
            }

            // if (block.GetLength(0) != 8 || block.GetLength(1) != 8)
            // {
            //     throw new ArgumentException("Block must be 8x8");
            // }

            int[] dct = new int[8 * 8];

            for (int u = 0; u < 8; u++)
            {
                for (int v = 0; v < 8; v++)
                {
                    double sum = 0;
                    for (int x = 0; x < 8; x++)
                    {
                        for (int y = 0; y < 8; y++)
                        {
                            sum += block[x * 8 + y] * Math.Cos((2 * x + 1) * u * Math.PI / 16) * Math.Cos((2 * y + 1) * v * Math.PI / 16);
                        }
                    }
                    dct[u * 8 + v] = (int)Math.Round((0.25 * alpha(u) * alpha(v) * sum));
                }
            }
            // return Flatten(dct);
            return dct;
        }
        public static int[] fdct(int[] b)
        {
            int fix_0_298631336 = 2446;
            int fix_0_390180644 = 3196;
            int fix_0_541196100 = 4433;
            int fix_0_765366865 = 6270;
            int fix_0_899976223 = 7373;
            int fix_1_175875602 = 9633;
            int fix_1_501321110 = 12299;
            int fix_1_847759065 = 15137;
            int fix_1_961570560 = 16069;
            int fix_2_053119869 = 16819;
            int fix_2_562915447 = 20995;
            int fix_3_072711026 = 25172;
            
            int constBits     = 13;
            int pass1Bits     = 2;
            int centerJSample = 128;

            for (int y = 0; y < 8; y++)
            {
                int x0 = b[y*8+0];
                int x1 = b[y*8+1];
                int x2 = b[y*8+2];
                int x3 = b[y*8+3];
                int x4 = b[y*8+4];
                int x5 = b[y*8+5];
                int x6 = b[y*8+6];
                int x7 = b[y*8+7];

                int tmp0 = x0 + x7;
                int tmp1 = x1 + x6;
                int tmp2 = x2 + x5;
                int tmp3 = x3 + x4;

                int tmp10 = tmp0 + tmp3;
                int tmp12 = tmp0 - tmp3;
                int tmp11 = tmp1 + tmp2;
                int tmp13 = tmp1 - tmp2;

                tmp0 = x0 - x7;
                tmp1 = x1 - x6;
                tmp2 = x2 - x5;
                tmp3 = x3 - x4;

                b[y*8+0] = (tmp10 + tmp11 - 8*centerJSample) << pass1Bits;
		        b[y*8+4] = (tmp10 - tmp11) << pass1Bits;
		        int z1 = (tmp12 + tmp13) * fix_0_541196100;
		        z1 += 1 << (constBits - pass1Bits - 1);
		        b[y*8+2] = (z1 + tmp12*fix_0_765366865) >> (constBits - pass1Bits);
		        b[y*8+6] = (z1 - tmp13*fix_1_847759065) >> (constBits - pass1Bits);

                tmp10 = tmp0 + tmp3;
		        tmp11 = tmp1 + tmp2;
		        tmp12 = tmp0 + tmp2;
		        tmp13 = tmp1 + tmp3;
		        z1 = (tmp12 + tmp13) * fix_1_175875602;
		        z1 += 1 << (constBits - pass1Bits - 1);
		        tmp0 = tmp0 * fix_1_501321110;
		        tmp1 = tmp1 * fix_3_072711026;
		        tmp2 = tmp2 * fix_2_053119869;
		        tmp3 = tmp3 * fix_0_298631336;
		        tmp10 = tmp10 * -fix_0_899976223;
		        tmp11 = tmp11 * -fix_2_562915447;
		        tmp12 = tmp12 * -fix_0_390180644;
		        tmp13 = tmp13 * -fix_1_961570560;
        
		        tmp12 += z1;
		        tmp13 += z1;
		        b[y*8+1] = (tmp0 + tmp10 + tmp12) >> (constBits - pass1Bits);
		        b[y*8+3] = (tmp1 + tmp11 + tmp13) >> (constBits - pass1Bits);
		        b[y*8+5] = (tmp2 + tmp11 + tmp12) >> (constBits - pass1Bits);
		        b[y*8+7] = (tmp3 + tmp10 + tmp13) >> (constBits - pass1Bits);
            }

            for (int x = 0; x < 8; x++) {
                int tmp0 = b[0*8+x] + b[7*8+x];
                int tmp1 = b[1*8+x] + b[6*8+x];
                int tmp2 = b[2*8+x] + b[5*8+x];
                int tmp3 = b[3*8+x] + b[4*8+x];
 
                int tmp10 = tmp0 + tmp3 + 1<<(pass1Bits-1);
                int tmp12 = tmp0 - tmp3;
                int tmp11 = tmp1 + tmp2;
                int tmp13 = tmp1 - tmp2;

                tmp0 = b[0*8+x] - b[7*8+x];
                tmp1 = b[1*8+x] - b[6*8+x];
                tmp2 = b[2*8+x] - b[5*8+x];
                tmp3 = b[3*8+x] - b[4*8+x];

                b[0*8+x] = (tmp10 + tmp11) >> pass1Bits;
                b[4*8+x] = (tmp10 - tmp11) >> pass1Bits;

                int z1 = (tmp12 + tmp13) * fix_0_541196100;
                z1 += 1 << (constBits + pass1Bits - 1);
                b[2*8+x] = (z1 + tmp12*fix_0_765366865) >> (constBits + pass1Bits);
                b[6*8+x] = (z1 - tmp13*fix_1_847759065) >> (constBits + pass1Bits);

                tmp10 = tmp0 + tmp3;
                tmp11 = tmp1 + tmp2;
                tmp12 = tmp0 + tmp2;
                tmp13 = tmp1 + tmp3;
                z1 = (tmp12 + tmp13) * fix_1_175875602;
                z1 += 1 << (constBits + pass1Bits - 1);
                tmp0 = tmp0 * fix_1_501321110;
                tmp1 = tmp1 * fix_3_072711026;
                tmp2 = tmp2 * fix_2_053119869;
                tmp3 = tmp3 * fix_0_298631336;
                tmp10 = tmp10 * -fix_0_899976223;
                tmp11 = tmp11 * -fix_2_562915447;
                tmp12 = tmp12 * -fix_0_390180644;
                tmp13 = tmp13 * -fix_1_961570560;

                tmp12 += z1;
                tmp13 += z1;
                b[1*8+x] = (tmp0 + tmp10 + tmp12) >> (constBits + pass1Bits);
                b[3*8+x] = (tmp1 + tmp11 + tmp13) >> (constBits + pass1Bits);
                b[5*8+x] = (tmp2 + tmp11 + tmp12) >> (constBits + pass1Bits);
                b[7*8+x] = (tmp3 + tmp10 + tmp13) >> (constBits + pass1Bits);
            }
            return b;
        }
        
        public static int[] Flatten(int[,] arr)
        {
            int height = arr.GetLength(0);
            int width = arr.GetLength(1);
            int[] flat = new int[height * width];
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j <width; j++)
                {
                    flat[i * width + j] = arr[i, j];
                }
            }
            return flat;
        }

        public static int[] Quantize(int[] dctCoeffs, int tableIx)
        {
            int[] quantized = new int[8 * 8];
            byte[] quantizationTable = JPEGWriter.QuantizationTablesUnscaled[tableIx];

            for (int i = 0; i < 64; i++)
            {
                    quantized[i] = (int)Math.Round(dctCoeffs[i] / (quantizationTable[i]) * 0.3);
                    //quantized[i, j] = (int)Math.Round(dctCoeffs[i, j] / 1);
            }
            return quantized;
        }

        private static YCbCrColor RGBtoYCbCr(Color c)
        {
            YCbCrColor yCbCr = new YCbCrColor();
            yCbCr.Y = (byte)(0.299 * c.R + 0.587 * c.G + 0.114 * c.B);
            yCbCr.Cb = (byte)(-0.1687 * c.R - 0.3313 * c.G + 0.5 * c.B + 128);
            yCbCr.Cr = (byte)(0.5 * c.R - 0.4187 * c.G - 0.0813 * c.B + 128);
            return yCbCr;
        }

        #endregion
        private Bitmap coverImage;
        private YCbCrColor[,] pixels;
        //private JPEGWriter writer;
        private dctCoeffs[,] quantized;
        public byte[] hfData {get; private set;}
        public int height {get; private set;}
        public int width {get; private set;}
        private string imagePath;
        public JStegImage(string imagePath)
        {
            coverImage = new Bitmap(imagePath);
            width = coverImage.Width / 8 * 8;
            height = coverImage.Height / 8 * 8;
            pixels = new YCbCrColor[height, width];
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    pixels[i,j] = RGBtoYCbCr(coverImage.GetPixel(j, i));
                }
            }
            coverImage.Dispose();
            this.imagePath = imagePath;

            hfData = Array.Empty<byte>();
            
            quantized = new dctCoeffs[width, height];
            ComputeQuantization();
        }

        public void Hide(HiddenFile hiddenFile)
        {
            hfData = hiddenFile.data;
        }

        public void Write(string? outImagePath=null)
        {
            // output to stdout for now
            JPEGWriter writer = new JPEGWriter(outImagePath, 50);
            writer.data = hfData;

            writer.WriteSOI();
            writer.WriteDQT();
            writer.WriteSOF0(height, width);
            writer.WriteDHT();
            writer.WriteSOSHeader();
            writer.WriteSOSScanData(quantized);
            writer.WriteEOI();
            writer.FlushAndClose();
        }

        private void ComputeQuantization()
        {
            // TODO make parallel
            for (int x = 0; x < width; x += 8)
            {
                for (int y = 0; y < height; y += 8)
                {
                    GetDCTCoeffs(x, y);
                }
            }
        }

        public void PrintCapacity()
        {
            JPEGWriter writer = new JPEGWriter(null, 50);
            writer.WriteSOSScanData(quantized, false);
        }
        private void GetDCTCoeffs(int x, int y)
        {
            int[] shiftedY  = new int[8 * 8];
            int[] shiftedCb = new int[8 * 8];
            int[] shiftedCr = new int[8 * 8];

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    shiftedY[i * 8 + j]  = (pixels[y + i, x + j].Y - 128);
                    //System.Console.Write(pixels[y + i, x + j].Cb + " ");
                    shiftedCb[i * 8 + j] = (pixels[y + i, x + j].Cb - 128);
                    shiftedCr[i * 8 + j] = (pixels[y + i, x + j].Cr - 128);
                }
            }
            //System.Console.WriteLine("\n");
            // double[,] dctY = DCT2(shiftedY);
            // double[,] dctCb = DCT2(shiftedCb);
            // double[,] dctCr = DCT2(shiftedCr);

            // static int[] shorten(long[] arr)
            // {
            //     int[] shortArr = new int[arr.Length];
            //     for (int i = 0; i < arr.Length; i++)
            //     {
            //         shortArr[i] = (int)arr[i];
            //     }
            //     return shortArr;
            // }

            // int[] dctY  = fdct(Flatten(shiftedY));
            // int[] dctCb = fdct(Flatten(shiftedCb));
            // int[] dctCr = fdct(Flatten(shiftedCr));

            int[] dctY  = DCT2(shiftedY);
            int[] dctCb = DCT2(shiftedCb);
            int[] dctCr = DCT2(shiftedCr);

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    //System.Console.Write(dctCb[i * 8 + j] + " ");
                }
            }

            // int[] quantizedY = Quantize(dctY, 0);
            // int[] quantizedCb = Quantize(dctCb, 1);
            // int[] quantizedCr = Quantize(dctCr, 1);

            int[] quantizedY = dctY;
            int[] quantizedCb = dctCb;
            int[] quantizedCr = dctCr;

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    // System.Console.Write(quantizedCb[i * 8 + j] + " ");
                    quantized[x+i,y+j] = new dctCoeffs() {
                        Y = quantizedY[i * 8 + j],
                        Cb = quantizedCb[i * 8 + j],
                        Cr = quantizedCr[i * 8 + j]
                    };
                }
            }
            //System.Console.WriteLine("\n");
        }
    }
}