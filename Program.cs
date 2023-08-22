using System.Drawing;

namespace Steganography
{
    class Program
    {
        public static void Main(string[] args)
        {
            string helpMessage = "Usage: steg <command> [<args>]\n" +
                "Steganography tool for hiding arbitrary files in images.\n" +
                "The available commands are:\n" +
                "   hide [METHOD] [FILE] [INPUT_IMG] [Q/bpB]\n" +
                "   extract [METHOD] [IMG]\n" +
                "   capacity [METHOD] [IMG]\n" +
                "   compress [INPUT_IMG] [OUTPUT_IMG] [QUALITY]\n" +
                "   help [command]\n" +
                "Use help [command] for more information about a command";
    
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
                        int quality = 50;
                        if (args.Length > 4)
                            quality = int.Parse(args[4]);
                        JStegImage img = new JStegImage(args[3], quality);
                        img.Hide(args[2]);
                    }
                    else if (args[1] == "lsb")
                    {
                        int bitsPerByte = int.Parse(args[4]);
                        LSbImage lsbImage = new LSbImage(args[3], bitsPerByte);
                        lsbImage.Hide(args[2]);
                    }
                    break;
                
                case "extract":
                    if (args[1] == "jsteg")
                    {
                        JStegImage.Extract(args[2]);
                    }
                    else if (args[1] == "lsb")
                    {
                        LSbImage.Extract(args[2]);
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
                        JStegImage img = new JStegImage(args[1], int.Parse(args[3]));
                        img.Compress(args[2]);
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
                        System.Console.WriteLine("Usage: steg hide [METHOD] [FILE] [INPUT_IMG] [Q/bpB]\n" +
                            "Hide a file in an image.\n\n" +
                            "METHOD can be either `jsteg` or `lsb`:\n" +
                            "    - `jsteg` - hides the file data in the DCT coefficients of the jpeg image.\n" +
                            "    - `lsb`   - hides the file data in the least significant bits of the pixels of the image\n" +
                            "FILE       - path to the file to be hidden\n" +
                            "INPUT_IMG  - path to the image to hide the file in\n" +
                            "Q/bpB      - quality of jpeg compression (1-100) or number of least significant bits to change (1-8) depending on the METHOD\n");
                    }
                    else if (args[1] == "extract")
                    {
                        System.Console.WriteLine("Usage: steg extract [METHOD] [IMG]\n" +
                            "Extract a file from an image.\n\n" +
                            "METHOD - either `jsteg` or `lsb`\n" +
                            "IMG    - the path to the image to extract the hidden file from\n" +
                            "The file will be saved in the current directory as 'extr_' + its original name.");
                    }
                    else if (args[1] == "capacity")
                    {
                        System.Console.WriteLine("Usage: steg capacity [METHOD] [IMG]\n" +
                            "Print the capacity of the image (size of file that can be hidden) for various settings of METHOD\n\n" +
                            "METHOD - either `jsteg` or `lsb`\n" +
                            "IMG    - the path to the image to determine the capacity of");
                    }
                    else if (args[1] == "compress")
                    {
                        System.Console.WriteLine("Usage: steg compress [INPUT_IMG] [OUTPUT_IMG] [QUALITY]\n" +
                            "Compress an image using jpeg compression.\n\n" +
                            "INPUT_IMG  - path to the image to be compressed\n" +
                            "OUTPUT_IMG - path to the compressed image\n" +
                            "QUALITY    - quality of jpeg compression (1-100)");
                    }
                    else if (args[1] == "help")
                    {
                        System.Console.WriteLine("Usage: steg help [command]\n" +
                            "Print help message for a command.\n" +
                            "command - the command to print help for (hide, extract, capacity, compress, help)\n");
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
