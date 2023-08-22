using System;

namespace Steganography
{
    /// <summary>
    /// Specification of a huffman encoding table.
    /// </summary>
    struct HuffmanSpec
    {
        // counts[i] is the number of codes of length i bits
        public byte[] count;
        // symbols[i] is the decoded i-th code word
        public byte[] symbol;
    }
}