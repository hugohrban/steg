using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace Steganography
{
    public class LSbImage : IStegImage
    {
        public Color[] pixels { get; private set; }
        private string imgPath;
        private int width;
        private int height;
        private int bitsPerByte;
        private string? outImagePath;

        /// <summary>
        /// Representation of an image bitmap. Reads the image from disk and stores it in an array of Color objects.
        /// </summary>
        /// <param name="imgPath"></param>
        public LSbImage(string imgPath, int bitsPerByte=1, string? outImagePath=null)
        {
            using (Bitmap coverImage = new Bitmap(imgPath))
            {
                this.width = coverImage.Width;
                this.height = coverImage.Height;
                this.imgPath = Path.GetFileName(imgPath);
                
                pixels = new Color[coverImage.Width * coverImage.Height];
                GetPixels(coverImage);
                
                this.bitsPerByte = bitsPerByte;
                this.outImagePath = outImagePath;

                if (bitsPerByte < 1 || bitsPerByte > 8)
                {
                    throw new Exception("invalid bitsPerByte value. Must be between 1 and 8.");
                }
            }
        }

        /// <summary>
        /// Hide a hiffenFile object in the least-significant bits of pixels of the image and save it to disk.
        /// </summary>
        /// <param name="hf"></param>
        /// <exception cref="InvalidDataException"></exception>
        public void Hide(string filePath)
        {
            HiddenFile hf = new HiddenFile(filePath, bitsPerByte);
            byte[] arr = hf.data;
            
            if (arr.Length > pixels.Length * 4 * bitsPerByte / 8)
            {
                throw new InvalidDataException("The image is too small to be able to contain the hidden file. Try higher bitsPerByte value or a larger image.");
            }

            if (bitsPerByte > 1)
            {
                // hide array single-threaded
                HideArrayTask(hf.data, 0, hf.data.Length);
            }
            else
            {
                // hiding array in 1 bit per byte encoding in parallel
                int threadCount = Environment.ProcessorCount;
                int chunkSize = arr.Length / threadCount;

                // start/end indices for which portion of arr each thread will hide
                int start = 0;
                int end = 0;
                Task[] tasks = new Task[threadCount];

                for (int i = 0; i < threadCount; i++)
                {
                    start = end;
                    end = start + chunkSize;
                    if (i == threadCount - 1)
                    {
                        end = arr.Length;
                    }
                    int localStart = start;
                    int localEnd = end;
                    tasks[i] = Task.Run(() => HideArrayTask(arr, localStart, localEnd));
                }

                // Wait for all tasks to finish
                Task.WaitAll(tasks);
            }
            Write();
        }

        /// <summary>
        /// Print the capacity of the image in bytes and kilobytes for different values of bitsPerByte.
        /// </summary>
        public void PrintCapacity()
        {
            System.Console.WriteLine($"Capacities for image {Path.GetFileName(imgPath)} using `lsb` method:");
            int nPixels = pixels.Length;
            int nChannels = 4;
            for (int i = 1; i <= 8; i++)
            {
                int nBits = nPixels * nChannels * i;
                int nBytes = nBits / 8;
                int nKBytes = nBytes / 1024;
                Console.WriteLine($"bitsPerByte: {i}, capacity: {nBytes} B = {nKBytes} kB");
            }
        }

        /// <summary>
        /// Get all pixels from a bitmap as an array of Color objects, line by line
        /// </summary>
        private void GetPixels(Bitmap bitmap)
        {
            pixels = new Color[bitmap.Width * bitmap.Height];
            int threadCount = Environment.ProcessorCount;
            int chunkSize = pixels.Length / threadCount;
            int start = 0;
            int end = 0;
            Task[] tasks = new Task[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                start = end;
                end = start + chunkSize;
                if (i == threadCount - 1)
                {
                    end = pixels.Length;
                }
                int localStart = start;
                int localEnd = end;
                tasks[i] = Task.Run(() => {
                    for (int j = localStart; j < localEnd; j++)
                    {
                        pixels[j] = bitmap.GetPixel(j % bitmap.Width, j / bitmap.Width);
                    }
                });
            }
            Task.WaitAll(tasks);
        }

        /// <summary>
        /// Write the image to disk. This method is called after hiding a file in the image using .Hide() method.
        /// </summary>
        private void Write()
        {
            Console.WriteLine("Writing LSb-steg image to disk...");
            Bitmap stegImage = new Bitmap(width, height);
            for (int i = 0; i < pixels.Length; i++)
            {
                int x = i % stegImage.Width;
                int y = i / stegImage.Width;
                stegImage.SetPixel(x, y, pixels[i]);
            }
            string stegImageName = (outImagePath is not null) ? outImagePath : ("steg_" + Path.GetFileNameWithoutExtension(imgPath) + ".png");
            stegImage.Save(stegImageName, ImageFormat.Png);
            System.Console.WriteLine($"Writing done. Image saved as {stegImageName}");
        }

        private void HideArrayTask(byte[] arr, int dataStartIndex, int dataEndIndex)
        {
            byte bufferMask = 1;
            int dataIx = dataStartIndex;
            byte buffer = arr[dataIx];
            bool bit;
            int bitMask;
            int startPixel = dataStartIndex;
            int endPixel = pixels.Length;
            if (bitsPerByte == 1)
            {
                // 1 byte of data is stored in every 2 pixels
                startPixel = 2 * dataStartIndex;
                endPixel = 2 * dataEndIndex;
            }
        
            // cycle through pixels
            for (int i = startPixel; i < endPixel; i++)
            {
                // cycle through all color channels ARGB
                for (int j = 0; j < 4; j++)
                {
                    // set the k least significant bits
                    for (int k = 0; k < bitsPerByte; k++)
                    {
                        // get current bit to be written from hf data
                        bit = (buffer & bufferMask) != 0;
                        // set mask to corresponding position in current color channel and k'th LS-bit
                        bitMask = 1 << (8 * j + k);
                        // set the bit in pixel to the value from data
                        
                        if (bit)
                        {
                            pixels[i] = Color.FromArgb(pixels[i].ToArgb() | bitMask);
                        }
                        else
                        {
                            pixels[i] = Color.FromArgb(pixels[i].ToArgb() & (~bitMask));
                        }
                        
                        bufferMask <<= 1;

                        // if we wrote the whole buffer, load the next byte of data from hf into buffer
                        if (bufferMask == 0x00)
                        {
                            bufferMask = 1;
                            dataIx++;
                            if (dataIx >= dataEndIndex)
                            {
                                return;
                            }
                            buffer = arr[dataIx];
                        }

                        // first 14 bytes are save using 1 bpB encoding
                        if (dataIx <= 13)
                        {
                            break;
                        }
                    }
                }
            }
            if (dataIx < dataEndIndex)
            {
                throw new Exception("Could not hide all data in the image. Probably not a valid steg image.");
            }
        }

        private static void WriteExtractedFile(byte[] data, string fileName)
        {
            using (var stream = File.Open("extr_" + fileName, FileMode.Create))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(data);
                    writer.Flush();
                }
            }
            Console.WriteLine($"Writing extracted file: {fileName} to disk. Saved as extr_{fileName}");
        }

        /// <summary>
        /// Try to extract a hidden file from image and save it with its original name prepended with "extr_".
        /// Throws an exception if the image does not contain a hidden file.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public static void Extract(string imagePath)
        {
            // TODO we dont need to store the metadata in data array, we can just read it and use it to extract the file
            LSbImage img = new LSbImage(imagePath);
            List<byte> data = new();    
            byte buffer = 0;
            byte bufferMask = 1;
            bool bit;

            // in bytes
            int fileNameLength = 0;         
            int dataLength = 0;             
            
            int bitsPerByte = 1;
            string fileName = "";

            for (int i = 0; i < img.pixels.Length; i++)
            {
                // cycle through all color channels ARGB
                for (int j = 0; j < 4; j++)
                {
                    // cycle through `k` least significant bits in a byte
                    for (int k = 0; k < bitsPerByte; k++)
                    {
                        bit = ((1 << (j * 8 + k)) & img.pixels[i].ToArgb()) != 0;
                        if (bit)
                        {
                            buffer |= bufferMask;
                        }

                        bufferMask <<= 1;
                        if (bufferMask == 0)
                        {
                            data.Add(buffer);
                            buffer = 0;
                            bufferMask = 1;
                        }

                        // verify magic number
                        if (data.Count == 13 && bufferMask == 1)
                        {
                            for (int l = 0; l < HiddenFile.magicNumber.Length; l++)
                            {
                                if (data[l] != HiddenFile.magicNumber[l])
                                {
                                    throw new Exception("magic number does not match. " +
                                        "probably not a valig steg image.");
                                }
                            }
                            Console.WriteLine("magic number OK");
                        }

                        // get bitsPerByte value
                        if (data.Count == 14 && bufferMask == 1)
                        {
                            bitsPerByte = data[13];
                            if (bitsPerByte < 1 || bitsPerByte > 8)
                            {
                                throw new Exception("invalid bitsPerByte value. Must be between 1 and 8.");
                            }
                            //break;
                            Console.WriteLine($"bitsPerByte: {bitsPerByte}");
                        }

                        // get file name length in bytes (2 B = 1 char)
                        if (data.Count == 15 && bufferMask == 1)
                        {
                            fileNameLength = data[14];
                            Console.WriteLine($"extracted file name length in bytes: {fileNameLength}");
                        }

                        // get the file name string
                        if (data.Count == 15 + fileNameLength && bufferMask == 1)
                        {
                            byte[] fileNameBytes = new byte[fileNameLength];
                            for (int l = 0; l < fileNameLength; l++)
                            {
                                fileNameBytes[l] = data[15 + l];
                            }
                            fileName = GetString(fileNameBytes);
                            Console.WriteLine($"extracted file name: {fileName}");
                        }

                        if (data.Count == 19 + fileNameLength && bufferMask == 1)
                        {
                            for (int l = 0; l < 4; l++)
                            {
                                dataLength |= data[15 + fileNameLength + l] << (8 * l);
                            }
                            Console.WriteLine($"data length in bytes: {dataLength}");
                        }

                        if (data.Count == 19 + fileNameLength + dataLength && bufferMask == 1)
                        {
                            byte[] fileData = new byte[dataLength];
                            Array.Copy(data.ToArray(), 19 + fileNameLength, fileData, 0, dataLength);
                            WriteExtractedFile(fileData, fileName);
                            return;
                        }
                    }
                }
            }
            throw new Exception("Could not extract file from image. Probably not a valid steg image.");
        }

        // Convert file name from byte array to string
        private static string GetString(byte[] ch)
        {
            char[] chars = new char[ch.Length / 2];
            for (int i = 0; i < ch.Length; i+=2)
            {
                chars[i / 2] = (char)((ch[i] << 8) | ch[i + 1]);
            }
            return new string(chars);
        }
    }
}

