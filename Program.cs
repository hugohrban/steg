using System.Drawing;
namespace Steganography
{


    class Program
    {
        public static void Main(string[] args)
        {
            //HiddenFile hf = new HiddenFile("hide/abc", StegType.LSbEncoding, 5);

            //LSbImage lSbImage = new LSbImage("covers/clouds.jpg");
            //lSbImage.Hide(hf);
            //lSbImage.Write();



            // LSbImage.Extract("steg/park.jpg");

            //LSbImage lsb2 = new LSbImage("steg_clouds.jpg");
            //Console.WriteLine(AreEqual(lSbImage.pixels, lsb2.pixels));
            //LSbImage.Extract("steg_clouds.jpg");

            // sbyte b = (sbyte)144;

            // sbyte[,] block = new sbyte[8,8] {   
            //     {52,55,61,66,70,61,64,73,},
            //     {63,59,55,90,109,85,69,72,},
            //     {62,59,68,113,144,104,66,73,},
            //     {63,58,71,122,154,106,70,69,},
            //     {67,61,68,104,126,88,68,70,},
            //     {79,65,60,70,77,68,58,75,},
            //     {85,71,64,59,55,61,65,83,},
            //     {87,79,69,68,65,76,78,94}
            // };
            
            
            // // // block of only 255's
            // int[,] arr = new int[8,8] {
            //     {255, 255, 255, 255, 255, 255, 255, 255},
            //     {255, 255, 255, 255, 255, 255, 255, 255},
            //     {255, 255, 255, 255, 255, 255, 255, 255},
            //     {255, 255, 255, 255, 255, 255, 255, 255},
            //     {255, 255, 255, 255, 255, 255, 255, 255},
            //     {255, 255, 255, 255, 255, 255, 255, 255},
            //     {255, 255, 255, 255, 255, 255, 255, 255},
            //     {255, 255, 255, 255, 255, 255, 255, 255},
            // };

            // // // var dct = JStegImage.fdct(arr);

            // double[,] dct2 = JStegImage.DCT2(arr);

            // for (int i = 0; i < 8; i++)
            // {
            //     for (int j = 0; j <8; j++)
            //     {
            //         Console.Write(dct2[i, j] + " ");
            //     }
            //     Console.WriteLine();
            // }

            

            

            // var shifted = JStegImage.Shift(block);

            // var dct = JStegImage.DCT2(sblock);

            // var quantized = JStegImage.Quantize(dct, 0);

            // var quantized = JStegImage.Quantize(dct, 0);

            // for (int i = 0; i < 8; i++)
            // {
            //     for (int j = 0; j <8; j++)
            //     {
            //         Console.Write(quantized[i, j] + " ");
            //     }
            //     Console.WriteLine();
            // }

            JStegImage img = new JStegImage("covers/cherry.jpg");
            //JStegImage img = new JStegImage("hid_nt2.jpg");
            img.Write();



        }
    }
}