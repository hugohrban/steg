using System;
namespace Steganography
{
	public interface IStegImage
	{
		/// <summary>
		/// Embed a file in an image and save it to disk in the format specified by the image.
		/// </summary>
		/// <param name="filePath">Path of file to be hidden</param>
		public void Hide(string filePath);

		/// <summary>
		/// Print the capacity of the image in bytes and kilobytes.
		/// </summary>
		public void PrintCapacity();

		/// <summary>
		/// Extract a file if it was embedded in the image in a particular way and save it.
		/// </summary>
		/// <param name="imagePath"></param>
		public static abstract void Extract(string imagePath);
	}
}
