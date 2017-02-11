namespace Common.UnitTests.Caching
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.Extensions.Options;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Trifling.Caching;
    using Trifling.Caching.Impl;
    using Trifling.Caching.Interfaces;
    using Trifling.Compression;
    using Trifling.Compression.Interfaces;
    using Trifling.Serialization.Interfaces;

    [TestClass]
    public class CacheManagerTests
    {
        [TestMethod]
        public void CacheManager_Constructor_WhenConstructed_ThenInitialisesEngine()
        {
            // ----- Arrange -----
            var options = new CacheEngineConfiguration();
            var configurationMoq = new Mock<IOptions<CacheManagerConfiguration>>();
            configurationMoq
                .Setup(_ => _.Value)
                .Returns(new CacheManagerConfiguration { CacheEngineConfiguration = options });

            var binarySerializerMoq = new Mock<IBinarySerializer>();
            var compressorFactoryMoq = new Mock<ICompressorFactory>();

            // we expect that the engine will be initialised with the options mocked above.
            var engineMoq = new Mock<ICacheEngine>();
            engineMoq
                .Setup(_ => _.Initialise(options))
                .Verifiable();

            // ----- Act -----
            var cache = new CacheManager(
                engineMoq.Object, 
                configurationMoq.Object,
                binarySerializerMoq.Object,
                compressorFactoryMoq.Object);

            // ----- Assert -----
            engineMoq.Verify(_ => _.Initialise(options), Times.Once());
        }

        [TestMethod]
        public void CacheManager_Remove_WhenCacheKeyNotFound_ThenReturnsFalse()
        {
            // ----- Arrange -----
            var cacheEntryKey = new CacheEntryKey("test-key", 55);
            var expectedKeyString = "test-key/55";

            var configurationMoq = new Mock<IOptions<CacheManagerConfiguration>>();
            CacheManagerTests.SetupConfigurationOptions(ref configurationMoq);

            var binarySerializerMoq = new Mock<IBinarySerializer>();
            var compressorFactoryMoq = new Mock<ICompressorFactory>();
            var engineMoq = new Mock<ICacheEngine>();
            engineMoq
                .Setup(_ => _.Remove(expectedKeyString))
                .Returns(false)
                .Verifiable();

            var cache = new CacheManager(
                engineMoq.Object,
                configurationMoq.Object,
                binarySerializerMoq.Object,
                compressorFactoryMoq.Object);

            // ----- Act -----
            var found = cache.Remove(cacheEntryKey);

            // ----- Assert -----
            Assert.IsFalse(found, "Engine returned true when false was expected.");
            engineMoq.Verify(_ => _.Remove(expectedKeyString), Times.Once());
        }

        [TestMethod]
        public void CacheManager_Remove_WhenCacheKeyFound_ThenReturnsTrue()
        {
            // ----- Arrange -----
            var cacheEntryKey = new CacheEntryKey("test-key", 55);
            var expectedKeyString = "test-key/55";

            var configurationMoq = new Mock<IOptions<CacheManagerConfiguration>>();
            CacheManagerTests.SetupConfigurationOptions(ref configurationMoq);

            var binarySerializerMoq = new Mock<IBinarySerializer>();
            var compressorFactoryMoq = new Mock<ICompressorFactory>();
            var engineMoq = new Mock<ICacheEngine>();
            engineMoq
                .Setup(_ => _.Remove(expectedKeyString))
                .Returns(true)
                .Verifiable();

            var cache = new CacheManager(
                engineMoq.Object,
                configurationMoq.Object,
                binarySerializerMoq.Object,
                compressorFactoryMoq.Object);

            // ----- Act -----
            var found = cache.Remove(cacheEntryKey);

            // ----- Assert -----
            Assert.IsTrue(found, "Engine returned false when true was expected.");
            engineMoq.Verify(_ => _.Remove(expectedKeyString), Times.Once());
        }

        [TestMethod]
        public void CacheManager_Cache_WhenCachingString_ThenEngineCalledToCache()
        {
            // ----- Arrange -----
            var cacheEntryKey = new CacheEntryKey("test-key", 55);
            var expectedKeyString = cacheEntryKey.ToString();
            var expectedExpiryTime = TimeSpan.FromMinutes(45d);

            var configurationMoq = new Mock<IOptions<CacheManagerConfiguration>>();
            CacheManagerTests.SetupConfigurationOptions(ref configurationMoq);

            var binarySerializerMoq = new Mock<IBinarySerializer>();
            CacheManagerTests.SetupBinarySerializerForFakeSerialize(ref binarySerializerMoq);

            var compressorFactoryMoq = new Mock<ICompressorFactory>();
            var engineMoq = new Mock<ICacheEngine>();
            engineMoq
                .Setup(_ => _.Cache(expectedKeyString, It.IsAny<byte[]>(), expectedExpiryTime))
                .Verifiable();

            var cache = new CacheManager(
                engineMoq.Object,
                configurationMoq.Object,
                binarySerializerMoq.Object,
                compressorFactoryMoq.Object);

            // ----- Act -----
            cache.Cache<string>(cacheEntryKey, "value to store", expectedExpiryTime);

            // ----- Assert -----
            engineMoq.Verify(_ => _.Cache(expectedKeyString, It.IsAny<byte[]>(), expectedExpiryTime), Times.Once());
        }

        [TestMethod]
        public void CacheManager_Cache_WhenCachingString_ThenSameValueReturned()
        {
            // ----- Arrange -----
            var cacheEntryKey = new CacheEntryKey("test-key", 55);
            var expectedKeyString = cacheEntryKey.ToString();
            var expectedExpiryTime = TimeSpan.FromMinutes(45d);

            var configurationMoq = new Mock<IOptions<CacheManagerConfiguration>>();
            CacheManagerTests.SetupConfigurationOptions(ref configurationMoq);

            var binarySerializerMoq = new Mock<IBinarySerializer>();
            binarySerializerMoq
                .Setup(_ => _.Serialize<string>("value to store"))
                .Returns(new byte[] { 9, 18, 27, 36, 45, 54, 63, 72, 81 });

            binarySerializerMoq
                .Setup(_ => _.Deserialize<string>(It.IsAny<byte[]>()))
                .Returns("value to store");

            var compressorFactoryMoq = new Mock<ICompressorFactory>();
            var engineMoq = new Mock<ICacheEngine>();

            var cache = new CacheManager(
                engineMoq.Object,
                configurationMoq.Object,
                binarySerializerMoq.Object,
                compressorFactoryMoq.Object);

            // ----- Act -----
            var value = cache.Cache<string>(cacheEntryKey, "value to store", expectedExpiryTime);

            // ----- Assert -----
            Assert.AreEqual("value to store", value);
        }

        [TestMethod]
        public void CacheManager_Cache_WhenCachingDateTime_ThenEngineCalledToCache()
        {
            // ----- Arrange -----
            var cacheEntryKey = new CacheEntryKey("test-key", 995010);
            var expectedKeyString = cacheEntryKey.ToString();
            var expectedExpiryTime = TimeSpan.FromMinutes(20d);

            var configurationMoq = new Mock<IOptions<CacheManagerConfiguration>>();
            CacheManagerTests.SetupConfigurationOptions(ref configurationMoq);

            var binarySerializerMoq = new Mock<IBinarySerializer>();
            CacheManagerTests.SetupBinarySerializerForFakeSerialize(ref binarySerializerMoq);

            var compressorFactoryMoq = new Mock<ICompressorFactory>();
            var engineMoq = new Mock<ICacheEngine>();
            engineMoq
                .Setup(_ => _.Cache(expectedKeyString, It.IsAny<byte[]>(), expectedExpiryTime))
                .Verifiable();

            var cache = new CacheManager(
                engineMoq.Object,
                configurationMoq.Object,
                binarySerializerMoq.Object,
                compressorFactoryMoq.Object);

            // ----- Act -----
            cache.Cache<DateTime>(cacheEntryKey, new DateTime(2007, 4, 9), expectedExpiryTime);

            // ----- Assert -----
            engineMoq.Verify(_ => _.Cache(expectedKeyString, It.IsAny<byte[]>(), expectedExpiryTime), Times.Once());
        }

        [TestMethod]
        public void CacheManager_Cache_WhenCachingDateTime_ThenSameValueReturned()
        {
            // ----- Arrange -----
            var cacheEntryKey = new CacheEntryKey("test-key", 995010);
            var expectedKeyString = cacheEntryKey.ToString();
            var expectedExpiryTime = TimeSpan.FromMinutes(20d);
            var dateValue = new DateTime(2007, 4, 9);

            var configurationMoq = new Mock<IOptions<CacheManagerConfiguration>>();
            CacheManagerTests.SetupConfigurationOptions(ref configurationMoq);

            var binarySerializerMoq = new Mock<IBinarySerializer>();
            binarySerializerMoq
                .Setup(_ => _.Serialize<DateTime>(dateValue))
                .Returns(new byte[] { 9, 18, 27, 36, 45, 54, 63, 72, 81 });

            binarySerializerMoq
                .Setup(_ => _.Deserialize<DateTime>(It.IsAny<byte[]>()))
                .Returns(new DateTime(dateValue.Ticks));

            var compressorFactoryMoq = new Mock<ICompressorFactory>();
            var engineMoq = new Mock<ICacheEngine>();

            var cache = new CacheManager(
                engineMoq.Object,
                configurationMoq.Object,
                binarySerializerMoq.Object,
                compressorFactoryMoq.Object);

            // ----- Act -----
            var value = cache.Cache<DateTime>(cacheEntryKey, dateValue, expectedExpiryTime);

            // ----- Assert -----
            Assert.AreEqual(new DateTime(2007, 4, 9), value);
        }

        [TestMethod]
        public void CacheManager_Cache_WhenCachingDateTime_ThenBinarySerializationPerformed()
        {
            // ----- Arrange -----
            var cacheEntryKey = new CacheEntryKey("test-key", 995010);
            var expectedKeyString = cacheEntryKey.ToString();
            var expectedExpiryTime = TimeSpan.FromMinutes(20d);
            var dateValue = new DateTime(2007, 4, 9);
            var givenSerializedValue = new byte[0];

            var configurationMoq = new Mock<IOptions<CacheManagerConfiguration>>();
            CacheManagerTests.SetupConfigurationOptions(ref configurationMoq);

            var binarySerializerMoq = new Mock<IBinarySerializer>();
            binarySerializerMoq
                .Setup(_ => _.Serialize<DateTime>(dateValue))
                .Returns(new byte[] { 9, 18, 27, 36, 45, 54, 63, 72, 81 })
                .Verifiable();

            binarySerializerMoq
                .Setup(_ => _.Deserialize<DateTime>(It.IsAny<byte[]>()))
                .Callback((byte[] b) => givenSerializedValue = b)
                .Returns(new DateTime(dateValue.Ticks))
                .Verifiable();

            var compressorFactoryMoq = new Mock<ICompressorFactory>();
            var engineMoq = new Mock<ICacheEngine>();

            var cache = new CacheManager(
                engineMoq.Object,
                configurationMoq.Object,
                binarySerializerMoq.Object,
                compressorFactoryMoq.Object);

            // ----- Act -----
            cache.Cache<DateTime>(cacheEntryKey, dateValue, expectedExpiryTime);

            // ----- Assert -----
            binarySerializerMoq.Verify(_ => _.Serialize<DateTime>(dateValue), Times.Once());
            binarySerializerMoq.Verify(_ => _.Deserialize<DateTime>(It.IsAny<byte[]>()), Times.Once());
            Assert.AreEqual(9, givenSerializedValue.Length);
            Assert.AreEqual(405, givenSerializedValue.Sum(_ => _));
        }

        [TestMethod]
        public void CacheManager_Cache_WhenCachingSerializableObject_ThenEngineCalledToCache()
        {
            // ----- Arrange -----
            var cacheEntryKey = new CacheEntryKey("test-key", 43210);
            var expectedKeyString = cacheEntryKey.ToString();
            var expectedExpiryTime = TimeSpan.FromMinutes(20d);

            var configurationMoq = new Mock<IOptions<CacheManagerConfiguration>>();
            CacheManagerTests.SetupConfigurationOptions(ref configurationMoq);

            var binarySerializerMoq = new Mock<IBinarySerializer>();
            CacheManagerTests.SetupBinarySerializerForFakeSerialize(ref binarySerializerMoq);

            var compressorFactoryMoq = new Mock<ICompressorFactory>();
            var engineMoq = new Mock<ICacheEngine>();
            engineMoq
                .Setup(_ => _.Cache(expectedKeyString, It.IsAny<byte[]>(), expectedExpiryTime))
                .Verifiable();

            var cache = new CacheManager(
                engineMoq.Object,
                configurationMoq.Object,
                binarySerializerMoq.Object,
                compressorFactoryMoq.Object);

            var list = new List<string> { "1", "2", "3", "4" };

            var fakedValue = new FakeTypeForTesting
            {
                Identifier = 900,
                Name = "p q r s t",
                First = new DateTime(2003, 1, 19),
                Hours = new List<TimeSpan> { TimeSpan.Zero, TimeSpan.FromHours(4.5d) },
                Lookups = new Dictionary<string, object>
                {
                    { "more1", "names" },
                    { "more2", 456d },
                    { "more3", list }
                }
            };

            // ----- Act -----
            cache.Cache<FakeTypeForTesting>(cacheEntryKey, fakedValue, expectedExpiryTime);

            // ----- Assert -----
            engineMoq.Verify(_ => _.Cache(expectedKeyString, It.IsAny<byte[]>(), expectedExpiryTime), Times.Once());
        }

        [TestMethod]
        public void CacheManager_Cache_WhenCachingSerializable_ThenSameValueReturnedNotSameInstance()
        {
            // ----- Arrange -----
            var cacheEntryKey = new CacheEntryKey("test-key", 43210);
            var expectedKeyString = cacheEntryKey.ToString();
            var expectedExpiryTime = TimeSpan.FromMinutes(20d);

            var list = new List<string> { "1", "2", "3", "4" };

            var fakedValue = new FakeTypeForTesting
            {
                Identifier = 900,
                Name = "p q r s t",
                First = new DateTime(2003, 1, 19),
                Hours = new List<TimeSpan> { TimeSpan.Zero, TimeSpan.FromHours(4.5d) },
                Lookups = new Dictionary<string, object>
                {
                    { "more1", "names" },
                    { "more2", 456d },
                    { "more3", list }
                }
            };

            var configurationMoq = new Mock<IOptions<CacheManagerConfiguration>>();
            CacheManagerTests.SetupConfigurationOptions(ref configurationMoq);

            var binarySerializerMoq = new Mock<IBinarySerializer>();
            binarySerializerMoq
                .Setup(_ => _.Serialize<FakeTypeForTesting>(fakedValue))
                .Returns(new byte[] { 9, 18, 27, 36, 45, 54, 63, 72, 81 });

            binarySerializerMoq
                .Setup(_ => _.Deserialize<FakeTypeForTesting>(It.IsAny<byte[]>()))
                .Returns(new FakeTypeForTesting(fakedValue));

            var compressorFactoryMoq = new Mock<ICompressorFactory>();
            var engineMoq = new Mock<ICacheEngine>();

            var cache = new CacheManager(
                engineMoq.Object,
                configurationMoq.Object,
                binarySerializerMoq.Object,
                compressorFactoryMoq.Object);

            // ----- Act -----
            var value = cache.Cache<FakeTypeForTesting>(cacheEntryKey, fakedValue, expectedExpiryTime);

            // ----- Assert -----
            Assert.AreNotSame(value, fakedValue, "the same object instance was returned.");
            Assert.IsNotNull(value);
            Assert.AreEqual(900, value.Identifier);
            Assert.AreEqual("p q r s t", value.Name);
            Assert.AreEqual(new DateTime(2003, 1, 19), value.First);
            Assert.AreEqual(2, value.Hours.Count);
            Assert.AreEqual(TimeSpan.Zero, value.Hours[0]);
            Assert.AreEqual(TimeSpan.FromHours(4.5d), value.Hours[1]);
            Assert.AreEqual(3, value.Lookups.Count);
            Assert.IsTrue(value.Lookups.ContainsKey("more1"));
            Assert.IsTrue(value.Lookups.ContainsKey("more2"));
            Assert.IsTrue(value.Lookups.ContainsKey("more3"));
            Assert.IsInstanceOfType(value.Lookups["more1"], typeof(string));
            Assert.IsInstanceOfType(value.Lookups["more2"], typeof(double));
            Assert.IsInstanceOfType(value.Lookups["more3"], typeof(List<string>));
        }

        [TestMethod]
        public void CacheManager_Cache_WhenCachingSerializable_ThenBinarySerializationPerformed()
        {
            // ----- Arrange -----
            var cacheEntryKey = new CacheEntryKey("test-key", 995010);
            var expectedKeyString = cacheEntryKey.ToString();
            var expectedExpiryTime = TimeSpan.FromMinutes(20d);
            var givenSerializedValue = new byte[0];

            var list = new List<string> { "1", "2", "3", "4" };

            var fakedValue = new FakeTypeForTesting
            {
                Identifier = 900,
                Name = "p q r s t",
                First = new DateTime(2003, 1, 19),
                Hours = new List<TimeSpan> { TimeSpan.Zero, TimeSpan.FromHours(4.5d) },
                Lookups = new Dictionary<string, object>
                {
                    { "more1", "names" },
                    { "more2", 456d },
                    { "more3", list }
                }
            };

            var configurationMoq = new Mock<IOptions<CacheManagerConfiguration>>();
            CacheManagerTests.SetupConfigurationOptions(ref configurationMoq);

            var binarySerializerMoq = new Mock<IBinarySerializer>();
            binarySerializerMoq
                .Setup(_ => _.Serialize<FakeTypeForTesting>(fakedValue))
                .Returns(new byte[] { 9, 18, 27, 36, 45, 54, 63, 72, 81 })
                .Verifiable();

            binarySerializerMoq
                .Setup(_ => _.Deserialize<FakeTypeForTesting>(It.IsAny<byte[]>()))
                .Callback((byte[] b) => givenSerializedValue = b)
                .Returns(new FakeTypeForTesting(fakedValue))
                .Verifiable();

            var compressorFactoryMoq = new Mock<ICompressorFactory>();
            var engineMoq = new Mock<ICacheEngine>();

            var cache = new CacheManager(
                engineMoq.Object,
                configurationMoq.Object,
                binarySerializerMoq.Object,
                compressorFactoryMoq.Object);

            // ----- Act -----
            cache.Cache<FakeTypeForTesting>(cacheEntryKey, fakedValue, expectedExpiryTime);

            // ----- Assert -----
            binarySerializerMoq.Verify(_ => _.Serialize<FakeTypeForTesting>(fakedValue), Times.Once());
            binarySerializerMoq.Verify(_ => _.Deserialize<FakeTypeForTesting>(It.IsAny<byte[]>()), Times.Once());
            Assert.AreEqual(9, givenSerializedValue.Length);
            Assert.AreEqual(405, givenSerializedValue.Sum(_ => _));
        }

        [TestMethod]
        public void CacheManager_Retrieve_WhenString_AndKeyNotPresent_ThenReturnsProvidedDefault()
        {
            // ----- Arrange -----
            var cacheEntryKey = new CacheEntryKey("test-key", 28463);
            var expectedKeyString = cacheEntryKey.ToString();
            var expectedExpiryTime = TimeSpan.FromMinutes(90d);

            var configurationMoq = new Mock<IOptions<CacheManagerConfiguration>>();
            CacheManagerTests.SetupConfigurationOptions(ref configurationMoq);

            var binarySerializerMoq = new Mock<IBinarySerializer>();
            var compressorFactoryMoq = new Mock<ICompressorFactory>();
            var engineMoq = new Mock<ICacheEngine>();
            engineMoq
                .Setup(_ => _.Retrieve(expectedKeyString))
                .Returns((byte[])null)
                .Verifiable();

            var cache = new CacheManager(
                engineMoq.Object,
                configurationMoq.Object,
                binarySerializerMoq.Object,
                compressorFactoryMoq.Object);

            // ----- Act -----
            var result = cache.Retrieve<string>(cacheEntryKey, "a b c d");

            // ----- Assert -----
            Assert.AreEqual("a b c d", result);
        }

        [TestMethod]
        public void CacheManager_Retrieve_WhenDecimal_AndKeyNotPresent_ThenReturnsProvidedDefault()
        {
            // ----- Arrange -----
            var cacheEntryKey = new CacheEntryKey("test-key", 28407);
            var expectedKeyString = cacheEntryKey.ToString();
            var expectedExpiryTime = TimeSpan.FromMinutes(21d);

            var configurationMoq = new Mock<IOptions<CacheManagerConfiguration>>();
            CacheManagerTests.SetupConfigurationOptions(ref configurationMoq);

            var binarySerializerMoq = new Mock<IBinarySerializer>();
            var compressorFactoryMoq = new Mock<ICompressorFactory>();
            var engineMoq = new Mock<ICacheEngine>();
            engineMoq
                .Setup(_ => _.Retrieve(expectedKeyString))
                .Returns((byte[])null)
                .Verifiable();

            var cache = new CacheManager(
                engineMoq.Object,
                configurationMoq.Object,
                binarySerializerMoq.Object,
                compressorFactoryMoq.Object);

            // ----- Act -----
            var result = cache.Retrieve<decimal>(cacheEntryKey, 1339m);

            // ----- Assert -----
            Assert.AreEqual(1339m, result);
        }

        [TestMethod]
        public void CacheManager_Retrieve_WhenTimeSpan_AndKeyNotPresent_ThenReturnsProvidedDefault()
        {
            // ----- Arrange -----
            var cacheEntryKey = new CacheEntryKey("test-key", 91407);
            var expectedKeyString = cacheEntryKey.ToString();
            var expectedExpiryTime = TimeSpan.FromMinutes(8d);

            var configurationMoq = new Mock<IOptions<CacheManagerConfiguration>>();
            CacheManagerTests.SetupConfigurationOptions(ref configurationMoq);

            var binarySerializerMoq = new Mock<IBinarySerializer>();
            var compressorFactoryMoq = new Mock<ICompressorFactory>();
            var engineMoq = new Mock<ICacheEngine>();
            engineMoq
                .Setup(_ => _.Retrieve(expectedKeyString))
                .Returns((byte[])null)
                .Verifiable();

            var cache = new CacheManager(
                engineMoq.Object,
                configurationMoq.Object,
                binarySerializerMoq.Object,
                compressorFactoryMoq.Object);

            // ----- Act -----
            var result = cache.Retrieve<TimeSpan>(cacheEntryKey, TimeSpan.FromMinutes(13.5d));

            // ----- Assert -----
            Assert.AreEqual(TimeSpan.FromMinutes(13.5d), result);
        }

        [TestMethod]
        public void CacheManager_Retrieve_WhenSerializable_AndKeyNotPresent_ThenReturnsProvidedDefault()
        {
            // ----- Arrange -----
            var cacheEntryKey = new CacheEntryKey("test-key", 2007);
            var expectedKeyString = cacheEntryKey.ToString();
            var expectedExpiryTime = TimeSpan.Zero;

            var configurationMoq = new Mock<IOptions<CacheManagerConfiguration>>();
            CacheManagerTests.SetupConfigurationOptions(ref configurationMoq);

            var binarySerializerMoq = new Mock<IBinarySerializer>();
            var compressorFactoryMoq = new Mock<ICompressorFactory>();
            var engineMoq = new Mock<ICacheEngine>();
            engineMoq
                .Setup(_ => _.Retrieve(expectedKeyString))
                .Returns((byte[])null)
                .Verifiable();

            var returnedType = new FakeTypeForTesting
            {
                Name = "a b c",
                First = new DateTime(2019, 12, 30),
                Identifier = 99
            };

            var cache = new CacheManager(
                engineMoq.Object,
                configurationMoq.Object,
                binarySerializerMoq.Object,
                compressorFactoryMoq.Object);

            // ----- Act -----
            var result = cache.Retrieve<FakeTypeForTesting>(cacheEntryKey, returnedType);

            // ----- Assert -----
            Assert.AreEqual(returnedType, result);
        }

        [TestMethod]
        public void CacheManager_Retrieve_WhenString_ThenEngineCalledToReadData()
        {
            // ----- Arrange -----
            var cacheEntryKey = new CacheEntryKey("test-key", 1163);
            var expectedKeyString = cacheEntryKey.ToString();
            var expectedExpiryTime = TimeSpan.FromMinutes(90d);

            var configurationMoq = new Mock<IOptions<CacheManagerConfiguration>>();
            CacheManagerTests.SetupConfigurationOptions(ref configurationMoq);

            var binarySerializerMoq = new Mock<IBinarySerializer>();
            var compressorFactoryMoq = new Mock<ICompressorFactory>();
            var engineMoq = new Mock<ICacheEngine>();
            engineMoq
                .Setup(_ => _.Retrieve(expectedKeyString))
                .Returns(new byte[] { 10, 21, 32, 43, 54, 65, 76, 87, 98 })
                .Verifiable();

            var cache = new CacheManager(
                engineMoq.Object,
                configurationMoq.Object,
                binarySerializerMoq.Object,
                compressorFactoryMoq.Object);

            // ----- Act -----
            cache.Retrieve<string>(cacheEntryKey);

            // ----- Assert -----
            engineMoq.Verify(_ => _.Retrieve(expectedKeyString), Times.Once());
        }

        [TestMethod]
        public void CacheManager_Retrieve_WhenArrayOfIntegers_ThenEngineCalledToReadData()
        {
            // ----- Arrange -----
            var cacheEntryKey = new CacheEntryKey("test-key", 6163);
            var expectedKeyString = cacheEntryKey.ToString();
            var expectedExpiryTime = TimeSpan.FromMinutes(41d);

            var configurationMoq = new Mock<IOptions<CacheManagerConfiguration>>();
            CacheManagerTests.SetupConfigurationOptions(ref configurationMoq);

            var binarySerializerMoq = new Mock<IBinarySerializer>();
            var compressorFactoryMoq = new Mock<ICompressorFactory>();
            var engineMoq = new Mock<ICacheEngine>();
            engineMoq
                .Setup(_ => _.Retrieve(expectedKeyString))
                .Returns(new byte[] { 10, 21, 32, 43, 54, 65, 76, 87, 98 })
                .Verifiable();

            var cache = new CacheManager(
                engineMoq.Object,
                configurationMoq.Object,
                binarySerializerMoq.Object,
                compressorFactoryMoq.Object);

            // ----- Act -----
            cache.Retrieve<int[]>(cacheEntryKey);

            // ----- Assert -----
            engineMoq.Verify(_ => _.Retrieve(expectedKeyString), Times.Once());
        }

        [TestMethod]
        public void CacheManager_Retrieve_WhenSerializableObject_ThenEngineCalledToReadData()
        {
            // ----- Arrange -----
            var cacheEntryKey = new CacheEntryKey("test-key", 8024);
            var expectedKeyString = cacheEntryKey.ToString();
            var expectedExpiryTime = TimeSpan.FromMinutes(13);

            var configurationMoq = new Mock<IOptions<CacheManagerConfiguration>>();
            CacheManagerTests.SetupConfigurationOptions(ref configurationMoq);

            var binarySerializerMoq = new Mock<IBinarySerializer>();
            var compressorFactoryMoq = new Mock<ICompressorFactory>();
            var engineMoq = new Mock<ICacheEngine>();
            engineMoq
                .Setup(_ => _.Retrieve(expectedKeyString))
                .Returns(new byte[] { 10, 21, 32, 43, 54, 65, 76, 87, 98 })
                .Verifiable();

            var cache = new CacheManager(
                engineMoq.Object,
                configurationMoq.Object,
                binarySerializerMoq.Object,
                compressorFactoryMoq.Object);

            // ----- Act -----
            cache.Retrieve<FakeTypeForTesting>(cacheEntryKey);

            // ----- Assert -----
            engineMoq.Verify(_ => _.Retrieve(expectedKeyString), Times.Once());
        }

        [TestMethod]
        public void CacheManager_Cache_WhenRetrievingSerializable_ThenBinaryDeserializationPerformed()
        {
            // ----- Arrange -----
            var cacheEntryKey = new CacheEntryKey("test-key", 10);
            var expectedKeyString = cacheEntryKey.ToString();
            var expectedExpiryTime = TimeSpan.FromMinutes(30d);
            var givenSerializedValue = new byte[0];

            var list = new List<string> { "1", "2", "3", "4" };

            var fakedValue = new FakeTypeForTesting
            {
                Identifier = 900,
                Name = "p q r s t",
                First = new DateTime(2003, 1, 19),
                Hours = new List<TimeSpan> { TimeSpan.Zero, TimeSpan.FromHours(4.5d) },
                Lookups = new Dictionary<string, object>
                {
                    { "more1", "names" },
                    { "more2", 456d },
                    { "more3", list }
                }
            };

            var configurationMoq = new Mock<IOptions<CacheManagerConfiguration>>();
            CacheManagerTests.SetupConfigurationOptions(ref configurationMoq);

            var binarySerializerMoq = new Mock<IBinarySerializer>();
            binarySerializerMoq
                .Setup(_ => _.Deserialize<FakeTypeForTesting>(It.IsAny<byte[]>()))
                .Callback((byte[] b) => givenSerializedValue = b)
                .Returns(fakedValue)
                .Verifiable();

            var compressorFactoryMoq = new Mock<ICompressorFactory>();
            var engineMoq = new Mock<ICacheEngine>();
            engineMoq
                .Setup(_ => _.Retrieve(expectedKeyString))
                .Returns(new byte[] { 9, 81, 72, 63, 54, 45, 36, 27, 18 })
                .Verifiable();

            var cache = new CacheManager(
                engineMoq.Object,
                configurationMoq.Object,
                binarySerializerMoq.Object,
                compressorFactoryMoq.Object);

            // ----- Act -----
            cache.Retrieve<FakeTypeForTesting>(cacheEntryKey);

            // ----- Assert -----
            engineMoq.Verify(_ => _.Retrieve(expectedKeyString), Times.Once());
            binarySerializerMoq.Verify(_ => _.Deserialize<FakeTypeForTesting>(It.IsAny<byte[]>()), Times.Once());
            Assert.AreEqual(9, givenSerializedValue.Length);
            Assert.AreEqual(405, givenSerializedValue.Sum(_ => _));
        }

        [TestMethod]
        public void CacheManager_Constructor_WhenNoCompressionOption_ThenCompressorFactoryNotUsed()
        {
            // ----- Arrange -----
            var compressorFactoryMoq = new Mock<ICompressorFactory>();
            compressorFactoryMoq
                .Setup(_ => _.Create<IGzipCompressor>(It.IsAny<CompressorConfiguration>()))
                .Verifiable();

            compressorFactoryMoq
                .Setup(_ => _.Create<IDeflateCompressor>(It.IsAny<CompressorConfiguration>()))
                .Verifiable();

            var cacheEngineMoq = new Mock<ICacheEngine>();
            var configurationOptionsMoq = new Mock<IOptions<CacheManagerConfiguration>>();
            configurationOptionsMoq
                .Setup(_ => _.Value)
                .Returns(new CacheManagerConfiguration { UseDeflateCompression = false, UseGzipCompression = false })
                .Verifiable();

            var binarySerializerMoq = new Mock<IBinarySerializer>();

            // ----- Act -----
            var cache = new CacheManager(
                cacheEngineMoq.Object,
                configurationOptionsMoq.Object,
                binarySerializerMoq.Object,
                compressorFactoryMoq.Object);

            // ----- Assert -----
            configurationOptionsMoq.Verify(_ => _.Value, Times.AtLeastOnce());
            compressorFactoryMoq.Verify(_ => _.Create<IGzipCompressor>(It.IsAny<CompressorConfiguration>()), Times.Never());
            compressorFactoryMoq.Verify(_ => _.Create<IDeflateCompressor>(It.IsAny<CompressorConfiguration>()), Times.Never());
        }

        [TestMethod]
        public void CacheManager_Constructor_WhenGzipCompressionOption_ThenGzipCompressorCreatedFromFactory()
        {
            // ----- Arrange -----
            var compressConfig = new CompressorConfiguration
            {
                CompressionLevel = System.IO.Compression.CompressionLevel.Optimal,
                MinimumSizeToCompress = 100
            };

            var compressorFactoryMoq = new Mock<ICompressorFactory>();
            compressorFactoryMoq
                .Setup(_ => _.Create<IGzipCompressor>(compressConfig))
                .Verifiable();

            compressorFactoryMoq
                .Setup(_ => _.Create<IDeflateCompressor>(It.IsAny<CompressorConfiguration>()))
                .Verifiable();

            var cacheEngineMoq = new Mock<ICacheEngine>();
            var configurationOptionsMoq = new Mock<IOptions<CacheManagerConfiguration>>();
            configurationOptionsMoq
                .Setup(_ => _.Value)
                .Returns(new CacheManagerConfiguration { UseDeflateCompression = false, UseGzipCompression = true, CompressionConfiguration = compressConfig })
                .Verifiable();

            var binarySerializerMoq = new Mock<IBinarySerializer>();

            // ----- Act -----
            var cache = new CacheManager(
                cacheEngineMoq.Object,
                configurationOptionsMoq.Object,
                binarySerializerMoq.Object,
                compressorFactoryMoq.Object);

            // ----- Assert -----
            configurationOptionsMoq.Verify(_ => _.Value, Times.AtLeastOnce());
            compressorFactoryMoq.Verify(_ => _.Create<IGzipCompressor>(compressConfig), Times.Once());
            compressorFactoryMoq.Verify(_ => _.Create<IDeflateCompressor>(It.IsAny<CompressorConfiguration>()), Times.Never());
        }

        [TestMethod]
        public void CacheManager_Constructor_WhenDeflateCompressionOption_ThenDeflateCompressorCreatedFromFactory()
        {
            // ----- Arrange -----
            var compressConfig = new CompressorConfiguration
            {
                CompressionLevel = System.IO.Compression.CompressionLevel.Fastest,
                MinimumSizeToCompress = 50
            };

            var compressorFactoryMoq = new Mock<ICompressorFactory>();
            compressorFactoryMoq
                .Setup(_ => _.Create<IGzipCompressor>(It.IsAny<CompressorConfiguration>()))
                .Verifiable();

            compressorFactoryMoq
                .Setup(_ => _.Create<IDeflateCompressor>(compressConfig))
                .Verifiable();

            var cacheEngineMoq = new Mock<ICacheEngine>();
            var configurationOptionsMoq = new Mock<IOptions<CacheManagerConfiguration>>();
            configurationOptionsMoq
                .Setup(_ => _.Value)
                .Returns(new CacheManagerConfiguration { UseDeflateCompression = true, UseGzipCompression = false, CompressionConfiguration = compressConfig })
                .Verifiable();

            var binarySerializerMoq = new Mock<IBinarySerializer>();

            // ----- Act -----
            var cache = new CacheManager(
                cacheEngineMoq.Object,
                configurationOptionsMoq.Object,
                binarySerializerMoq.Object,
                compressorFactoryMoq.Object);

            // ----- Assert -----
            configurationOptionsMoq.Verify(_ => _.Value, Times.AtLeastOnce());
            compressorFactoryMoq.Verify(_ => _.Create<IDeflateCompressor>(compressConfig), Times.Once());
            compressorFactoryMoq.Verify(_ => _.Create<IGzipCompressor>(It.IsAny<CompressorConfiguration>()), Times.Never());
        }

        [TestMethod]
        public void CacheManager_RetirieveOrRecache_WhenKeyIsPresent_ThenRetrievesAndDoesntCache()
        {
            // ----- Arrange -----
            var cacheEntryKey = new CacheEntryKey("test-node", 789, new DateTime(2017, 4, 24));
            var compressorFactoryMoq = new Mock<ICompressorFactory>();
            var cacheEngineMoq = new Mock<ICacheEngine>();
            cacheEngineMoq
                .Setup(_ => _.Retrieve(cacheEntryKey.ToString()))
                .Returns(new byte[] { 0, 100, 200, 0, 50, 150, 250, 0, 25, 75, 125, 175, 225 })
                .Verifiable();

            cacheEngineMoq
                .Setup(_ => _.Cache(cacheEntryKey.ToString(), It.IsAny<byte[]>(), It.IsAny<TimeSpan>()))
                .Verifiable();

            var configurationOptionsMoq = new Mock<IOptions<CacheManagerConfiguration>>();
            var binarySerializerMoq = new Mock<IBinarySerializer>();
            binarySerializerMoq
                .Setup(_ => _.Deserialize<string>(It.IsAny<byte[]>()))
                .Returns("the cached value");

            binarySerializerMoq
                .Setup(_ => _.Serialize<string>("the replacement cached value"))
                .Returns(new byte[] { 0, 100, 200, 0, 50, 150, 250, 0, 25, 75, 125, 175, 225 });

            var cache = new CacheManager(
                cacheEngineMoq.Object,
                configurationOptionsMoq.Object,
                binarySerializerMoq.Object,
                compressorFactoryMoq.Object);

            // ----- Act -----
            var result = cache.RetrieveOrRecache<string>(
                cacheEntryKey,
                () => "the replacement cached value",
                TimeSpan.FromHours(1.5d));

            // ----- Assert -----
            Assert.AreEqual("the cached value", result);
            cacheEngineMoq.Verify(_ => _.Retrieve(cacheEntryKey.ToString()), Times.Once());
            cacheEngineMoq.Verify(_ => _.Cache(cacheEntryKey.ToString(), It.IsAny<byte[]>(), It.IsAny<TimeSpan>()), Times.Never());
        }

        [TestMethod]
        public void CacheManager_RetirieveOrRecache_WhenKeyIsNotPresent_ThenExecutesFunctionAndCaches()
        {
            // ----- Arrange -----
            var callToFuncHappened = false;
            Func<string> funcToGetValue = () =>
            {
                callToFuncHappened = true;
                return "the replacement cached value";
            };

            var cacheEntryKey = new CacheEntryKey("test-node", 440, "green");
            var compressorFactoryMoq = new Mock<ICompressorFactory>();
            var cacheEngineMoq = new Mock<ICacheEngine>();
            cacheEngineMoq
                .Setup(_ => _.Retrieve(cacheEntryKey.ToString()))
                .Returns((byte[])null)
                .Verifiable();

            cacheEngineMoq
                .Setup(_ => _.Cache(cacheEntryKey.ToString(), It.IsAny<byte[]>(), TimeSpan.FromHours(1.5d)))
                .Verifiable();

            var configurationOptionsMoq = new Mock<IOptions<CacheManagerConfiguration>>();
            var binarySerializerMoq = new Mock<IBinarySerializer>();
            binarySerializerMoq
                .Setup(_ => _.Serialize<string>("the replacement cached value"))
                .Returns(new byte[] { 0, 100, 200, 0, 50, 150, 250, 0, 25, 75, 125, 175, 225 });

            var cache = new CacheManager(
                cacheEngineMoq.Object,
                configurationOptionsMoq.Object,
                binarySerializerMoq.Object,
                compressorFactoryMoq.Object);

            // ----- Act -----
            var result = cache.RetrieveOrRecache<string>(
                cacheEntryKey,
                funcToGetValue,
                TimeSpan.FromHours(1.5d));

            // ----- Assert -----
            Assert.AreEqual("the replacement cached value", result);
            cacheEngineMoq.Verify(_ => _.Retrieve(cacheEntryKey.ToString()), Times.Once());
            cacheEngineMoq.Verify(_ => _.Cache(cacheEntryKey.ToString(), It.IsAny<byte[]>(), TimeSpan.FromHours(1.5d)), Times.Once());
            Assert.IsTrue(callToFuncHappened);
        }
        
        [TestMethod]
        public void CacheManager_Retirieve_WhenCompressionConfigured_ThenUtilisesStreamDecompression()
        {
            // ----- Arrange -----
            var cacheEntryKey = new CacheEntryKey("test-node-nine");
            var cacheEngineMoq = new Mock<ICacheEngine>();
            cacheEngineMoq
                .Setup(_ => _.Retrieve(cacheEntryKey.ToString()))
                .Returns(new byte[] { 0, 100, 200, 0, 50, 150, 250, 0, 25, 75, 125, 175, 225 });

            var configurationOptionsMoq = new Mock<IOptions<CacheManagerConfiguration>>();
            configurationOptionsMoq
                .Setup(_ => _.Value)
                .Returns(new CacheManagerConfiguration { UseDeflateCompression = true });

            var streamCompressionMoq = new Mock<IDeflateCompressor>();
            streamCompressionMoq
                .Setup(_ => _.DecompressStream(It.IsAny<Stream>(), It.IsAny<Stream>()))
                .Callback((Stream i, Stream o) => i.CopyTo(o))
                .Verifiable();

            var compressorFactoryMoq = new Mock<ICompressorFactory>();
            compressorFactoryMoq
                .Setup(_ => _.Create<IDeflateCompressor>(It.IsAny<CompressorConfiguration>()))
                .Returns(streamCompressionMoq.Object)
                .Verifiable();

            var binarySerializerMoq = new Mock<IBinarySerializer>();
            binarySerializerMoq
                .Setup(_ => _.DeserializeFromStream<string>(It.IsAny<Stream>()))
                .Returns((Stream s) => "the light at the end of the tunnel is an oncoming train.");

            var cache = new CacheManager(
                cacheEngineMoq.Object,
                configurationOptionsMoq.Object,
                binarySerializerMoq.Object,
                compressorFactoryMoq.Object);

            // ----- Act -----
            var result = cache.Retrieve<string>(cacheEntryKey);

            // ----- Assert -----
            Assert.AreEqual("the light at the end of the tunnel is an oncoming train.", result);
            streamCompressionMoq.Verify(_ => _.DecompressStream(It.IsAny<Stream>(), It.IsAny<Stream>()), Times.Once());
            binarySerializerMoq.Verify(_ => _.DeserializeFromStream<string>(It.IsAny<Stream>()), Times.Once());
        }

        [TestMethod]
        public void CacheManager_Cache_WhenCompressionConfigured_ThenUtilisesStreamCompression()
        {
            // ----- Arrange -----
            var expectedCacheValue = new byte[] { 131, 144, 76, 223, 0, 0, 144, 76, 131, 0, 0 };

            var cacheEntryKey = new CacheEntryKey("test-1-2-3");
            var cacheEngineMoq = new Mock<ICacheEngine>();
            cacheEngineMoq
                .Setup(_ => _.Cache(cacheEntryKey.ToString(), expectedCacheValue, TimeSpan.FromMinutes(30d)))
                .Verifiable();

            var configurationOptionsMoq = new Mock<IOptions<CacheManagerConfiguration>>();
            configurationOptionsMoq
                .Setup(_ => _.Value)
                .Returns(new CacheManagerConfiguration { UseDeflateCompression = true });

            var streamCompressionMoq = new Mock<IDeflateCompressor>();
            streamCompressionMoq
                .Setup(_ => _.CompressStream(It.IsAny<Stream>(), It.IsAny<Stream>()))
                .Callback((Stream i, Stream o) =>
                {
                    i.Seek(0L, SeekOrigin.Begin);
                    i.CopyTo(o);
                    o.Flush();
                })
                .Verifiable();

            var compressorFactoryMoq = new Mock<ICompressorFactory>();
            compressorFactoryMoq
                .Setup(_ => _.Create<IDeflateCompressor>(It.IsAny<CompressorConfiguration>()))
                .Returns(streamCompressionMoq.Object)
                .Verifiable();

            var binarySerializerMoq = new Mock<IBinarySerializer>();
            binarySerializerMoq
                .Setup(_ => _.SerializeToStream<string>("around and 'round the mulberry bush...", It.IsAny<Stream>()))
                .Callback((string v, Stream o) =>
                {
                    o.Write(expectedCacheValue, 0, 11);
                    o.Flush();
                })
                .Verifiable();

            var cache = new CacheManager(
                cacheEngineMoq.Object,
                configurationOptionsMoq.Object,
                binarySerializerMoq.Object,
                compressorFactoryMoq.Object);

            // ----- Act -----
            cache.Cache<string>(
                cacheEntryKey,
                "around and 'round the mulberry bush...",
                TimeSpan.FromMinutes(30d));

            // ----- Assert -----
            streamCompressionMoq.Verify(_ => _.CompressStream(It.IsAny<Stream>(), It.IsAny<Stream>()), Times.Once());
            binarySerializerMoq.Verify(_ => _.SerializeToStream<string>("around and 'round the mulberry bush...", It.IsAny<Stream>()), Times.Once());
            cacheEngineMoq.Verify(_ => _.Cache(cacheEntryKey.ToString(), expectedCacheValue, TimeSpan.FromMinutes(30d)), Times.Once());
        }

        private static void SetupConfigurationOptions(ref Mock<IOptions<CacheManagerConfiguration>> configurationMoq)
        {
            configurationMoq
                .Setup(_ => _.Value)
                .Returns(new CacheManagerConfiguration { CacheEngineConfiguration = new CacheEngineConfiguration() });
        }

        private static void SetupBinarySerializerForFakeSerialize(ref Mock<IBinarySerializer> binarySerializerMoq)
        {
            binarySerializerMoq
                .Setup(_ => _.Serialize(It.IsAny<object>()))
                .Returns(new byte[] { 9, 8, 7, 6, 5, 4, 3, 2, 1 });
        }
    }

    #region classes for faking

    public class FakeTypeForTesting
    {
        public FakeTypeForTesting()
        {
        }

        public FakeTypeForTesting(FakeTypeForTesting other)
        {
            this.First = other.First;
            this.Hours = other.Hours;
            this.Identifier = other.Identifier;
            this.Lookups = other.Lookups;
            this.Name = other.Name;
        }

        public int Identifier { get; set; }

        public string Name { get; set; }

        public DateTime First { get; set; }

        public IList<TimeSpan> Hours { get; set; }

        public Dictionary<string, object> Lookups { get; set; }
    }

    #endregion classes for faking
}
