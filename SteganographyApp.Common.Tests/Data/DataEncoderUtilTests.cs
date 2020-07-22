﻿using Moq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SteganographyApp.Common.Data;
using SteganographyApp.Common.Providers;

namespace SteganographyApp.Common.Tests
{
    [TestClass]
    public class DataEncoderUtilTests
    {

        private static readonly byte[] InputBytes = new byte[1];
        private static readonly string Password = "password";
        private static readonly bool UseCompression = false;
        private static readonly int DummyCount = 10;
        private static readonly string RandomSeed = "randomSeed";

        private static readonly string StringToDecode = "stringToDecode";

        private Mock<IEncryptionProvider> mockEncryptionProvider;
        private Mock<IBinaryUtil> mockBinaryUtil;
        private Mock<IDummyUtil> mockDummyUtil;
        private Mock<IRandomizeUtil> mockRandomUtil;
        private Mock<ICompressionUtil> mockCompressionUtil;

        [TestInitialize]
        public void Initialize()
        {
            mockEncryptionProvider = new Mock<IEncryptionProvider>();
            mockBinaryUtil = new Mock<IBinaryUtil>();
            mockDummyUtil = new Mock<IDummyUtil>();
            mockRandomUtil = new Mock<IRandomizeUtil>();
            mockCompressionUtil = new Mock<ICompressionUtil>();

            Injector.UseProvider<IEncryptionProvider>(mockEncryptionProvider.Object);
            Injector.UseProvider<IBinaryUtil>(mockBinaryUtil.Object);
            Injector.UseProvider<IDummyUtil>(mockDummyUtil.Object);
            Injector.UseProvider<IRandomizeUtil>(mockRandomUtil.Object);
            Injector.UseProvider<ICompressionUtil>(mockCompressionUtil.Object);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Injector.ResetProviders();
        }

        [TestMethod]
        public void TestEncode()
        {
            string encryptedString = "encrypted_string";
            mockEncryptionProvider.Setup(provider => provider.Encrypt(It.IsAny<string>(), It.IsAny<string>())).Returns(encryptedString);

            string binaryString = "binary_string";
            mockBinaryUtil.Setup(provider => provider.ToBinaryString(It.IsAny<string>())).Returns(binaryString);

            string dummyString = "dummy_string";
            mockDummyUtil.Setup(provider => provider.InsertDummies(It.IsAny<int>(), It.IsAny<string>())).Returns(dummyString);

            string randomizedString = "randomized_string";
            mockRandomUtil.Setup(provider => provider.RandomizeBinaryString(It.IsAny<string>(), It.IsAny<string>())).Returns(randomizedString);

            var util = new DataEncoderUtil();

            string result = util.Encode(InputBytes, Password, UseCompression, DummyCount, RandomSeed);

            Assert.AreEqual(randomizedString, result);

            mockEncryptionProvider.Verify(provider => provider.Encrypt(It.IsAny<string>(), Password), Times.Once());
            mockBinaryUtil.Verify(util => util.ToBinaryString(encryptedString), Times.Once());
            mockDummyUtil.Verify(util => util.InsertDummies(DummyCount, binaryString), Times.Once());
            mockRandomUtil.Verify(util => util.RandomizeBinaryString(dummyString, RandomSeed), Times.Once());
            mockCompressionUtil.Verify(util => util.Compress(It.IsAny<byte[]>()), Times.Never());
        }

        [TestMethod]
        public void TestEncodeWithCompressionEnabled()
        {
            mockEncryptionProvider.Setup(provider => provider.Encrypt(It.IsAny<string>(), It.IsAny<string>())).Returns("encrypted_string");
            mockBinaryUtil.Setup(util => util.ToBinaryString(It.IsAny<string>())).Returns("binary_string");
            mockDummyUtil.Setup(util => util.InsertDummies(It.IsAny<int>(), It.IsAny<string>())).Returns("dummy_string");
            mockRandomUtil.Setup(util => util.RandomizeBinaryString(It.IsAny<string>(), It.IsAny<string>())).Returns("randomized_string");

            var util = new DataEncoderUtil();

            string result = util.Encode(InputBytes, Password, true, DummyCount, RandomSeed);

            mockCompressionUtil.Verify(util => util.Compress(InputBytes), Times.Once());
        }

        [TestMethod]
        public void TestDecode()
        {
            string randomizedString = "randomized_string";
            mockRandomUtil.Setup(util => util.ReorderBinaryString(It.IsAny<string>(), It.IsAny<string>())).Returns(randomizedString);

            string dummyString = "dummy_string";
            mockDummyUtil.Setup(util => util.RemoveDummies(It.IsAny<int>(), It.IsAny<string>())).Returns(dummyString);

            string base64String = "base64String";
            mockBinaryUtil.Setup(util => util.ToBase64String(It.IsAny<string>())).Returns(base64String);

            string encryptedString = "ZW5jcnlwdGVkX3N0cmluZw==";
            mockEncryptionProvider.Setup(provider => provider.Decrypt(It.IsAny<string>(), It.IsAny<string>())).Returns(encryptedString);

            var util = new DataEncoderUtil();

            byte[] result = util.Decode(StringToDecode, Password, UseCompression, DummyCount, RandomSeed);

            mockRandomUtil.Verify(provider => provider.ReorderBinaryString(StringToDecode, RandomSeed), Times.Once());
            mockDummyUtil.Verify(provider => provider.RemoveDummies(DummyCount, randomizedString), Times.Once());
            mockBinaryUtil.Verify(provider => provider.ToBase64String(dummyString), Times.Once());
            mockEncryptionProvider.Verify(provider => provider.Decrypt(base64String, Password), Times.Once());
            mockCompressionUtil.Verify(util => util.Decompress(It.IsAny<byte[]>()), Times.Never());
        }

        [TestMethod]
        public void TestDecodWithCompressionDisabled()
        {
            mockRandomUtil.Setup(util => util.ReorderBinaryString(It.IsAny<string>(), It.IsAny<string>())).Returns("randomized_string");
            mockDummyUtil.Setup(util => util.RemoveDummies(It.IsAny<int>(), It.IsAny<string>())).Returns("dummy_string");
            mockBinaryUtil.Setup(util => util.ToBase64String(It.IsAny<string>())).Returns("base64String");
            mockEncryptionProvider.Setup(provider => provider.Decrypt(It.IsAny<string>(), It.IsAny<string>())).Returns("ZW5jcnlwdGVkX3N0cmluZw==");

            var util = new DataEncoderUtil();

            byte[] result = util.Decode(StringToDecode, Password, true, DummyCount, RandomSeed);

            mockCompressionUtil.Verify(util => util.Decompress(It.IsAny<byte[]>()), Times.Once());
        }

    }
}