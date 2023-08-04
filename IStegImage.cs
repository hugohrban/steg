using System;
namespace Steganography
{
	public interface IStegImage
	{
		// save the image to file
		void Write();
		// embed a file in the image
		void Hide(HiddenFile hiddenFile);
		// extract a file if it was embedded in the image in a particular way and save it
		static abstract void Extract(string imagePath);
	}
}
