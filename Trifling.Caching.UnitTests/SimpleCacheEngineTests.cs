namespace Trifling.Caching.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Trifling.Caching.Impl;

    /// <summary>
    /// Unit tests for the <see cref="SimpleCacheEngine"/> class. 
    /// </summary>
    [TestClass]
    public class SimpleCacheEngineTests
    {
        [TestMethod]
        public void SimpleCacheEngineTest_Cache_WhenExpiryTimeIsDaysLater_ThenRetainedInCache()
        {
            // ----- Arrange -----
            const string CacheEntryKey = "first key value";
            var engine = new SimpleCacheEngine();

            // ----- Act -----
            engine.Cache(
                CacheEntryKey,
                new byte[] { 3, 4, 5, 6, 7, 8, 9, 0, 1, 2 },
                TimeSpan.FromHours(240d));

            var retrieved = engine.Retrieve(CacheEntryKey);

            // ----- Assert -----
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(10, retrieved.Length);
            Assert.AreEqual(3, retrieved[0]);
            Assert.AreEqual(45, retrieved.Sum(x => x));
        }

        [TestMethod]
        public void SimpleCacheEngineTest_Cache_WhenExpiryTimeIsSecondsLater_ThenRemovedFromCache()
        {
            // ----- Arrange -----
            const string CacheEntryKey = "another/key/name";
            var engine = new SimpleCacheEngine();

            // ----- Act -----
            engine.Cache(
                CacheEntryKey,
                new byte[] { 3, 4, 5, 6, 7, 8, 9, 0, 1, 2 },
                TimeSpan.FromSeconds(0.64d));

            Thread.Sleep(1100);

            var retrieved = engine.Retrieve(CacheEntryKey);

            // ----- Assert -----
            Assert.IsNull(retrieved);
        }

        [TestMethod]
        public void SimpleCacheEngineTest_Remove_WhenRemoveExistingItem_ThenRemovedFromCache()
        {
            // ----- Arrange -----
            const string CacheEntryKey = "key/for/remove/test/0001";
            var engine = new SimpleCacheEngine();

            engine.Cache(
                CacheEntryKey,
                new byte[] { 3, 4, 5, 6, 7, 8, 9, 0, 1, 2 },
                TimeSpan.FromMinutes(20.5d));

            // ----- Act -----
            var removed = engine.Remove(CacheEntryKey);

            var retrieved = engine.Retrieve(CacheEntryKey);

            // ----- Assert -----
            Assert.IsTrue(removed);
            Assert.IsNull(retrieved);
        }

        [TestMethod]
        public void SimpleCacheEngineTest_Remove_WhenRemoveNonexistentItem_ThenReturnsFalse()
        {
            // ----- Arrange -----
            const string CacheEntryKey = "key/for/remove/test/2222";
            var engine = new SimpleCacheEngine();

            // ----- Act -----
            var removed = engine.Remove(CacheEntryKey);

            var retrieved = engine.Retrieve(CacheEntryKey);

            // ----- Assert -----
            Assert.IsFalse(removed);
            Assert.IsNull(retrieved);
        }

        [TestMethod]
        public void SimpleCacheEngineTest_CacheAsSet_WhenSetOfDecimalsCreated_ThenRetrieveSetReturnsDecimals()
        {
            // ----- Arrange -----
            var cacheKey = "create-new-set/1";
            var listValues = new List<decimal> { 34.4m, 8.12m, 9m, 101022.0003m, 0.000000000046m };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);

            // ----- Act -----
            engine.CacheAsSet(cacheKey, listValues, TimeSpan.FromSeconds(6.3333d));

            var retrieved = engine.RetrieveSet<decimal>(cacheKey);

            // ----- Assert -----
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(listValues.Count, retrieved.Count);
            Assert.AreNotSame(listValues, retrieved, "the engine cheated and returned the exact same instance.");
            for (var i = 0; i < listValues.Count; i++)
            {
                Assert.IsTrue(retrieved.Contains(listValues[i]), "expected set values did not match at " + i);
            }
        }

        [TestMethod]
        public void SimpleCacheEngineTest_CacheAsSet_WhenSetOfDateTimesCreated_ThenRetrieveSetReturnsDateTimes()
        {
            // ----- Arrange -----
            var cacheKey = "create-new-set-of-dates";
            var listValues = new List<DateTime>
            {
                new DateTime(2013, 1, 21, 14, 29, 50, 0),
                new DateTime(2018, 10, 31, 7, 2, 27, 400),
                new DateTime(2003, 4, 2, 17, 40, 13, 550)
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);

            // ----- Act -----
            engine.CacheAsSet(cacheKey, listValues, TimeSpan.FromSeconds(5.5d));

            var retrieved = engine.RetrieveSet<DateTime>(cacheKey);

            // ----- Assert -----
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(listValues.Count, retrieved.Count);
            Assert.AreNotSame(listValues, retrieved, "the engine cheated and returned the exact same instance.");
            for (var i = 0; i < listValues.Count; i++)
            {
                Assert.IsTrue(retrieved.Contains(listValues[i]), "expected set values did not match at " + i);
            }
        }

        [TestMethod]
        public void SimpleCacheEngineTest_CacheAsSet_WhenSetOfLongsCreated_ThenRetrieveSetReturnsLongs()
        {
            // ----- Arrange -----
            var cacheKey = "create-new-set-of-longs";
            var listValues = new List<long>
            {
                4898L,
                330401L,
                5L,
                204050607080L,
                971941410033032L
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);

            // ----- Act -----
            engine.CacheAsSet(cacheKey, listValues, TimeSpan.FromSeconds(6.1d));

            var retrieved = engine.RetrieveSet<long>(cacheKey);

            // ----- Assert -----
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(listValues.Count, retrieved.Count);
            Assert.AreNotSame(listValues, retrieved, "the engine cheated and returned the exact same instance.");
            for (var i = 0; i < listValues.Count; i++)
            {
                Assert.IsTrue(retrieved.Contains(listValues[i]), "expected set values did not match at " + i);
            }
        }

        [TestMethod]
        public void SimpleCacheEngineTest_CacheAsSet_WhenSetOfDoublesCreated_ThenRetrieveSetReturnsDoubles()
        {
            // ----- Arrange -----
            var cacheKey = "create-new-set-of-doubles";
            var listValues = new List<double>
            {
                44d,
                454184069335d,
                5.0000000000022d,
                0.0120110155332d,
                891440125700.10408883333d,
                78366020d / 6d
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);

            // ----- Act -----
            engine.CacheAsSet(cacheKey, listValues, TimeSpan.FromSeconds(8d));

            var retrieved = engine.RetrieveSet<double>(cacheKey);

            // ----- Assert -----
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(listValues.Count, retrieved.Count);
            Assert.AreNotSame(listValues, retrieved, "the engine cheated and returned the exact same instance.");
            for (var i = 0; i < listValues.Count; i++)
            {
                Assert.IsTrue(retrieved.Any(x => Math.Abs(x - listValues[i]) <= 0.0000000000001d), "expected set values did not match at " + i);
            }
        }

        [TestMethod]
        public void SimpleCacheEngineTest_AddToSet_WhenSetDoesntContainItem_ThenReturnsTrue()
        {
            // ----- Arrange -----
            var cacheKey = "add-new-item-to-set/1";
            var listValues = new List<int>
            {
                81,
                466,
                300,
                78
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsSet(cacheKey, listValues, TimeSpan.FromSeconds(7.4d));

            // ----- Act -----
            var result = engine.AddToSet(cacheKey, 807045);

            // ----- Assert -----
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void SimpleCacheEngineTest_AddToSet_WhenSetContainsItem_ThenReturnsFalse()
        {
            // ----- Arrange -----
            var cacheKey = "add-new-item-to-set/3";
            var listValues = new List<int>
            {
                6070,
                100971,
                130,
                8
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsSet(cacheKey, listValues, TimeSpan.FromSeconds(7.4d));

            // ----- Act -----
            var result = engine.AddToSet(cacheKey, 130);

            // ----- Assert -----
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SimpleCacheEngineTest_AddToSet_WhenSetDoesntContainItem_ThenItemIsAddedToSet()
        {
            // ----- Arrange -----
            var cacheKey = "add-new-item-to-set/2";
            var listValues = new List<int>
            {
                81,
                466,
                300,
                78
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsSet(cacheKey, listValues, TimeSpan.FromSeconds(7.4d));

            // ----- Act -----
            engine.AddToSet(cacheKey, 807045);

            var newList = engine.RetrieveSet<int>(cacheKey);

            // ----- Assert -----
            Assert.IsNotNull(newList);
            Assert.AreEqual(5, newList.Count);
            Assert.IsTrue(newList.Contains(807045));
        }

        [TestMethod]
        public void SimpleCacheEngineTest_RetrieveSet_WhenCacheEntryKeyDoesntExist_ThenReturnsNull()
        {
            // ----- Arrange -----
            var cacheKey = "retrieve-set-not-found/0/0";

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);

            // ----- Act -----
            var result = engine.RetrieveSet<int>(cacheKey);

            // ----- Assert -----
            Assert.IsNull(result);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_CacheAsList_WhenListOfDecimalsCreated_ThenRetrieveListReturnsDecimals()
        {
            // ----- Arrange -----
            var cacheKey = "create-new-list/1";
            var listValues = new List<decimal> { 34.4m, 8.12m, 9m, 101022.0003m, 0.000000000046m };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);

            // ----- Act -----
            engine.CacheAsList(cacheKey, listValues, TimeSpan.FromSeconds(6.3333d));

            var retrieved = engine.RetrieveList<decimal>(cacheKey);

            // ----- Assert -----
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(listValues.Count, retrieved.Count);
            Assert.AreNotSame(listValues, retrieved, "the engine cheated and returned the exact same instance.");
            for (var i = 0; i < listValues.Count; i++)
            {
                // this test ensures ordering is maintained
                Assert.AreEqual(listValues[i], retrieved[i], "expected list values did not match at " + i);
            }
        }

        [TestMethod]
        public void SimpleCacheEngineTests_CacheAsList_WhenListOfDateTimesCreated_ThenRetrieveListReturnsDateTimes()
        {
            // ----- Arrange -----
            var cacheKey = "create-new-list-of-dates";
            var listValues = new List<DateTime>
            {
                new DateTime(2013, 1, 21, 14, 29, 50, 0),
                new DateTime(2018, 10, 31, 7, 2, 27, 400),
                new DateTime(2003, 4, 2, 17, 40, 13, 550)
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);

            // ----- Act -----
            engine.CacheAsList(cacheKey, listValues, TimeSpan.FromSeconds(5.5d));

            var retrieved = engine.RetrieveList<DateTime>(cacheKey);

            // ----- Assert -----
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(listValues.Count, retrieved.Count);
            Assert.AreNotSame(listValues, retrieved, "the engine cheated and returned the exact same instance.");
            for (var i = 0; i < listValues.Count; i++)
            {
                // this test ensures ordering is maintained
                Assert.AreEqual(listValues[i], retrieved[i], "expected list values did not match at " + i);
            }
        }

        [TestMethod]
        public void SimpleCacheEngineTests_CacheAsList_WhenListOfLongsCreated_ThenRetrieveListReturnsLongs()
        {
            // ----- Arrange -----
            var cacheKey = "create-new-list-of-longs";
            var listValues = new List<long>
            {
                4898L,
                330401L,
                5L,
                204050607080L,
                971941410033032L
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);

            // ----- Act -----
            engine.CacheAsList(cacheKey, listValues, TimeSpan.FromSeconds(6.1d));

            var retrieved = engine.RetrieveList<long>(cacheKey);

            // ----- Assert -----
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(listValues.Count, retrieved.Count);
            Assert.AreNotSame(listValues, retrieved, "the engine cheated and returned the exact same instance.");
            for (var i = 0; i < listValues.Count; i++)
            {
                // this test ensures ordering is maintained
                Assert.AreEqual(listValues[i], retrieved[i], "expected list values did not match at " + i);
            }
        }

        [TestMethod]
        public void SimpleCacheEngineTests_CacheAsList_WhenListOfDoublesCreated_ThenRetrieveListReturnsDoubles()
        {
            // ----- Arrange -----
            var cacheKey = "create-new-list-of-doubles";
            var listValues = new List<double>
            {
                44d,
                454184069335d,
                5.0000000000022d,
                0.0120110155332d,
                891440125700.10408883333d,
                78366020d / 6d
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);

            // ----- Act -----
            engine.CacheAsList(cacheKey, listValues, TimeSpan.FromSeconds(8d));

            var retrieved = engine.RetrieveList<double>(cacheKey);

            // ----- Assert -----
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(listValues.Count, retrieved.Count);
            Assert.AreNotSame(listValues, retrieved, "the engine cheated and returned the exact same instance.");
            for (var i = 0; i < listValues.Count; i++)
            {
                // this test ensures ordering is maintained
                Assert.AreEqual(listValues[i], retrieved[i], 0.0000000000001d, "expected list values did not match at " + i);
            }
        }

        [TestMethod]
        public void SimpleCacheEngineTests_CacheAsList_WhenDuplicateEntriesListed_ThenRetrieveListReturnsDuplicateEntries()
        {
            // ----- Arrange -----
            var cacheKey = "create-new-list-of-duplicated";
            var listValues = new List<DateTime>
            {
                new DateTime(2010, 9, 17),
                new DateTime(2019, 12, 28),
                new DateTime(2016, 2, 29),
                new DateTime(2019, 12, 28),
                new DateTime(2019, 12, 28),
                new DateTime(2004, 4, 14)
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);

            // ----- Act -----
            engine.CacheAsList(cacheKey, listValues, TimeSpan.FromSeconds(6d));

            var retrieved = engine.RetrieveList<DateTime>(cacheKey);

            // ----- Assert -----
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(listValues.Count, retrieved.Count);
            Assert.AreNotSame(listValues, retrieved, "the engine cheated and returned the exact same instance.");
            for (var i = 0; i < listValues.Count; i++)
            {
                // this test ensures ordering is maintained
                Assert.AreEqual(listValues[i], retrieved[i], "expected list values did not match at " + i);
            }
        }

        [TestMethod]
        public void SimpleCacheEngineTests_AppendToList_WhenCacheEntryKeyExists_ThenReturnsTrue()
        {
            // ----- Arrange -----
            var cacheKey = "add-new-item-to-list/1";
            var listValues = new List<int>
            {
                81,
                466,
                300,
                78
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsList(cacheKey, listValues, TimeSpan.FromSeconds(4.4d));

            // ----- Act -----
            var result = engine.AppendToList(cacheKey, 807045);

            // ----- Assert -----
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_AppendToList_WhenCacheEntryKeyDoesntExist_ThenReturnsFalse()
        {
            // ----- Arrange -----
            var cacheKey = "add-new-item-to-list/30303";
            var listValues = new List<int>
            {
                6070,
                100971,
                130,
                8
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsList(cacheKey, listValues, TimeSpan.FromSeconds(3.4d));

            // ----- Act -----
            var result = engine.AppendToList("add-new-item-to-list/40404", 130);

            // ----- Assert -----
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_RetrieveList_WhenCacheEntryKeyDoesntExist_ThenReturnsNull()
        {
            // ----- Arrange -----
            var cacheKey = "retrieve-list-not-found/0/0";

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);

            // ----- Act -----
            var result = engine.RetrieveList(cacheKey);

            // ----- Assert -----
            Assert.IsNull(result);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_ExistsInSet_WhenCacheEntryKeyDoesntExist_ThenReturnsFalse()
        {
            // ----- Arrange -----
            var cacheKey = "some-non-existent-set/01";

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);

            // ----- Act -----
            var result = engine.ExistsInSet(cacheKey, 4548.222d);

            // ----- Assert -----
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_ExistsInSet_WhenSetDoesntContainValue_ThenReturnsFalse()
        {
            // ----- Arrange -----
            var cacheKey = "set-of-ints/01";
            var setValues = new SortedSet<int>
            {
                5478101,
                466,
                300,
                701018,
                99, 
                92
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsSet(cacheKey, setValues, TimeSpan.FromSeconds(3.1d));

            // ----- Act -----
            var result = engine.ExistsInSet(cacheKey, -100);

            // ----- Assert -----
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_ExistsInSet_WhenSetDoesContainValue_ThenReturnsTrue()
        {
            // ----- Arrange -----
            var cacheKey = "set-of-ints/02";
            var setValues = new SortedSet<int>
            {
                3,
                99,
                130,
                300,
                6070,
                100971,
                701018,
                707008
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsSet(cacheKey, setValues, TimeSpan.FromSeconds(3.1d));

            // ----- Act -----
            var result = engine.ExistsInSet(cacheKey, 130);

            // ----- Assert -----
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_RemoveFromList_WhenDictionaryKeyExists_ThenReturnsTrue()
        {
            // ----- Arrange -----
            var cacheKey = "remove-from-list-of-string";
            var listValues = new List<string>
            {
                "aa a aaa a",
                "bb bb bb b",
                "c cc ccc c",
                "d d d dddd"
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsList(cacheKey, listValues, TimeSpan.FromSeconds(4d));

            // ----- Act -----
            var result = engine.RemoveFromList(cacheKey, "bb bb bb b");

            // ----- Assert -----
            Assert.AreEqual(1L, result);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_RemoveFromList_WhenDictionaryKeyExists_ThenListAfterwardsContainsOneLess()
        {
            // ----- Arrange -----
            var cacheKey = "remove-from-list-of-string/B";
            var listValues = new List<string>
            {
                "bb bb bb b",
                "aa a aaa a",
                "bb bb bb b",
                "bb bb bb b",
                "c cc ccc c",
                "bb bb bb b",
                "d d d dddd"
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsList(cacheKey, listValues, TimeSpan.FromSeconds(5));

            // ----- Act -----
            engine.RemoveFromList(cacheKey, "bb bb bb b");

            var retrieved = engine.RetrieveList<string>(cacheKey);

            // ----- Assert -----
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(6, retrieved.Count);
            Assert.AreEqual("aa a aaa a", retrieved[0]);
            Assert.AreEqual("bb bb bb b", retrieved[1]);
            Assert.AreEqual("bb bb bb b", retrieved[2]);
            Assert.AreEqual("c cc ccc c", retrieved[3]);
            Assert.AreEqual("bb bb bb b", retrieved[4]);
            Assert.AreEqual("d d d dddd", retrieved[5]);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_CacheAsDictionary_WhenDictionaryOfByteArrays_ThenRetrieveDictionaryReturnsByteArrays()
        {
            // ----- Arrange -----
            var cacheKey = "create-new-dictionary/bytes/1";
            var initialValues = new Dictionary<string, byte[]>
            {
                { "alpha", new byte[] { 0, 8, 99, 202, 1, 1 } },
                { "beta", new byte[] { 33, 107, 41, 9, 0, 9 } },
                { "gamma", new byte[] { 18, 14 } },
                { "delta", new byte[] { 6, 16, 26, 36, 46 } },
                { "epsilon", new byte[] { 4 } }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);

            // ----- Act -----
            engine.CacheAsDictionary(cacheKey, initialValues, TimeSpan.FromSeconds(7.2d));

            IDictionary<string, byte[]> retrieved = engine.RetrieveDictionary(cacheKey);

            // ----- Assert -----
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(initialValues.Count, retrieved.Count);
            Assert.AreNotSame(initialValues, retrieved, "the engine cheated and returned the exact same instance.");
            foreach (var key in initialValues.Keys)
            {
                Assert.IsTrue(retrieved.ContainsKey(key), "expected dictionary keys did not match for key \"" + key + "\"");
                Assert.AreEqual(initialValues[key].Length, retrieved[key].Length, "byte array is not the expected length.");

                for (var i = 0; i < initialValues[key].Length; i++)
                {
                    Assert.AreEqual(initialValues[key][i], retrieved[key][i], "byte array values did not match on key \"" + key + "\" at position " + i);
                }
            }
        }

        [TestMethod]
        public void SimpleCacheEngineTests_CacheAsDictionary_WhenDictionaryOfUShortsCreated_ThenRetrieveDictionaryReturnsUShorts()
        {
            // ----- Arrange -----
            var cacheKey = "create-new-dictionary/1";
            var initialValues = new Dictionary<string, ushort>
            {
                { "alpha", 0 },
                { "beta", 32800 },
                { "gamma", 65000 },
                { "delta", 1441 },
                { "epsilon", 0 }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);

            // ----- Act -----
            engine.CacheAsDictionary(cacheKey, initialValues, TimeSpan.FromSeconds(7.2d));

            var retrieved = engine.RetrieveDictionary<ushort>(cacheKey);

            // ----- Assert -----
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(initialValues.Count, retrieved.Count);
            Assert.AreNotSame(initialValues, retrieved, "the engine cheated and returned the exact same instance.");
            foreach (var key in initialValues.Keys)
            {
                Assert.IsTrue(retrieved.ContainsKey(key), "expected dictionary keys did not match for key \"" + key + "\"");
                Assert.AreEqual(initialValues[key], retrieved[key], "expected dictionary values did not match at key \"" + key + "\"");
            }
        }

        [TestMethod]
        public void SimpleCacheEngineTests_CacheAsDictionary_WhenDictionaryOfFloatsCreated_ThenRetrieveDictionaryReturnsFloats()
        {
            // ----- Arrange -----
            var cacheKey = "create-new-dictionary/2";
            var initialValues = new Dictionary<string, float>
            {
                { "001", 13121.14295f },
                { "009", 13121.14295f },
                { "080", 0.000035f }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);

            // ----- Act -----
            engine.CacheAsDictionary(cacheKey, initialValues, TimeSpan.FromSeconds(5.5d));

            var retrieved = engine.RetrieveDictionary<float>(cacheKey);

            // ----- Assert -----
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(initialValues.Count, retrieved.Count);
            Assert.AreNotSame(initialValues, retrieved, "the engine cheated and returned the exact same instance.");
            foreach (var key in initialValues.Keys)
            {
                Assert.IsTrue(retrieved.ContainsKey(key), "expected dictionary keys did not match for key \"" + key + "\"");
                Assert.IsTrue(Math.Abs(initialValues[key] - retrieved[key]) < 0.000001, "expected dictionary values did not match at key \"" + key + "\"");
            }
        }

        [TestMethod]
        public void SimpleCacheEngineTests_CacheAsDictionary_WhenSetOfDateTimesCreated_ThenRetrieveSetReturnsDateTimes()
        {
            // ----- Arrange -----
            var cacheKey = "create-new-dictionary/3";
            var initialValues = new Dictionary<string, DateTime>
            {
                { "calendar entry 1", new DateTime(2009, 11, 3) },
                { "calendar entry 2", new DateTime(2019, 4, 30) },
                { "calendar entry 6", new DateTime(2012, 8, 6, 13, 7, 44, 880) }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);

            // ----- Act -----
            engine.CacheAsDictionary(cacheKey, initialValues, TimeSpan.FromSeconds(8d));

            var retrieved = engine.RetrieveDictionary<DateTime>(cacheKey);

            // ----- Assert -----
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(initialValues.Count, retrieved.Count);
            Assert.AreNotSame(initialValues, retrieved, "the engine cheated and returned the exact same instance.");
            foreach (var key in initialValues.Keys)
            {
                Assert.IsTrue(retrieved.ContainsKey(key), "expected dictionary keys did not match for key \"" + key + "\"");
                Assert.AreEqual(initialValues[key], retrieved[key], "expected dictionary values did not match at key \"" + key + "\"");
            }
        }

        [TestMethod]
        public void SimpleCacheEngineTests_RetrieveDictionaryEntry_WhenDictionaryKeyDoesntExist_ThenReturnsFalse()
        {
            // ----- Arrange -----
            var cacheKey = "retrieve-entry-dictionary/1";
            var initialValues = new Dictionary<string, long>
            {
                { "Uptime", 18022240L },
                { "Sequence_Number", 3310221L },
                { "Previous_Recreate", 452115L },
                { "Items_Deleted", 122011L }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsDictionary(cacheKey, initialValues, TimeSpan.FromSeconds(7d));

            // ----- Act -----
            int retrieved;
            var found = engine.RetrieveDictionaryEntry(cacheKey, "Refresh_Delay", out retrieved);

            // ----- Assert -----
            Assert.IsFalse(found);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_RetrieveDictionaryEntry_WhenDictionaryKeyExists_ThenReturnsTrue()
        {
            // ----- Arrange -----
            var cacheKey = "retrieve-entry-dictionary/2";
            var initialValues = new Dictionary<string, DateTime>
            {
                { "PreviousBusinessDay", new DateTime(2017, 2, 3) },
                { "BatchStartTime", new DateTime(2017, 2, 5, 18, 16, 44) },
                { "NextBusinessDay", new DateTime(2017, 2, 7) }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsDictionary(cacheKey, initialValues, TimeSpan.FromSeconds(6.5d));

            // ----- Act -----
            DateTime retrieved;
            var found = engine.RetrieveDictionaryEntry(cacheKey, "NextBusinessDay", out retrieved);

            // ----- Assert -----
            Assert.IsTrue(found);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_RetrieveDictionaryEntry_WhenDictionaryKeyExists_ThenOutputValueMatches()
        {
            // ----- Arrange -----
            var cacheKey = "retrieve-entry-dictionary/3";
            var initialValues = new Dictionary<string, DateTime>
            {
                { "PreviousBusinessDay", new DateTime(2017, 1, 30) },
                { "BatchStartTime", new DateTime(2017, 1, 30, 18, 8, 20) },
                { "NextBusinessDay", new DateTime(2017, 1, 31) }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsDictionary(cacheKey, initialValues, TimeSpan.FromSeconds(6.5d));

            // ----- Act -----
            DateTime retrieved;
            var found = engine.RetrieveDictionaryEntry(cacheKey, "BatchStartTime", out retrieved);

            // ----- Assert -----
            Assert.AreEqual(new DateTime(2017, 1, 30, 18, 8, 20), retrieved);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_RetrieveDictionaryEntry_WhenDictionaryKeyExists_AndByteArrayDictionary_ThenOutputValueMatches()
        {
            // ----- Arrange -----
            var cacheKey = "retrieve-entry-dictionary/4";
            var initialValues = new Dictionary<string, byte[]>
            {
                { "ZZ9 Plural Z Alpha", new byte[] { 84, 8, 84, 8, 10 } },
                { "Gamma Quadrant", new byte[] { 91, 177, 202, 220, 211 } }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsDictionary(cacheKey, initialValues, TimeSpan.FromSeconds(5.1d));

            // ----- Act -----
            byte[] retrieved;
            var found = engine.RetrieveDictionaryEntry(cacheKey, "Gamma Quadrant", out retrieved);

            // ----- Assert -----
            Assert.IsTrue(found);
            for (var i = 0; i < 5; i++)
            {
                Assert.AreEqual(initialValues["Gamma Quadrant"][i], retrieved[i], "byte array mismatch at " + i);
            }
        }

        [TestMethod]
        public void SimpleCacheEngineTests_AddToDictionary_WhenDictionaryDoesntContainKey_ThenReturnsTrue()
        {
            // ----- Arrange -----
            var cacheKey = "add-new-key-to-dictionary/1";
            var initialValues = new Dictionary<string, int>
            {
                { "K081", 81 },
                { "P466", 466 },
                { "E300", 300 },
                { "T078", 78 }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsDictionary(cacheKey, initialValues, TimeSpan.FromSeconds(7.4d));

            // ----- Act -----
            var result = engine.AddToDictionary(cacheKey, "P099", 99);

            // ----- Assert -----
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_AddToDictionary_WhenDictionaryContainsKey_ThenReturnsFalse()
        {
            // ----- Arrange -----
            var cacheKey = "add-new-key-to-dictionary/1";
            var initialValues = new Dictionary<string, int>
            {
                { "K081", 81 },
                { "P466", 466 },
                { "E300", 300 },
                { "T078", 78 }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsDictionary(cacheKey, initialValues, TimeSpan.FromSeconds(4.1d));

            // ----- Act -----
            var result = engine.AddToDictionary(cacheKey, "P466", 90199);

            // ----- Assert -----
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_AddToDictionary_WhenDictionaryDoesntContainKey_ThenKeyIsAddedToDictionary()
        {
            // ----- Arrange -----
            var cacheKey = "add-new-key-to-dictionary/2";
            var initialValues = new Dictionary<string, int>
            {
                { "K081", 81 },
                { "P466", 466 },
                { "E300", 300 },
                { "T078", 78 }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsDictionary(cacheKey, initialValues, TimeSpan.FromSeconds(4.1d));

            // ----- Act -----
            var result = engine.AddToDictionary(cacheKey, "J276", 90276);

            var newDictionary = engine.RetrieveDictionary<int>(cacheKey);

            // ----- Assert -----
            Assert.IsNotNull(newDictionary);
            Assert.AreEqual(5, newDictionary.Count);
            Assert.IsTrue(newDictionary.ContainsKey("J276"));
            Assert.AreEqual(90276, newDictionary["J276"]);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_UpdateDictionaryEntry_WhenDictionaryDoesntContainKey_ThenReturnsFalse()
        {
            // ----- Arrange -----
            var cacheKey = "update-key-in-dictionary/1";
            var initialValues = new Dictionary<string, int>
            {
                { "K081", 81 },
                { "P466", 466 },
                { "E300", 300 },
                { "T078", 78 }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsDictionary(cacheKey, initialValues, TimeSpan.FromSeconds(4.1d));

            // ----- Act -----
            var result = engine.UpdateDictionaryEntry(cacheKey, "J276", 90276);

            // ----- Assert -----
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_UpdateDictionaryEntry_WhenDictionaryContainsKey_ThenReturnsTrue()
        {
            // ----- Arrange -----
            var cacheKey = "update-key-in-dictionary/2";
            var initialValues = new Dictionary<string, int>
            {
                { "K081", 81 },
                { "P466", 466 },
                { "E300", 300 },
                { "T078", 78 }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsDictionary(cacheKey, initialValues, TimeSpan.FromSeconds(4.1d));

            // ----- Act -----
            var result = engine.UpdateDictionaryEntry(cacheKey, "E300", 90276);

            // ----- Assert -----
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_UpdateDictionaryEntry_WhenDictionaryContainsKey_ThenNewDictionaryKeyValueStored()
        {
            // ----- Arrange -----
            var cacheKey = "update-key-in-dictionary/3";
            var initialValues = new Dictionary<string, int>
            {
                { "K081", 81 },
                { "P466", 466 },
                { "E300", 300 },
                { "T078", 78 }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsDictionary(cacheKey, initialValues, TimeSpan.FromSeconds(4.1d));

            // ----- Act -----
            engine.UpdateDictionaryEntry(cacheKey, "E300", 90276);

            int newValue;
            var fetched = engine.RetrieveDictionaryEntry(cacheKey, "E300", out newValue);

            // ----- Assert -----
            Assert.AreEqual(90276, newValue);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_RetrieveDictionary_WhenCacheEntryKeyExists_ThenReturnsEntireDictionaryByteArray()
        {
            // ----- Arrange -----
            var cacheKey = "retrieve-dictionary/1";
            var initialValues = new Dictionary<string, byte[]>
            {
                { "farm", new byte[] { 9, 134, 25, 70, 70, 161, 30, 97 } },
                { "office", new byte[] { 52, 200, 13, 7, 133, 133, 133 } },
                { "dock", new byte[] { 81, 81, 101, 90, 9, 22, 1, 204, 2, 18, 63 } },
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsDictionary(cacheKey, initialValues, TimeSpan.FromSeconds(7d));

            // ----- Act -----
            IDictionary<string, byte[]> retrieved = engine.RetrieveDictionary(cacheKey);

            // ----- Assert -----
            Assert.AreEqual(3, retrieved.Count);
            Assert.AreNotSame(initialValues, retrieved, "the engine cheated and returned the exact same instance.");
            foreach (var key in initialValues.Keys)
            {
                Assert.IsTrue(retrieved.ContainsKey(key), "expected dictionary keys did not match for key \"" + key + "\"");
                Assert.AreEqual(initialValues[key].Length, retrieved[key].Length, "expected dictionary values did not match at key \"" + key + "\"");
                for (var i = 0; i < initialValues[key].Length; i++)
                {
                    Assert.AreEqual(initialValues[key][i], retrieved[key][i], "expected dictionary values did not match at key \"" + key + "\"");
                }
            }
        }

        [TestMethod]
        public void SimpleCacheEngineTests_RetrieveDictionary_WhenCacheEntryKeyDoesntExist_ThenReturnsNull()
        {
            // ----- Arrange -----
            var cacheKey = "retrieve-dictionary-not-found/8/9/10";

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);

            // ----- Act -----
            var result = engine.RetrieveDictionary<int>(cacheKey);

            // ----- Assert -----
            Assert.IsNull(result);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_RemoveFromDictionary_WhenCacheEntryKeyDoesntExist_ThenReturnsFalse()
        {
            // ----- Arrange -----
            var cacheKey = "remove-from-dictionary-doesnt-exist/40/404";

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);

            // ----- Act -----
            var result = engine.RemoveFromDictionary(cacheKey, "any old key");

            // ----- Assert -----
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_RemoveFromDictionary_WhenDictionaryKeyDoesntExist_ThenReturnsFalse()
        {
            // ----- Arrange -----
            var cacheKey = "remove-from-dictionary/1.0";
            var dictionaryItems = new Dictionary<string, int>
            {
                { "001", 1 },
                { "002", 2 },
                { "003", 3 },
                { "004", 4 }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsDictionary(cacheKey, dictionaryItems, TimeSpan.FromSeconds(4.1));

            // ----- Act -----
            var result = engine.RemoveFromDictionary(cacheKey, "any old key");

            // ----- Assert -----
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_RemoveFromDictionary_WhenDictionaryKeyExists_ThenReturnsTrue()
        {
            // ----- Arrange -----
            var cacheKey = "remove-from-dictionary/2.0";
            var dictionaryItems = new Dictionary<string, int>
            {
                { "001", 1 },
                { "002", 2 },
                { "003", 3 },
                { "004", 4 }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsDictionary(cacheKey, dictionaryItems, TimeSpan.FromSeconds(4.5));

            // ----- Act -----
            var result = engine.RemoveFromDictionary(cacheKey, "002");

            // ----- Assert -----
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_CacheAsQueue_WhenQueueCreatedWith5Items_ThenCanPop5Items()
        {
            // ----- Arrange -----
            var cacheKey = "cache-as-queue/1";
            var queuedItems = new int[] { 56, 899, 1040, 3030, 81 };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);

            // ----- Act -----
            engine.CacheAsQueue(cacheKey, queuedItems, TimeSpan.FromSeconds(3.9));

            // ----- Assert -----
            int outputValue;
            Assert.IsTrue(engine.PopQueue(cacheKey, out outputValue));
            Assert.AreEqual(56, outputValue);
            Assert.IsTrue(engine.PopQueue(cacheKey, out outputValue));
            Assert.AreEqual(899, outputValue);
            Assert.IsTrue(engine.PopQueue(cacheKey, out outputValue));
            Assert.AreEqual(1040, outputValue);
            Assert.IsTrue(engine.PopQueue(cacheKey, out outputValue));
            Assert.AreEqual(3030, outputValue);
            Assert.IsTrue(engine.PopQueue(cacheKey, out outputValue));
            Assert.AreEqual(81, outputValue);
            Assert.IsFalse(engine.PopQueue(cacheKey, out outputValue));
        }

        [TestMethod]
        public void SimpleCacheEngineTests_PushQueue_WhenPushedIntoEmptyQueue_ThenCanPop1Items()
        {
            // ----- Arrange -----
            var cacheKey = "push-queue/2";
            var queuedItems = new int[0];

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsQueue(cacheKey, queuedItems, TimeSpan.FromSeconds(4.9));

            // ----- Act -----
            engine.PushQueue(cacheKey, 4987);

            // ----- Assert -----
            int outputValue;
            Assert.IsTrue(engine.PopQueue(cacheKey, out outputValue));
            Assert.AreEqual(4987, outputValue);
            Assert.IsFalse(engine.PopQueue(cacheKey, out outputValue));
        }

        [TestMethod]
        public void SimpleCacheEngineTests_PopQueue_WhenPoppedFromEmptyQueue_ThenReturnsFalse()
        {
            // ----- Arrange -----
            var cacheKey = "pop-queue/1";
            var queuedItems = new int[0];

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsQueue(cacheKey, queuedItems, TimeSpan.FromSeconds(4.9));

            // ----- Act -----

            // ----- Assert -----
            int outputValue;
            Assert.IsFalse(engine.PopQueue(cacheKey, out outputValue));
        }

        [TestMethod]
        public void SimpleCacheEngineTests_CacheAsQueue_ByteArray_WhenQueueCreatedWith4Items_ThenCanPop4Items()
        {
            // ----- Arrange -----
            var cacheKey = "cache-as-queue-byte-array/1";
            var queuedItems = new byte[][]
            {
                new byte[] { 56, 99, 40, 230 },
                new byte[] { 9, 99, 19, 89, 29 },
                new byte[] { 1, 20, 30 },
                new byte[] { 44, 210, 210, 210, 210, 10 }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);

            // ----- Act -----
            engine.CacheAsQueue(cacheKey, queuedItems, TimeSpan.FromSeconds(3.9));

            // ----- Assert -----
            byte[] outputValue;
            Assert.IsTrue(engine.PopQueue(cacheKey, out outputValue));
            Assert.IsTrue(ByteArraysEqual(new byte[] { 56, 99, 40, 230 }, outputValue));
            Assert.IsTrue(engine.PopQueue(cacheKey, out outputValue));
            Assert.IsTrue(ByteArraysEqual(new byte[] { 9, 99, 19, 89, 29 }, outputValue));
            Assert.IsTrue(engine.PopQueue(cacheKey, out outputValue));
            Assert.IsTrue(ByteArraysEqual(new byte[] { 1, 20, 30 }, outputValue));
            Assert.IsTrue(engine.PopQueue(cacheKey, out outputValue));
            Assert.IsTrue(ByteArraysEqual(new byte[] { 44, 210, 210, 210, 210, 10 }, outputValue));
            Assert.IsFalse(engine.PopQueue(cacheKey, out outputValue));
        }

        [TestMethod]
        public void SimpleCacheEngineTests_PushQueue_ByteArray_WhenPushedIntoEmptyQueue_ThenCanPop1Items()
        {
            // ----- Arrange -----
            var cacheKey = "push-queue-byte-array/2";
            var queuedItems = new byte[0][];

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsQueue(cacheKey, queuedItems, TimeSpan.FromSeconds(4.9));

            // ----- Act -----
            engine.PushQueue(cacheKey, new byte[] { 190, 5, 162, 8, 1, 1, 0, 6 });

            // ----- Assert -----
            byte[] outputValue;
            Assert.IsTrue(engine.PopQueue(cacheKey, out outputValue));
            Assert.IsTrue(ByteArraysEqual(new byte[] { 190, 5, 162, 8, 1, 1, 0, 6 }, outputValue));
            Assert.IsFalse(engine.PopQueue(cacheKey, out outputValue));
        }

        [TestMethod]
        public void SimpleCacheEngineTests_PopQueue_ByteArray_WhenPoppedFromEmptyQueue_ThenReturnsFalse()
        {
            // ----- Arrange -----
            var cacheKey = "pop-queue-byte-array/1";
            var queuedItems = new byte[0][];

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsQueue(cacheKey, queuedItems, TimeSpan.FromSeconds(4.9));

            // ----- Act -----

            // ----- Assert -----
            byte[] outputValue;
            Assert.IsFalse(engine.PopQueue(cacheKey, out outputValue));
        }

        [TestMethod]
        public void SimpleCacheEngineTests_CacheAsDictionary_ByteArray_WhenDictionaryCreated_ThenRetrieveDictionaryReturnsByteArrays()
        {
            // ----- Arrange -----
            var cacheKey = "create-new-dictionary-byte-array/2";
            var initialValues = new Dictionary<string, byte[]>
            {
                { "001", new byte[] { 131, 21, 142, 9, 5 } },
                { "009", new byte[] { 1, 31, 211, 4, 29, 5 } },
                { "080", new byte[] { 0, 0, 0, 35 } }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);

            // ----- Act -----
            engine.CacheAsDictionary(cacheKey, initialValues, TimeSpan.FromSeconds(4.5d));

            var retrieved = engine.RetrieveDictionary(cacheKey);

            // ----- Assert -----
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(initialValues.Count, retrieved.Count);
            Assert.AreNotSame(initialValues, retrieved, "the engine cheated and returned the exact same instance.");
            foreach (var key in initialValues.Keys)
            {
                Assert.IsTrue(retrieved.ContainsKey(key), "expected dictionary keys did not match for key \"" + key + "\"");
                Assert.IsTrue(ByteArraysEqual(initialValues[key], retrieved[key]), "expected dictionary values did not match at key \"" + key + "\"");
            }
        }

        [TestMethod]
        public void SimpleCacheEngineTests_CacheAsDictionary_ByteArray_WhenSetCreated_ThenRetrieveSetReturnsByteArrays()
        {
            // ----- Arrange -----
            var cacheKey = "create-new-dictionary-byte-array/3";
            var initialValues = new Dictionary<string, byte[]>
            {
                { "1", new byte[] { 2, 0, 0, 9, 11, 3 } },
                { "2", new byte[] { 201, 94, 30 } },
                { "6", new byte[] { 201, 28, 61, 37, 44, 88 } }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);

            // ----- Act -----
            engine.CacheAsDictionary(cacheKey, initialValues, TimeSpan.FromSeconds(8d));

            var retrieved = engine.RetrieveDictionary(cacheKey);

            // ----- Assert -----
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(initialValues.Count, retrieved.Count);
            Assert.AreNotSame(initialValues, retrieved, "the engine cheated and returned the exact same instance.");
            foreach (var key in initialValues.Keys)
            {
                Assert.IsTrue(retrieved.ContainsKey(key), "expected dictionary keys did not match for key \"" + key + "\"");
                Assert.IsTrue(ByteArraysEqual(initialValues[key], retrieved[key]), "expected dictionary values did not match at key \"" + key + "\"");
            }
        }

        [TestMethod]
        public void SimpleCacheEngineTests_RetrieveDictionaryEntry_ByteArray_WhenDictionaryKeyDoesntExist_ThenReturnsFalse()
        {
            // ----- Arrange -----
            var cacheKey = "retrieve-entry-dictionary-byte-array/1";
            var initialValues = new Dictionary<string, byte[]>
            {
                { "U01", new byte[] { 180, 22, 2, 40 } },
                { "S02", new byte[] { 3, 31, 0, 2, 2, 1 } },
                { "P00", new byte[] { 45, 211, 5 } },
                { "I98", new byte[] { 122, 11 } }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsDictionary(cacheKey, initialValues, TimeSpan.FromSeconds(7d));

            // ----- Act -----
            byte[] retrieved;
            var found = engine.RetrieveDictionaryEntry(cacheKey, "M08", out retrieved);

            // ----- Assert -----
            Assert.IsFalse(found);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_RetrieveDictionaryEntry_ByteArray_WhenDictionaryKeyExists_ThenReturnsTrue()
        {
            // ----- Arrange -----
            var cacheKey = "retrieve-entry-dictionary-byte-array/2";
            var initialValues = new Dictionary<string, byte[]>
            {
                { "U01", new byte[] { 180, 22, 2, 40 } },
                { "S02", new byte[] { 3, 31, 0, 2, 2, 1 } },
                { "P00", new byte[] { 45, 211, 5 } },
                { "I98", new byte[] { 122, 11 } }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsDictionary(cacheKey, initialValues, TimeSpan.FromSeconds(6.5d));

            // ----- Act -----
            byte[] retrieved;
            var found = engine.RetrieveDictionaryEntry(cacheKey, "S02", out retrieved);

            // ----- Assert -----
            Assert.IsTrue(found);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_RetrieveDictionaryEntry_ByteArray_WhenDictionaryKeyExists_ThenOutputValueMatches()
        {
            // ----- Arrange -----
            var cacheKey = "retrieve-entry-dictionary/3";
            var initialValues = new Dictionary<string, byte[]>
            {
                { "U01", new byte[] { 180, 22, 2, 40 } },
                { "S02", new byte[] { 3, 31, 0, 2, 2, 1 } },
                { "P00", new byte[] { 45, 211, 5 } },
                { "I98", new byte[] { 122, 11 } }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsDictionary(cacheKey, initialValues, TimeSpan.FromSeconds(6.5d));

            // ----- Act -----
            byte[] retrieved;
            var found = engine.RetrieveDictionaryEntry(cacheKey, "S02", out retrieved);

            // ----- Assert -----
            Assert.IsTrue(ByteArraysEqual(new byte[] { 3, 31, 0, 2, 2, 1 }, retrieved));
        }

        [TestMethod]
        public void SimpleCacheEngineTests_AddToDictionary_ByteArray_WhenDictionaryDoesntContainKey_ThenReturnsTrue()
        {
            // ----- Arrange -----
            var cacheKey = "add-new-key-to-dictionary-byte-array/1";
            var initialValues = new Dictionary<string, byte[]>
            {
                { "K081", new byte[] { 81, 100 } },
                { "P466", new byte[] { 4, 66, 0, 0, 0, 0, 2 } },
                { "E300", new byte[] { 200 } },
                { "T078", new byte[] { 78 } }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsDictionary(cacheKey, initialValues, TimeSpan.FromSeconds(7.4d));

            // ----- Act -----
            var result = engine.AddToDictionary(cacheKey, "P099", new byte[] { 99 });

            // ----- Assert -----
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_AddToDictionary_ByteArray_WhenDictionaryContainsKey_ThenReturnsFalse()
        {
            // ----- Arrange -----
            var cacheKey = "add-new-key-to-dictionary-byte-array/1";
            var initialValues = new Dictionary<string, byte[]>
            {
                { "K081", new byte[] { 81, 100 } },
                { "P466", new byte[] { 4, 66, 0, 0, 0, 0, 2 } },
                { "E300", new byte[] { 200 } },
                { "T078", new byte[] { 78 } }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsDictionary(cacheKey, initialValues, TimeSpan.FromSeconds(4.1d));

            // ----- Act -----
            var result = engine.AddToDictionary(cacheKey, "P466", new byte[] { 90, 19, 9 });

            // ----- Assert -----
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_AddToDictionary_ByteArray_WhenDictionaryDoesntContainKey_ThenKeyIsAddedToDictionary()
        {
            // ----- Arrange -----
            var cacheKey = "add-new-key-to-dictionary-byte-array/2";
            var initialValues = new Dictionary<string, byte[]>
            {
                { "K081", new byte[] { 81, 100 } },
                { "P466", new byte[] { 4, 66, 0, 0, 0, 0, 2 } },
                { "E300", new byte[] { 200 } },
                { "T078", new byte[] { 78 } }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsDictionary(cacheKey, initialValues, TimeSpan.FromSeconds(4.1d));

            // ----- Act -----
            var result = engine.AddToDictionary(cacheKey, "J276", new byte[] { 90, 2, 76 });

            var newDictionary = engine.RetrieveDictionary(cacheKey);

            // ----- Assert -----
            Assert.IsNotNull(newDictionary);
            Assert.AreEqual(5, newDictionary.Count);
            Assert.IsTrue(newDictionary.ContainsKey("J276"));
            Assert.IsTrue(ByteArraysEqual(new byte[] { 90, 2, 76 }, newDictionary["J276"]));
        }

        [TestMethod]
        public void SimpleCacheEngineTests_UpdateDictionaryEntry_ByteArray_WhenDictionaryDoesntContainKey_ThenReturnsFalse()
        {
            // ----- Arrange -----
            var cacheKey = "update-key-in-dictionary-byte-array/1";
            var initialValues = new Dictionary<string, byte[]>
            {
                { "K081", new byte[] { 81, 100 } },
                { "P466", new byte[] { 4, 66, 0, 0, 0, 0, 2 } },
                { "E300", new byte[] { 200 } },
                { "T078", new byte[] { 78 } }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsDictionary(cacheKey, initialValues, TimeSpan.FromSeconds(4.1d));

            // ----- Act -----
            var result = engine.UpdateDictionaryEntry(cacheKey, "J276", new byte[] { 90, 2, 76 });

            // ----- Assert -----
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_UpdateDictionaryEntry_ByteArray_WhenDictionaryContainsKey_ThenReturnsTrue()
        {
            // ----- Arrange -----
            var cacheKey = "update-key-in-dictionary-byte-array/2";
            var initialValues = new Dictionary<string, byte[]>
            {
                { "K081", new byte[] { 81, 100 } },
                { "P466", new byte[] { 4, 66, 0, 0, 0, 0, 2 } },
                { "E300", new byte[] { 200 } },
                { "T078", new byte[] { 78 } }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsDictionary(cacheKey, initialValues, TimeSpan.FromSeconds(4.1d));

            // ----- Act -----
            var result = engine.UpdateDictionaryEntry(cacheKey, "E300", new byte[] { 90, 2, 76 });

            // ----- Assert -----
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_UpdateDictionaryEntry_ByteArray_WhenDictionaryContainsKey_ThenNewDictionaryKeyValueStored()
        {
            // ----- Arrange -----
            var cacheKey = "update-key-in-dictionary-byte-array/3";
            var initialValues = new Dictionary<string, byte[]>
            {
                { "K081", new byte[] { 81, 100 } },
                { "P466", new byte[] { 4, 66, 0, 0, 0, 0, 2 } },
                { "E300", new byte[] { 200 } },
                { "T078", new byte[] { 78 } }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsDictionary(cacheKey, initialValues, TimeSpan.FromSeconds(4.1d));

            // ----- Act -----
            engine.UpdateDictionaryEntry(cacheKey, "E300", new byte[] { 90, 2, 76 });

            byte[] newValue;
            var fetched = engine.RetrieveDictionaryEntry(cacheKey, "E300", out newValue);

            // ----- Assert -----
            Assert.IsTrue(ByteArraysEqual(new byte[] { 90, 2, 76 }, newValue));
        }

        [TestMethod]
        public void SimpleCacheEngineTests_RetrieveDictionary_ByteArray_WhenCacheEntryKeyDoesntExist_ThenReturnsNull()
        {
            // ----- Arrange -----
            var cacheKey = "retrieve-dictionary-not-found-byte-array/8/9/10";

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);

            // ----- Act -----
            IDictionary<string, byte[]> result = engine.RetrieveDictionary(cacheKey);

            // ----- Assert -----
            Assert.IsNull(result);
        }
        
        [TestMethod]
        public void SimpleCacheEngineTests_RemoveFromDictionary_ByteArray_WhenDictionaryKeyDoesntExist_ThenReturnsFalse()
        {
            // ----- Arrange -----
            var cacheKey = "remove-from-dictionary-byte-array/1.0";
            var dictionaryItems = new Dictionary<string, byte[]>
            {
                { "001", new byte[] { 1, 0, 0, 1 } },
                { "002", new byte[] { 2 } },
                { "003", new byte[] { 3 } },
                { "004", new byte[] { 7, 4 } }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsDictionary(cacheKey, dictionaryItems, TimeSpan.FromSeconds(4.1));

            // ----- Act -----
            var result = engine.RemoveFromDictionary(cacheKey, "any old key");

            // ----- Assert -----
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_RemoveFromDictionary_ByteArray_WhenDictionaryKeyExists_ThenReturnsTrue()
        {
            // ----- Arrange -----
            var cacheKey = "remove-from-dictionary-byte-array/2.0";
            var dictionaryItems = new Dictionary<string, byte[]>
            {
                { "001", new byte[] { 1, 0, 0, 1 } },
                { "002", new byte[] { 2 } },
                { "003", new byte[] { 3 } },
                { "004", new byte[] { 7, 4 } }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsDictionary(cacheKey, dictionaryItems, TimeSpan.FromSeconds(4.5));

            // ----- Act -----
            var result = engine.RemoveFromDictionary(cacheKey, "002");

            // ----- Assert -----
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void SimpleCacheEngineTest_CacheAsSet_ByteArray_WhenCacheAsSet_ThenRetrieveSetReturnsByteArrays()
        {
            // ----- Arrange -----
            var cacheKey = "create-new-set-byte-array/1";
            var listValues = new List<byte[]>
            {
                new byte[] { 3, 44 },
                new byte[] { 81, 2, 9 },
                new byte[] { 101, 0, 22, 0, 0, 0, 3 },
                new byte[] { 0, 0, 0, 46 }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);

            // ----- Act -----
            engine.CacheAsSet(cacheKey, listValues, TimeSpan.FromSeconds(6.3333d));

            var retrieved = engine.RetrieveSet(cacheKey);

            // ----- Assert -----
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(listValues.Count, retrieved.Count);
            Assert.AreNotSame(listValues, retrieved, "the engine cheated and returned the exact same instance.");
            for (var i = 0; i < listValues.Count; i++)
            {
                Assert.IsTrue(retrieved.Any(r => ByteArraysEqual(r, listValues[i])), "expected set values did not match at " + i);
            }
        }

        [TestMethod]
        public void SimpleCacheEngineTest_AddToSet_ByteArray_WhenSetDoesntContainItem_ThenReturnsTrue()
        {
            // ----- Arrange -----
            var cacheKey = "add-new-item-to-set-byte-array/1";
            var listValues = new List<byte[]>
            {
                new byte[] { 3, 44 },
                new byte[] { 81, 2, 9 },
                new byte[] { 101, 0, 22, 0, 0, 0, 3 },
                new byte[] { 0, 0, 0, 46 }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsSet(cacheKey, listValues, TimeSpan.FromSeconds(7.4d));

            // ----- Act -----
            var result = engine.AddToSet(cacheKey, new byte[] { 80, 7, 0, 45 });

            // ----- Assert -----
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void SimpleCacheEngineTest_AddToSet_ByteArray_WhenSetContainsItem_ThenReturnsFalse()
        {
            // ----- Arrange -----
            var cacheKey = "add-new-item-to-set-byte-array/3";
            var listValues = new List<byte[]>
            {
                new byte[] { 3, 44 },
                new byte[] { 81, 2, 9 },
                new byte[] { 101, 0, 22, 0, 0, 0, 3 },
                new byte[] { 0, 0, 0, 46 }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsSet(cacheKey, listValues, TimeSpan.FromSeconds(7.4d));

            // ----- Act -----
            var result = engine.AddToSet(cacheKey, new byte[] { 81, 2, 9 });

            // ----- Assert -----
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SimpleCacheEngineTest_AddToSet_ByteArray_WhenSetDoesntContainItem_ThenItemIsAddedToSet()
        {
            // ----- Arrange -----
            var cacheKey = "add-new-item-to-set-byte-array/2";
            var listValues = new List<byte[]>
            {
                new byte[] { 3, 44 },
                new byte[] { 81, 2, 9 },
                new byte[] { 101, 0, 22, 0, 0, 0, 3 },
                new byte[] { 0, 0, 0, 46 }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsSet(cacheKey, listValues, TimeSpan.FromSeconds(7.4d));

            // ----- Act -----
            engine.AddToSet(cacheKey, new byte[] { 8, 0, 70, 145 });

            var newList = engine.RetrieveSet(cacheKey);

            // ----- Assert -----
            Assert.IsNotNull(newList);
            Assert.AreEqual(5, newList.Count);
            Assert.IsTrue(newList.Any(l => ByteArraysEqual(l, new byte[] { 8, 0, 70, 145 })));
        }

        [TestMethod]
        public void SimpleCacheEngineTest_RetrieveSet_ByteArray_WhenCacheEntryKeyDoesntExist_ThenReturnsNull()
        {
            // ----- Arrange -----
            var cacheKey = "retrieve-set-not-found-byte-array/0/0";

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);

            // ----- Act -----
            var result = engine.RetrieveSet(cacheKey);

            // ----- Assert -----
            Assert.IsNull(result);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_CacheAsList_ByteArray_WhenCacheAsList_ThenRetrieveListReturnsByteArrays()
        {
            // ----- Arrange -----
            var cacheKey = "create-new-list-byte-array/1";
            var listValues = new List<byte[]>
            {
                new byte[] { 3, 44 },
                new byte[] { 81, 29, 101, 0, 22 },
                new byte[] { 0, 3, 0, 0, 0, 0, 0, 46 }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);

            // ----- Act -----
            engine.CacheAsList(cacheKey, listValues, TimeSpan.FromSeconds(6.3333d));

            var retrieved = engine.RetrieveList(cacheKey);

            // ----- Assert -----
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(listValues.Count, retrieved.Count);
            Assert.AreNotSame(listValues, retrieved, "the engine cheated and returned the exact same instance.");
            for (var i = 0; i < listValues.Count; i++)
            {
                // this test ensures ordering is maintained
                Assert.IsTrue(ByteArraysEqual(listValues[i], retrieved[i]), "expected list values did not match at " + i);
            }
        }

        [TestMethod]
        public void SimpleCacheEngineTests_CacheAsList_ByteArray_WhenDuplicateEntriesListed_ThenRetrieveListReturnsDuplicateEntries()
        {
            // ----- Arrange -----
            var cacheKey = "create-new-list-of-duplicated-byte-arrays";
            var listValues = new List<byte[]>
            {
                new byte[] { 2, 0, 10, 9, 17 },
                new byte[] { 20, 19, 12, 28 },
                new byte[] { 201, 6, 2, 2, 0, 0, 9 },
                new byte[] { 2, 0, 10, 9, 17 },
                new byte[] { 2, 0, 10, 9, 17 },
                new byte[] { 200, 44, 14 }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);

            // ----- Act -----
            engine.CacheAsList(cacheKey, listValues, TimeSpan.FromSeconds(6d));

            var retrieved = engine.RetrieveList(cacheKey);

            // ----- Assert -----
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(listValues.Count, retrieved.Count);
            Assert.AreNotSame(listValues, retrieved, "the engine cheated and returned the exact same instance.");
            for (var i = 0; i < listValues.Count; i++)
            {
                // this test ensures ordering is maintained
                Assert.IsTrue(ByteArraysEqual(listValues[i], retrieved[i]), "expected list values did not match at " + i);
            }
        }

        [TestMethod]
        public void SimpleCacheEngineTests_AppendToList_ByteArray_WhenCacheEntryKeyExists_ThenReturnsTrue()
        {
            // ----- Arrange -----
            var cacheKey = "add-new-item-to-list-byte-arrays/1";
            var listValues = new List<byte[]>
            {
                new byte[] { 2, 0, 10, 9, 17 },
                new byte[] { 20, 19, 12, 28 },
                new byte[] { 201, 6, 2, 2, 0, 0, 9 },
                new byte[] { 200, 44, 14 }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsList(cacheKey, listValues, TimeSpan.FromSeconds(4.4d));

            // ----- Act -----
            var result = engine.AppendToList(cacheKey, new byte[] { 8, 0, 70, 45 });

            // ----- Assert -----
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_AppendToList_ByteArray_WhenCacheEntryKeyDoesntExist_ThenReturnsFalse()
        {
            // ----- Arrange -----
            var cacheKey = "add-new-item-to-list-byte-array/30303";
            var listValues = new List<byte[]>
            {
                new byte[] { 2, 0, 10, 9, 17 },
                new byte[] { 20, 19, 12, 28 },
                new byte[] { 201, 6, 2, 2, 0, 0, 9 },
                new byte[] { 200, 44, 14 }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsList(cacheKey, listValues, TimeSpan.FromSeconds(3.4d));

            // ----- Act -----
            var result = engine.AppendToList("add-new-item-to-list-byte-array/40404", new byte[] { 130 });

            // ----- Assert -----
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_RetrieveList_ByteArray_WhenCacheEntryKeyDoesntExist_ThenReturnsNull()
        {
            // ----- Arrange -----
            var cacheKey = "retrieve-list-not-found-byte-array/0/0";

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);

            // ----- Act -----
            var result = engine.RetrieveList(cacheKey);

            // ----- Assert -----
            Assert.IsNull(result);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_ExistsInSet_ByteArray_WhenCacheEntryDoesntExist_ThenReturnsFalse()
        {
            // ----- Arrange -----
            var engine = new SimpleCacheEngine();
            engine.Initialize(null);

            // ----- Act -----
            var result = engine.ExistsInSet("a-set-that-doesn't-exist", new byte[] { 45, 48, 2, 22 });

            // ----- Assert -----
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_ExistsInSet_ByteArray_WhenSetDoesntContainValue_ThenReturnsFalse()
        {
            // ----- Arrange -----
            var cacheKey = "exists-in-set-of-byte-array/01";
            var setValues = new HashSet<byte[]>
            {
                new byte[] { 2, 0, 10, 9, 17 },
                new byte[] { 20, 19, 12, 28 },
                new byte[] { 201, 6, 2, 2, 0, 0, 9 },
                new byte[] { 200, 44, 14 }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsSet(cacheKey, setValues, TimeSpan.FromSeconds(4d));

            // ----- Act -----
            var result = engine.ExistsInSet(cacheKey, new byte[] { 45, 48, 2, 22 });

            // ----- Assert -----
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_ExistsInSet_ByteArray_WhenSetDoesContainValue_ThenReturnsTrue()
        {
            // ----- Arrange -----
            var cacheKey = "exists-in-set-of-byte-array/02";
            var setValues = new HashSet<byte[]>
            {
                new byte[] { 2, 0, 10, 9, 17 },
                new byte[] { 20, 19, 12, 28 },
                new byte[] { 201, 6, 2, 2, 0, 0, 9 },
                new byte[] { 200, 44, 14 }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsSet(cacheKey, setValues, TimeSpan.FromSeconds(3.2d));

            // ----- Act -----
            var result = engine.ExistsInSet(cacheKey, new byte[] { 201, 6, 2, 2, 0, 0, 9 });

            // ----- Assert -----
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_RemoveFromList_ByteArray_WhenDictionaryKeyExists_ThenReturnsTrue()
        {
            // ----- Arrange -----
            var cacheKey = "remove-from-list-of-byte-arrays";
            var listValues = new List<byte[]>
            {
                new byte[] { 2, 0, 10, 9, 17 },
                new byte[] { 20, 19, 12, 28 },
                new byte[] { 201, 6, 2, 2, 0, 0, 9 },
                new byte[] { 200, 44, 14 }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsList(cacheKey, listValues, TimeSpan.FromSeconds(4d));

            // ----- Act -----
            var result = engine.RemoveFromList(cacheKey, new byte[] { 20, 19, 12, 28 });

            // ----- Assert -----
            Assert.AreEqual(1L, result);
        }

        [TestMethod]
        public void SimpleCacheEngineTests_RemoveFromList_ByteArray_WhenDictionaryKeyExists_ThenListAfterwardsContainsOneLess()
        {
            // ----- Arrange -----
            var cacheKey = "remove-from-list-of-byte-arrays/B/0";
            var listValues = new List<byte[]>
            {
                new byte[] { 2, 0, 10, 9, 17 },
                new byte[] { 20, 19, 12, 28 },
                new byte[] { 201, 6, 2, 2, 0, 0, 9 },
                new byte[] { 200, 44, 14 },
                new byte[] { 20, 19, 12, 28 },
                new byte[] { 20, 19, 12, 28 },
                new byte[] { 200, 44, 14 }
            };

            var engine = new SimpleCacheEngine();
            engine.Initialize(null);
            engine.CacheAsList(cacheKey, listValues, TimeSpan.FromSeconds(5));

            // ----- Act -----
            engine.RemoveFromList(cacheKey, new byte[] { 20, 19, 12, 28 });

            var retrieved = engine.RetrieveList(cacheKey);

            // ----- Assert -----
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(4, retrieved.Count);
            Assert.IsTrue(ByteArraysEqual(new byte[] { 2, 0, 10, 9, 17 }, retrieved[0]));
            Assert.IsTrue(ByteArraysEqual(new byte[] { 201, 6, 2, 2, 0, 0, 9 }, retrieved[1]));
            Assert.IsTrue(ByteArraysEqual(new byte[] { 200, 44, 14 }, retrieved[2]));
            Assert.IsTrue(ByteArraysEqual(new byte[] { 200, 44, 14 }, retrieved[3]));
        }

        private static bool ByteArraysEqual(byte[] a, byte[] b)
        {
            if (a == null && b == null)
            {
                return true;
            }

            if (a == null || b == null)
            {
                return false;
            }

            if (a.Length != b.Length)
            {
                return false;
            }

            for (var i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
