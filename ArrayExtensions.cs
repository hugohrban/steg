namespace Steganography
{
    public static class Extensions
    {
        public static int Sum(this byte[] array)
        {
            int sum = 0;
            for (int i = 0; i < array.Length; i++)
            {
                sum += array[i];
            }
            return sum;
        }
    }
}