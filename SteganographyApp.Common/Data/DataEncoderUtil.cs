﻿namespace SteganographyApp.Common.Data
{
    using System;

    using SteganographyApp.Common.Injection;

    /// <summary>
    /// The contract for interacting with the DataEncoderUtil instance.
    /// </summary>
    public interface IDataEncoderUtil
    {
        /// <include file='../docs.xml' path='docs/members[@name="DataEncoderUtil"]/Encode/*' />
        string Encode(byte[] bytes, string password, bool useCompression, int dummyCount, string randomSeed);

        /// <include file='../docs.xml' path='docs/members[@name="DataEncoderUtil"]/Decode/*' />
        byte[] Decode(string binary, string password, bool useCompression, int dummyCount, string randomSeed);
    }

    /// <summary>
    /// A case class inheriting from Exception that specifies an error occured
    /// when an IFileCoder instance attempted to transform input data.
    /// </summary>
    public class TransformationException : Exception
    {
        /// <include file='../docs.xml' path='docs/members[@name="Exceptions"]/GeneralMessage/*' />
        public TransformationException(string message) : base(message) { }

        /// <include file='../docs.xml' path='docs/members[@name="Exceptions"]/GeneralMessageInner/*' />
        public TransformationException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Utility class to encode a file to encrypted binary data or decode the encrypted binary string to the
    /// original file bytes.
    /// </summary>
    [Injectable(typeof(IDataEncoderUtil))]
    public class DataEncoderUtil : IDataEncoderUtil
    {
        /// <include file='../docs.xml' path='docs/members[@name="DataEncoderUtil"]/Encode/*' />
        public string Encode(byte[] bytes, string password, bool useCompression, int dummyCount, string randomSeed)
        {
            if (useCompression)
            {
                bytes = Injector.Provide<ICompressionUtil>().Compress(bytes);
            }

            string base64 = Convert.ToBase64String(bytes);

            if (password != string.Empty)
            {
                try
                {
                    base64 = Injector.Provide<IEncryptionProxy>().Encrypt(base64, password);
                }
                catch (Exception e)
                {
                    throw new TransformationException("An exception occured while encrypting content.", e);
                }
            }

            string binary = Injector.Provide<IBinaryUtil>().ToBinaryString(base64);

            if (dummyCount > 0)
            {
                binary = Injector.Provide<IDummyUtil>().InsertDummies(dummyCount, binary, randomSeed);
            }

            if (randomSeed != string.Empty)
            {
                binary = Injector.Provide<IRandomizeUtil>().RandomizeBinaryString(binary, randomSeed);
            }

            return binary;
        }

        /// <include file='../docs.xml' path='docs/members[@name="DataEncoderUtil"]/Decode/*' />
        public byte[] Decode(string binary, string password, bool useCompression, int dummyCount, string randomSeed)
        {
            if (randomSeed != string.Empty)
            {
                binary = Injector.Provide<IRandomizeUtil>().ReorderBinaryString(binary, randomSeed);
            }

            if (dummyCount > 0)
            {
                binary = Injector.Provide<IDummyUtil>().RemoveDummies(dummyCount, binary, randomSeed);
            }

            var decoded64String = Injector.Provide<IBinaryUtil>().ToBase64String(binary);
            if (password != string.Empty)
            {
                try
                {
                    decoded64String = Injector.Provide<IEncryptionProxy>().Decrypt(decoded64String, password);
                }
                catch (Exception e)
                {
                    throw new TransformationException("An exception occured while decrypting content.", e);
                }
            }

            byte[] decoded = Convert.FromBase64String(decoded64String);

            if (useCompression)
            {
                try
                {
                    return Injector.Provide<ICompressionUtil>().Decompress(decoded);
                }
                catch (Exception e)
                {
                    throw new TransformationException("An exception occurred while decompressing content.", e);
                }
            }

            return decoded;
        }
    }
}