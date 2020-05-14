using System;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SteganographyApp.Common.Data;

namespace SteganographyApp.Common.Arguments
{

    static class Parsers
    {

        static readonly int MAX_DUMMY_COUNT = 100;
        static readonly int MIN_DUMMY_COUNT = 20;

        /// <summary>
        /// Parses a boolean from the value parameter and sets the InsertDummies property.
        /// Will also attempt to call <see cref="ParseDummyCount"/>
        /// </summary>
        /// <param name="arguments">The InputArguments instance to modify.</param>
        /// <param name="value">The string representation of the InsertDummies boolean flag.</param>
        public static void ParseInsertDummies(InputArguments arguments, string value)
        {
            arguments.InsertDummies = Boolean.Parse(value);
            ParseDummyCount(arguments);
        }

        /// <summary>
        /// Parses a boolean from the value parameter and sets the DeleteAfterConversion property.
        /// </summary>
        /// <param name="arguments">The InputArguments instance to modify.</param>
        /// <param name="value">The string representation of the InsertDummies boolean flag.</param>
        public static void ParseDeleteOriginals(InputArguments arguments, string value)
        {
            arguments.DeleteAfterConversion = Boolean.Parse(value);
        }

        /// <summary>
        /// Parses the number of dummy entries to insert into the image based on the
        /// properties retrieved from the first and last images.
        /// <para>Note: the first and last image can be the same image if only one cover image
        /// was provided.</para>
        /// </summary>
        public static void ParseDummyCount(InputArguments arguments)
        {
            if (!arguments.InsertDummies || arguments.CoverImages == null)
            {
                return;
            }
            
            long dummyCount = 1;
            int[] imageIndexes = new int[] { 0, arguments.CoverImages.Length - 1 };
            foreach (int imageIndex in imageIndexes)
            {
                using (Image<Rgba32> image = Image.Load(arguments.CoverImages[imageIndex]))
                {
                    dummyCount += dummyCount * (image.Width * image.Height);
                }
            }
            string seed = dummyCount.ToString();
            arguments.DummyCount = IndexGenerator.FromString(seed).Next(MAX_DUMMY_COUNT) + MIN_DUMMY_COUNT;
        }

        /// <summary>
        /// Parses the compression level and sets the CompressionLevel property value.
        /// </summary>
        /// <param name="arguments">The InputArguments instance to modify.</param>
        /// <param name="value">The string representation of an int value.</param>
        /// <exception cref="ArgumentValueException">Thrown if the string value could not be converted
        /// to an int or if the value is less than 0 or greater than 9.</exception>
        public static void ParseCompressionLevel(InputArguments arguments, string value)
        {
            try
            {
                arguments.CompressionLevel = Convert.ToInt32(value);
                if (arguments.CompressionLevel < 0 || arguments.CompressionLevel > 9)
                {
                    throw new ArgumentValueException("The compression level must be a whole number between 0 and 9 inclusive.");
                }
            }
            catch (Exception e) when (!(e is ArgumentValueException))
            {
                throw new ArgumentValueException($"Could not parse compression level from value: {value}", e);
            }
        }

        /// <summary>
        /// Parses an int32 from the value string and sets the ChunkByteSize property
        /// </summary>
        /// <param name="arguments">The InputArguments instance to modify.</param>
        /// <param name="value">The string representation of an int value.</param>
        /// <exception cref="ArgumentValueException">Thrown if the string value could not be converted
        /// to an int or if the value of the int is less than or equal to 0.</exception>
        public static void ParseChunkSize(InputArguments arguments, string value)
        {
            try
            {
                arguments.ChunkByteSize = Convert.ToInt32(value);
                if(arguments.ChunkByteSize <= 0)
                {
                    throw new ArgumentValueException("The chunk size value must be a positive whole number with a value more than 0.");
                }
            }
            catch(Exception e) when (!(e is ArgumentValueException))
            {
                throw new ArgumentValueException($"Could not parse chunk size from value {value}", e);
            }
        }

        /// <summary>
        /// Parses a boolean from the value and sets the PrintStack property
        /// </summary>
        /// <param name="arguments">The InputArguments instance to modify</param>
        /// <param name="value">A string representation of a boolean value.</param>
        /// <exception cref="ArgumentValueException">Thrown if a boolean value could not
        /// be parsed from the value parameter.</exception>
        public static void ParsePrintStack(InputArguments arguments, string value)
        {
            arguments.PrintStack = Boolean.Parse(value);
        }

        /// <summary>
        /// Checks if the specified file, value, exists and then sets the
        /// FileToEncode property.
        /// </summary>
        /// <param name="arguments">The InputArguments instance to modify.</param>
        /// <param name="value">The relative or absolute path to the file to encode.</param>
        /// <exception cref="ArgumentValueException">Thrown if the specified input file
        /// could not be found.</exception>
        public static void ParseFileToEncode(InputArguments arguments, string value)
        {
            if (!File.Exists(value))
            {
                throw new ArgumentValueException($"File to decode could not be found at {value}");
            }
            else if (File.GetAttributes(value).HasFlag(FileAttributes.Directory))
            {
                throw new ArgumentValueException($"Input file at {value} was a directory but a file is required.");
            }
            arguments.FileToEncode = value;
        }

        /// <summary>
        /// Parses a boolean from the value string and sets the UseCompression property.
        /// </summary>
        /// <param name="arguments">The InputArguments instance to modify</param>
        /// <param name="value">A string representation of a boolean value.</param>
        /// <exception cref="ArgumentValueException">Thrown if a boolean value could not be parsed
        /// from the value parameter</exception>
        public static void ParseUseCompression(InputArguments arguments, string value)
        {
            arguments.UseCompression = Boolean.Parse(value);
        }

        /// <summary>
        /// Sets the EncodeDecode action based on the value returned from the Enum.TryParse method.
        /// </summary>
        /// <param name="args">The InputArguments instance to make modifications to.</param>
        /// <param name="value">A string representation of an EncodeDecodeAction enum value.</param>
        /// <exception cref="ArgumentValueException">Thrown if
        /// the string value does not map to an enum value.</exception>
        public static void ParseEncodeOrDecodeAction(InputArguments args, string value)
        {
            value = value.Replace("-", "");
            if(!Enum.TryParse(value, true, out ActionEnum action))
            {
                throw new ArgumentValueException($"Invalid value for action argument. Expected 'encode', 'decode', 'clean', 'calculate-storage-space', or 'calculate-encrypted-size' got {value}");
            }
            args.EncodeOrDecode = action;
        }

    }

}