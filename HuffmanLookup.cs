using System;

namespace Steganography
{
    /// <summary>
    /// Huffman lookup table is a compiled look-up table representation of a huffmanSpec.
    /// Each value maps to an int of which the 8 most significant bits hold the
    /// codeword size in bits and the 24 least significant bits hold the codeword.
    /// The maximum codeword size is 16 bits.
    /// </summary>
    class HuffmanLookup
    {
        public int[] huffmanCodes;
        public HuffmanLookup(HuffmanSpec spec)
        {
            int maxVal = 0;
            for (int i = 0; i < spec.symbol.Length; i++)
            {
                if (spec.symbol[i] > maxVal)
                {
                    maxVal = spec.symbol[i];
                }
            }
            huffmanCodes = new int[maxVal + 1];
            int code = 0;
            int k = 0;
            for (int i = 0; i < spec.count.Length; i++)
            {
                int nBits = (i + 1) << 24;
                for (int j = 0; j < spec.count[i]; j++)
                {
                    huffmanCodes[spec.symbol[k]] = nBits | code;
                    code++;
                    k++;
                }
                code <<= 1;
            }
        }
    }
}