using System.Drawing;

namespace Steganography
{
    class Program
    {
        public static void Main(string[] args)
        {
            string helpMessage = "Usage: steg <command> [<args>]\n" +
                "The available steganography commands are:\n" +
                "   hide [METHOD] [FILE] [INPUT_IMG] [OUTPUT_IMG]\n" +
                "   extract [METHOD] [IMG]\n" +
                "   capacity [METHOD] [IMG]\n" +
                "   help [command]\n" +
                "Use help [command] for more information about a command\n";
    
            if (args.Length < 1)
            {
                System.Console.WriteLine(helpMessage);
                return;
            }
            switch (args[0])
            {
                case "hide":
                    if (args[1] == "jsteg")
                    {
                        HiddenFile hf = new HiddenFile(args[2], StegType.JSteg);
                        JStegImage img = new JStegImage(args[3]);
                        img.Hide(hf);
                        int quality = 50;
                        if (args.Length > 5)
                            quality = int.Parse(args[5]);
                        img.Write(args[4], quality);
                        
                    }
                    else if (args[1] == "lsb")
                    {
                        int bitsPerByte = int.Parse(args[3]);
                        HiddenFile hf = new HiddenFile(args[2], StegType.LSbEncoding, bitsPerByte);
                        LSbImage lSbImage = new LSbImage(args[4]);
                        lSbImage.Hide(hf);
                        lSbImage.Write();
                    }
                    break;
                
                case "extract":
                    if (args[1] == "jsteg")
                    {
                        JPEGExtractor ext = new JPEGExtractor(args[2]);
                        ext.ReadFile();
                    }
                    else if (args[1] == "lsb")
                    {
                        LSbImage lsb = new LSbImage(args[2]);
                        lsb.Extract();
                    }
                    break;

                case "capacity":
                    if (args[1] == "jsteg")
                    {
                        JStegImage img = new JStegImage(args[2]);
                        img.PrintCapacity();
                    }
                    else if (args[1] == "lsb")
                    {
                        LSbImage lsb = new LSbImage(args[2]);
                        lsb.PrintCapacity();
                    }
                    break;

                case "compress":
                    {
                        JStegImage img = new JStegImage(args[1]);
                        img.Write(args[2], int.Parse(args[3]));
                    }
                    break;

                case "help":
                    if (args.Length < 2)
                    {
                        System.Console.WriteLine(helpMessage);
                        return;
                    }
                    if (args[1] == "hide")
                    {
                        System.Console.WriteLine("Usage: steg hide [METHOD] [FILE] [INPUT_IMG] [OUTPUT_IMG]\n" +
                            "Hide a file in an image\n" +
                            "METHOD can be either `jsteg` or `lsb`:\n" +
                            "    `jsteg` - hides the file data in the AC coefficients of the jpeg image\n" +
                            "    `lsb` - hides the file data in the least significant bits of the pixels of the image\n" +
                            "FILE       - path to the file to be hidden\n" +
                            "INPUT_IMG  - path to the image to hide the file in\n" +
                            "OUTPUT_IMG - path to the image to save the result to\n");
                    }
                    else if (args[1] == "extract")
                    {
                        System.Console.WriteLine("Usage: steg extract [METHOD] [IMG]\n" +
                            "Extract a file from an image\n" +
                            "METHOD can be either `jsteg` or `lsb`\n" +
                            "IMG is the path to the image to extract the hidden file from\n" +
                            "The file will be saved in the current directory as 'extr_' + its original name.");
                    }
                    else
                    {
                        System.Console.WriteLine(helpMessage);
                    }
                    break;
            }
        }
    }
}
