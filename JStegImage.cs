using System;
using System.Drawing;
using System.Threading.Tasks;

namespace Steganography
{
    public class JStegImage : IStegImage
    {
        # region static stuff

        /// <summary>
        /// Extracts a file from a jpeg image file. The file must have been embedded in the image using the `jsteg` method.
        /// This method creates an instance of JPEGExtractor and calls its ReadFile method.
        /// </summary>
        /// <param name="imagePath"></param>
        public static void Extract(string imagePath)
        {
            JPEGExtractor extractor = new JPEGExtractor(imagePath);
            System.Console.WriteLine($"Extracting file from {imagePath}...");
            extractor.ExtractFile();
        }

        /// <summary>
        /// Simple computation of forward discrete cosine transform based on the explicit formula.
        /// </summary>
        /// <param name="block">8x8 block (unshifted)</param>
        /// <returns>dct coefficients of the block in natural (line-by-line) order</returns>
        public static int[] DCT2(int[] block)
        {
            double alpha(int u) => (u == 0) ? 1 / Math.Sqrt(2) : 1;

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
                            sum += (block[x * 8 + y] - 128) * Math.Cos((2 * x + 1) * u * Math.PI / 16) * Math.Cos((2 * y + 1) * v * Math.PI / 16);
                        }
                    }
                    dct[u * 8 + v] = (int)Math.Round((0.25 * alpha(u) * alpha(v) * sum));
                }
            }
            return dct;
        }

        /// <summary>
        /// Fast implementation of forward discrete cosine transform. 
        /// </summary>
        /// <param name="b">8x8 block (unshifted)</param>
        /// <returns>dct coefficients of the block in natural (line-by-line) order</returns>
        /// <remarks>
        /// Source: http://www.ijg.org/files/jpegsrc.v8c.tar.gz
        /// </remarks>
        public static int[] fdctint(int[] b)
        {
            // trigonometric constants in fixed point
            const int fix_0_298631336 = 2446;
            const int fix_0_390180644 = 3196;
            const int fix_0_541196100 = 4433;
            const int fix_0_765366865 = 6270;
            const int fix_0_899976223 = 7373;
            const int fix_1_175875602 = 9633;
            const int fix_1_501321110 = 12299;
            const int fix_1_847759065 = 15137;
            const int fix_1_961570560 = 16069;
            const int fix_2_053119869 = 16819;
            const int fix_2_562915447 = 20995;
            const int fix_3_072711026 = 25172;

            const int constBits     = 13;
            const int pass1Bits     = 2;
            const int centerJSample = 128;

            // process rows
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

                b[y*8+0] = (tmp10 + tmp11 - 8 * centerJSample) << pass1Bits;
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
		        tmp10 = tmp10 * (-fix_0_899976223);
		        tmp11 = tmp11 * (-fix_2_562915447);
		        tmp12 = tmp12 * (-fix_0_390180644);
		        tmp13 = tmp13 * (-fix_1_961570560);

		        tmp12 += z1;
		        tmp13 += z1;
		        b[y*8+1] = (tmp0 + tmp10 + tmp12) >> (constBits - pass1Bits);
		        b[y*8+3] = (tmp1 + tmp11 + tmp13) >> (constBits - pass1Bits);
		        b[y*8+5] = (tmp2 + tmp11 + tmp12) >> (constBits - pass1Bits);
		        b[y*8+7] = (tmp3 + tmp10 + tmp13) >> (constBits - pass1Bits);
            }

            // process columns
            for (int x = 0; x < 8; x++) {
                int tmp0 = b[0*8+x] + b[7*8+x];
                int tmp1 = b[1*8+x] + b[6*8+x];
                int tmp2 = b[2*8+x] + b[5*8+x];
                int tmp3 = b[3*8+x] + b[4*8+x];

                int tmp10 = tmp0 + tmp3 + (1<<(pass1Bits-1));
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
                z1 += (1 << (constBits + pass1Bits - 1));
                b[2*8+x] = (z1 + tmp12*fix_0_765366865) >> constBits + pass1Bits;
                b[6*8+x] = (z1 - tmp13*fix_1_847759065) >> constBits + pass1Bits;

                tmp10 = tmp0 + tmp3;
                tmp11 = tmp1 + tmp2;
                tmp12 = tmp0 + tmp2;
                tmp13 = tmp1 + tmp3;
                z1 = (tmp12 + tmp13) * fix_1_175875602;
                z1 += (1 << (constBits + pass1Bits - 1));
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
                b[1*8+x] = (tmp0 + tmp10 + tmp12) >> constBits + pass1Bits;
                b[3*8+x] = (tmp1 + tmp11 + tmp13) >> constBits + pass1Bits;
                b[5*8+x] = (tmp2 + tmp11 + tmp12) >> constBits + pass1Bits;
                b[7*8+x] = (tmp3 + tmp10 + tmp13) >> constBits + pass1Bits;
            }

            // scale down the coefficients by 8
            for (int i = 0; i < b.Length; i++)
            {
                b[i] >>= 3;
            }
            return b;
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

        private YCbCrColor[,] pixels;
        private dctCoeffs[,] dctCoefficients;
        public byte[] hfData {get; private set;}
        public int height {get; private set;}
        public int width {get; private set;}
        private string imagePath;
        private const int blockSize = 8;
        private int quality;
        private string? outImagePath;

        /// <summary>
        /// Creates a JStegImage object from a jpeg image file. <br/>
        /// Computes DCT coefficients of the image, which are used in the jpeg encoding
        /// process and can be used to hide data in the image.  <br/>
        /// JSteg algorithm:<br/>
        ///    1. Compute DCT coefficients for all 8x8 blocks in the image. <br/>
        ///    2. Hide the data in the least-significant bits of AC coefficients of the DCT, 
        ///         whose abs. value is greater than 1. <br/>
        ///    3. Save the image in the jpeg format. <br/>
        /// </summary>
        /// <param name="imagePath"></param>
        public JStegImage(string imagePath, int quality=50, string? outImagePath=null)
        {
            using (Bitmap coverImage = new Bitmap(imagePath))
            {            
                width = coverImage.Width / blockSize * blockSize;
                height = coverImage.Height / blockSize * blockSize;
                pixels = new YCbCrColor[height, width];
                
                // parallelized conversion from RGB to YCbCr
                int threadCount = Environment.ProcessorCount;
                int rowsPerThread = height / threadCount;
                Task[] tasks = new Task[threadCount];
                for (int i = 0; i < threadCount; i++)
                {
                    int startRow = i * rowsPerThread;
                    int endRow = (i == threadCount - 1) ? height : (i + 1) * rowsPerThread;
                    tasks[i] = Task.Run(() => {
                        for (int row = startRow; row < endRow; row++)
                        {
                            for (int col = 0; col < width; col++)
                            {
                                pixels[row, col] = RGBtoYCbCr(coverImage.GetPixel(col, row));
                            }
                        }
                    });
                }
                Task.WaitAll(tasks);
            }
            
            this.imagePath = imagePath;
            hfData = Array.Empty<byte>();
            this.quality = quality;
            this.outImagePath = outImagePath;

            dctCoefficients = new dctCoeffs[width, height];
            ComputeDCTParallel();
        }

        /// <summary>
        /// Hide a file in the image and save the result as a new jpg image.
        /// </summary>
        /// <param name="hiddenFile"></param>
        public void Hide(string filePath)
        {
            System.Console.WriteLine($"Hiding file {filePath} in {imagePath}");
            HiddenFile hiddenFile = new HiddenFile(filePath);
            hfData = hiddenFile.data;
            string stegImagePath = (outImagePath is not null) ? outImagePath : "steg_" + Path.GetFileName(imagePath);
            Write(stegImagePath);
        }

        /// <summary>
        /// Prints the capacity of the image for different quality factors from 100 to 5, with step 5.
        /// Computation is performed in parallel.
        /// </summary>
        public void PrintCapacity()
        {
            // we create a write for each quality setting and call the WriteSOSScanData method with 
            // the quantized coefficients, and writeMode set to false, so that the writer doesn't actually write anything
            // after "writing" we get the capacity from the writer's capacityCounter field
            int[] capacitiesBits = new int[20];
            Parallel.For(0, 20, i => {
                JPEGWriter writer = new JPEGWriter(null, 100 - i * 5);
                writer.WriteSOSScanData(dctCoefficients, false);
                capacitiesBits[i] = writer.capacityCounter;
                writer.FlushAndClose();
                writer.Dispose();
            });

            for (int i = 0; i < 20; i++)
            {
                int Q = 100 - i * 5;
                int capacityB = capacitiesBits[i] / 8;
                int capacityKB = capacityB / 1024;
                Console.WriteLine($"Capacity using `jsteg` method with Q={Q}: {capacityB} B = {capacityKB} KB");
            }
            
            // synchronous version
            // JPEGWriter writer;    
            // for (int Q = 100; Q > 0; Q -= 5)
            // {
            //     writer = new JPEGWriter(null, Q);
            //     writer.WriteSOSScanData(dctCoefficients, false);
            //     int capacityB = writer.capacityCounter / 8;
            //     int capacityKB = capacityB / 1024;
            //     Console.WriteLine($"Capacity using `jsteg` method with Q={Q}: {capacityB} B = {capacityKB} KB");
            // }
        }

        /// <summary>
        /// Compress the image and save the result as a new jpg image. 
        /// The quality setting is chosen when creating the JStegImage object.
        /// </summary>
        /// <param name="outImagePath"></param>
        public void Compress(string outImagePath)
        {
            // this method is just a wrapper around the Write method, 
            // so that users don't call Write directly by accident, since .Hide() does it already
            Write(outImagePath);
        }
        
        /// <summary>
        /// Write the image to a jpg file. If no path is specified, the image will be written to stdout.
        /// </summary>
        /// <param name="outImagePath"></param>
        /// <param name="quality">JPEG compression quality setting (from 1 to 100) </param>
        private void Write(string? outImagePath=null)
        {
            JPEGWriter writer = new JPEGWriter(outImagePath, quality);
            writer.data = hfData;

            writer.WriteSOI();
            writer.WriteDQT();
            writer.WriteSOF0(height, width);
            writer.WriteDHT();
            writer.WriteSOSHeader();
            writer.WriteSOSScanData(dctCoefficients);
            writer.WriteEOI();
            writer.FlushAndClose();
            writer.Dispose();
            System.Console.WriteLine($"Image written to {outImagePath}");
        }

        /// <summary>
        /// Compute DCT coefficients for all 8x8 blocks in the image. (Synchronous version = slow)
        /// </summary>
        private void ComputeDCT()
        {
            System.Console.WriteLine("Computing DCT coefficients...");
            for (int x = 0; x < width; x += blockSize)
            {
                for (int y = 0; y < height; y += blockSize)
                {
                    GetDCTCoeffs(x, y);
                }
            }
            System.Console.WriteLine("DCT coefficients computed.");
        }

        /// <summary>
        /// Compute DCT coefficients for all 8x8 blocks in the image parallelly.
        /// </summary>
        private void ComputeDCTParallel()
        {
            System.Console.WriteLine("Computing DCT coefficients...");
            int blocksWidth = width / blockSize;
            int blocksHeight = height / blockSize;
            int blocksCount = blocksWidth * blocksHeight;
            int threadsCount = Environment.ProcessorCount;
            int blocksPerThread = blocksCount / threadsCount;

            Task[] tasks = new Task[threadsCount];
            for (int i = 0; i < threadsCount; i++)
            {
                int startBlock = i * blocksPerThread;
                int endBlock = (i == threadsCount - 1) ? blocksCount : (i + 1) * blocksPerThread;
                tasks[i] = Task.Run(() => {
                    for (int block = startBlock; block < endBlock; block++)
                    {
                        int x = block % blocksWidth * blockSize; 
                        int y = block / blocksWidth * blockSize;
                        GetDCTCoeffs(x, y);
                    }
                });
            }
            Task.WaitAll(tasks);
            System.Console.WriteLine("DCT coefficients computed.");
        }
        
        /// <summary>
        /// Computes DCT coefficients for a block of 8x8 pixels starting at (x,y) absolute coordinates 
        /// and updates the `quantized` array.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void GetDCTCoeffs(int x, int y)
        {
            int[] blockY  = new int[8 * 8];
            int[] blockCb = new int[8 * 8];
            int[] blockCr = new int[8 * 8];

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    blockY[i * 8 + j]  = pixels[y + i, x + j].Y;
                    blockCb[i * 8 + j] = pixels[y + i, x + j].Cb;
                    blockCr[i * 8 + j] = pixels[y + i, x + j].Cr;
                }
            }

            // SLOW - directly implementing the DCT equation
            int[] DCT_Y  = DCT2(blockY);
            int[] DCT_Cb = DCT2(blockCb);
            int[] DCT_Cr = DCT2(blockCr);

            // FAST
            // int[] DCT_Y  = fdctint(blockY);
            // int[] DCT_Cb = fdctint(blockCb);
            // int[] DCT_Cr = fdctint(blockCr);

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    dctCoefficients[x+i,y+j] = new dctCoeffs() {
                        Y  = DCT_Y[i * 8 + j],
                        Cb = DCT_Cb[i * 8 + j],
                        Cr = DCT_Cr[i * 8 + j]
                    };
                }
            }
        }
    }
}