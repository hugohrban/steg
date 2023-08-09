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

        public void Write(string? outImagePath=null, int quality=50)
        {
            // output to stdout for now
            JPEGWriter writer = new JPEGWriter(outImagePath, quality);
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
                    shiftedY[i * 8 + j]  = (pixels[y + i, x + j].Y  - 128);
                    shiftedCb[i * 8 + j] = (pixels[y + i, x + j].Cb - 128);
                    shiftedCr[i * 8 + j] = (pixels[y + i, x + j].Cr - 128);
                }
            }

            int[] quantizedY  = DCT2(shiftedY);
            int[] quantizedCb = DCT2(shiftedCb);
            int[] quantizedCr = DCT2(shiftedCr);

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    quantized[x+i,y+j] = new dctCoeffs() {
                        Y  = quantizedY[i * 8 + j],
                        Cb = quantizedCb[i * 8 + j],
                        Cr = quantizedCr[i * 8 + j]
                    };
                }
            }
        }
    }
}