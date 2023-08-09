using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace Steganography
{
    public class LSbImage//: IStegImage
    {
        public Color[] pixels { get; private set; }
        private Bitmap coverImage;
        private string imgPath;

        public int[] GetCapacities()
        {
            // TODO calculate capacities (max size of a hidden file) for all values of bitsPerByte (1-8)
            throw new NotImplementedException();
        }

        // get all pixels from a bitmap as an array of Color objects, line by line
        private Color[] GetPixels(Bitmap bitmap)
        {
            Color[] pixels = new Color[bitmap.Width * bitmap.Height];
            for (int i = 0; i < bitmap.Height; i++)
            {
                for (int j = 0; j < bitmap.Width; j++)
                {
                    pixels[i * bitmap.Width + j] = bitmap.GetPixel(j, i);
                }
            }
            return pixels;
        }

        public LSbImage(string imgPath)
        {
            coverImage = new Bitmap(imgPath);
            this.imgPath = imgPath;
            this.pixels = GetPixels(coverImage);
        }

        // write the image to disk. Call this method after hiding a file in the image.
        public void Write()
        {
            System.Console.WriteLine("Writing LSb-steg image to disk...");
            Bitmap stegImage = new Bitmap(coverImage.Width, coverImage.Height);
            for (int i = 0; i < pixels.Length; i++)
            {
                int x = i % stegImage.Width;
                int y = i / stegImage.Width;
                stegImage.SetPixel(x, y, pixels[i]);
            }
            string imgName = Path.GetFileName(imgPath);
            stegImage.Save("steg/" + imgName);
            System.Console.WriteLine($"Writing done. Image saved as steg/{imgName}");
        }

        private void HideArray(byte[] arr, int bitsPerByte)
        {
            // TODO multithreading
        }

        // hide a hiffenFile object in the image
        public void Hide(HiddenFile hf)
        {
            int bufferMask = 1;
            int dataIx = 0;
            byte buffer = hf.data[dataIx];
            bool bit;
            int bitMask;

            System.Console.WriteLine($"Hiding file {hf.fileName} in image {Path.GetFileName(imgPath)}...");

            // cycle through all pixels 
            for (int i = 0; i < pixels.Length; i++)
            {
                // cycle through all color channels ARGB
                for (int j = 0; j < 4; j++)
                {
                    // set the k least significant bits
                    for (int k = 0; k < hf.bitsPerByte; k++)
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
                        if (bufferMask >= 0x100)
                        {
                            bufferMask = 1;
                            dataIx++;
                            if (dataIx >= hf.data.Length)
                            {
                                System.Console.WriteLine("Hiding done.");
                                return;
                            }
                            buffer = hf.data[dataIx];
                        }

                        // first 13 bytes of hf are written in 1 bitPerByte encoding
                        if (dataIx <= 13)
                        {
                            break;
                        }
                    }
                }
            }
            if (dataIx < hf.data.Length - 1)
            {
                throw new InvalidDataException("The image is too small to be able to contain the hidden file. Try higher bitsPerByte value or a larger image.");
            }
            System.Console.WriteLine("Hiding done.");
        }

        /// <summary>
        /// Write the extracted file to disk.
        /// </summary>
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

        // Convert file name from byte array to string
        public static string GetString(byte[] ch)
        {
            char[] chars = new char[ch.Length / 2];
            for (int i = 0; i < ch.Length; i+=2)
            {
                chars[i / 2] = (char)((ch[i] << 8) | ch[i + 1]);
            }
            return new string(chars);
        }

        // try to extract a hidden file from image
        public void Extract()
        {
            // TODO we dont need to store the metadata in data array, we can just read it and use it to extract the file
            //LSbImage img = new LSbImage(imgPath);
            List<byte> data = new();
            byte buffer = 0;
            byte bufferMask = 1;
            bool bit;

            int fileNameLength = 0;         // in bytes
            int dataLength = 0;             // in bytes
            int bitsPerByte = 1;
            string fileName = "";

            for (int i = 0; i < pixels.Length; i++)
            {
                // cycle through all color channels ARGB
                for (int j = 0; j < 4; j++)
                {
                    // cycle through `k` least significant bits in a byte
                    for (int k = 0; k < bitsPerByte; k++)
                    {
                        bit = ((1 << (j * 8 + k)) & pixels[i].ToArgb()) != 0;
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
                                    throw new ArgumentException("magic number does not match. " +
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
        }
    }
}

