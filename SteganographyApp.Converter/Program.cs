﻿using System;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SteganographyApp.Common;
using SteganographyApp.Common.Arguments;

namespace SteganographyApp.Converter
{
    class Program
    {

        /// <summary>
        /// The mime type of png images. All images that have the png mimetype should not
        /// be put through the conversion process.
        /// </summary>
        private static readonly string PngMimeType = "image/png";

        static void Main(string[] args)
        {
            Console.WriteLine("\nSteganography Converter\n");
            if (Array.IndexOf(args, "--help") != -1 || Array.IndexOf(args, "-h") != -1)
            {
                PrintHelp();
                return;
            }

            var parser = new ArgumentParser();
            if(!parser.TryParse(args, out IInputArguments arguments, PostValidation))
            {
                parser.PrintCommonErrorMessage();
                return;
            }
            
            ConvertImagesToPng(arguments);
        }

        /// <summary>
        /// Performs some validation once all the user inputted values have been parsed and individually
        /// validated.
        /// </summary>
        private static string PostValidation(IInputArguments inputs)
        {
            if (inputs.EncodeOrDecode != ActionEnum.Convert)
            {
                return "The converter utility only supports the Convert action.";
            }
            if (Checks.IsNullOrEmpty(inputs.CoverImages))
            {
                return "At least one image must be provided to convert.";
            }
            return null;
        }

        /// <summary>
        /// Converts all of the images to a PNG format and will optionally delete
        /// the original images after convertion if the delete option was specified
        /// by the user.
        /// </summary>
        private static void ConvertImagesToPng(IInputArguments args)
        {
            string[] lossyImages = args.CoverImages.Where(FilterOutPngImages).ToArray();
            Console.WriteLine("Converting {0} images.", lossyImages.Length);
            var tracker = ProgressTracker.CreateAndDisplay(lossyImages.Length, "Converting images", "Finished converting all images");

            foreach (string coverImage in lossyImages)
            {
                var encoder = new PngEncoder();
                encoder.CompressionLevel = args.CompressionLevel;
                using(var image = Image.Load(coverImage))
                {
                    image.Save(ReplaceFileExtension(coverImage), encoder);
                }

                if (args.DeleteAfterConversion)
                {
                    File.Delete(coverImage);
                }

                tracker.UpdateAndDisplayProgress();
            }
        }

        /// <summary>
        /// Filters out any images that already have the png format.
        /// </summary>
        private static bool FilterOutPngImages(string image)
        {
            return Image.DetectFormat(image).DefaultMimeType != PngMimeType;
        }

        /// <summary>
        /// Takes in the path to the specified image, stripts out the existing file extension
        /// and replaces it with a png extension.
        /// </summary>
        /// <param name="image">The path to the image being converted</param>
        private static string ReplaceFileExtension(string image)
        {
            int index = image.LastIndexOf('.');
            return $"{image.Substring(0, index)}.png";
        }

        /// <summary>
        /// Attempts to print the help info retrieved from the help.props file.
        /// </summary>
        private static void PrintHelp()
        {
            var parser = new HelpParser();
            if (!parser.TryParseHelpFile(out HelpInfo info))
            {
                parser.PrintCommonErrorMessage();
                return;
            }

            Console.WriteLine("Steganography Converter Help\n");

            foreach (string message in info.GetHelpMessagesFor(HelpItemSet.Converter))
            {
                Console.WriteLine("{0}\n", message);
            }
        }
    }
}
