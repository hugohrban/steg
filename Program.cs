using System.Drawing;

namespace Steganography
{
    class Program
    {
        public static void Main(string[] args)
        {
            # region help messages

            string helpMessageGeneral = 
                "Usage: dotnet run <command> [<args>]\n" +
                "Steganography tool for hiding arbitrary files in images.\n" +
                "========================================================\n" +
                "The available commands are:\n" +
                "   hide METHOD FILE INPUT_IMG [Q/bpB] [OUTPUT_IMG]\n" +
                "   extract METHOD IMG\n" +
                "   capacity METHOD IMG\n" +
                "   compress INPUT_IMG OUTPUT_IMG QUALITY\n" +
                "   help COMMAND\n" +
                "Use help COMMAND for more information about a given command.";

            string helpMessageHide = 
                "Usage: dotnet run hide METHOD FILE INPUT_IMG [Q/bpB] [OUTPUT_IMG]\n" +
                "Hide a file in an image.\n" +
                "=====================================================\n" +
                "METHOD can be either `jsteg` or `lsb`:\n" +
                "    - `jsteg` - hides the file data in the DCT coefficients of the jpeg image.\n" +
                "    - `lsb`   - hides the file data in the least significant bits of the pixels of the image\n\n" +
                "FILE             - path to the file to be hidden\n" +
                "INPUT_IMG        - path to the image to hide the file in\n" +
                "Q/bpB (optional) - depending on METHOD:\n" +
                "    - `jsteg` - quality of jpeg compression (1-100) (recommended Q=50)\n" +
                "    - `lsb`   - number of least significant bits to change (1-8) (recommended bpB=1)\n\n" +
                "OUTPUT_IMG (optional) - path to the output image. If not specified, the input image will be saved as \"steg_\" + its original name.\n" + 
                "The order of the optional parameters does not matter, it is inferred by the program.\n\n" +
                "Examples: dotnet run hide jsteg file.txt input.jpg 50\n" +
                "          dotnet run hide lsb file.txt input.jpg 1\n" +
                "          dotnet run hide lsb file.txt input.jpg output.jpg\n" +
                "          dotnet run hide jsteg file.txt input.jpg output.jpg 20";

            string helpMessageExtract = 
                "Usage: dotnet run extract METHOD IMG\n" +
                "Extract a file from an image.\n" +
                "==================================\n" +
                "METHOD - either `jsteg` or `lsb`\n" +
                "IMG    - the path to the image to extract the hidden file from\n" +
                "The file will be saved in the current directory as 'extr_' + its original name.";

            string helpMessageCapacity = 
                "Usage: dotnet run capacity METHOD IMG\n" +
                "Print the capacity of the image (size of file that can be hidden) for various settings of METHOD\n" +
                "================================================================================================\n" +
                "METHOD - either `jsteg` or `lsb`\n" +
                "IMG    - the path to the image to determine the capacity of";

            string helpMessageCompress = 
                "Usage: dotnet run compress INPUT_IMG OUTPUT_IMG QUALITY\n" +
                "Compress an image using jpeg compression.\n" +
                "=======================================================\n" +
                "INPUT_IMG  - path to the image to be compressed\n" +
                "OUTPUT_IMG - path to the compressed image\n" +
                "QUALITY    - quality of jpeg compression (1-100)";

            string helpMessageHelp = 
                "Usage: dotnet run help COMMAND\n" +
                "Print help message for a command.\n" +
                "=================================\n" +
                "COMMAND - the command to print help for (hide, extract, capacity, compress, help)\n";
            
            # endregion
    
            if (args.Length < 1)
            {
                System.Console.WriteLine(helpMessageGeneral);
                return;
            }
            switch (args[0])
            {
                case "hide":
                    if (args.Length < 4)
                        {
                            System.Console.WriteLine(helpMessageHide);
                            return;
                        }
                    if (args[1] == "jsteg")
                    {
                        int quality = 50;
                        string? outImagePath = null;
                        if (args.Length > 4)
                        {
                            if (IsInteger(args[4]))
                            {
                                quality = int.Parse(args[4]);
                                if (args.Length > 5)
                                {
                                    outImagePath = args[5];
                                }
                            }
                            else
                            {
                                outImagePath = args[4];
                                if (args.Length > 5)
                                {
                                    quality = int.Parse(args[5]);
                                }
                            }
                        }
                        JStegImage img = new JStegImage(args[3], quality, outImagePath);
                        img.Hide(args[2]);
                    }
                    else if (args[1] == "lsb")
                    {
                        int bitsPerByte = 1;
                        string? outImagePath = null;
                        if (args.Length > 4)
                        {
                           if (IsInteger(args[4]))
                            {
                                bitsPerByte = int.Parse(args[4]);
                                if (args.Length > 5)
                                {
                                    outImagePath = args[5];
                                }
                            }
                            else
                            {
                                outImagePath = args[4];
                                if (args.Length > 5)
                                {
                                    bitsPerByte = int.Parse(args[5]);
                                }
                            }
                        }
                        LSbImage lsbImage = new LSbImage(args[3], bitsPerByte, outImagePath);
                        lsbImage.Hide(args[2]);
                    }
                    else
                    {
                        System.Console.WriteLine(helpMessageHide);
                    }
                    break;
                
                case "extract":
                    if (args.Length < 3)
                    {
                        System.Console.WriteLine(helpMessageExtract);
                        return;
                    }
                    if (args[1] == "jsteg")
                    {
                        JStegImage.Extract(args[2]);
                    }
                    else if (args[1] == "lsb")
                    {
                        LSbImage.Extract(args[2]);
                    }
                    else
                    {
                        System.Console.WriteLine(helpMessageExtract);
                    }
                    break;

                case "capacity":
                    if (args.Length < 3)
                    {
                        System.Console.WriteLine(helpMessageCapacity);
                        return;
                    }
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
                    else 
                    {
                        System.Console.WriteLine(helpMessageCapacity);
                    }
                    break;

                case "compress":    
                    {
                        if (args.Length < 4)
                        {
                            System.Console.WriteLine(helpMessageCompress);
                            return;
                        }
                        JStegImage img = new JStegImage(args[1], int.Parse(args[3]));
                        img.Compress(args[2]);
                    }
                    break;

                case "help":
                    if (args.Length < 2)
                    {
                        System.Console.WriteLine(helpMessageGeneral);
                        return;
                    }
                    if (args[1] == "hide")
                    {
                        System.Console.WriteLine(helpMessageHide);
                    }
                    else if (args[1] == "extract")
                    {
                        System.Console.WriteLine(helpMessageExtract);
                    }
                    else if (args[1] == "capacity")
                    {
                        System.Console.WriteLine(helpMessageCapacity);
                    }
                    else if (args[1] == "compress")
                    {
                        System.Console.WriteLine(helpMessageCompress);
                    }
                    else if (args[1] == "help")
                    {
                        System.Console.WriteLine(helpMessageHelp);
                    } 
                    else
                    {             
                        System.Console.WriteLine(helpMessageGeneral);
                    }
                    break;

                default:
                    System.Console.WriteLine(helpMessageGeneral);
                    break;
            }
        }
        public static bool IsInteger(string input)
        {
            // Try to parse the input as an integer
            return int.TryParse(input, out _);
        }
    }
}
