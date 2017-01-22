namespace Trifling.Caching.UnitTests
{
    using System;
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
        public void SimpleCacheEngine_Cache_WhenExpiryTimeIsDaysLater_ThenRetainedInCache()
        {
            // ----- Arrange -----
            const string cacheEntryKey = "first key value";
            var engine = new SimpleCacheEngine();

            // ----- Act -----
            engine.Cache(
                cacheEntryKey,
                new byte[] { 3, 4, 5, 6, 7, 8, 9, 0, 1, 2 },
                TimeSpan.FromHours(240d));

            var retrieved = engine.Retrieve(cacheEntryKey);

            // ----- Assert -----
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(10, retrieved.Length);
            Assert.AreEqual(3, retrieved[0]);
            Assert.AreEqual(45, retrieved.Sum(x => x));
        }

        [TestMethod]
        public void SimpleCacheEngine_Cache_WhenExpiryTimeIsSecondsLater_ThenRemovedFromCache()
        {
            // ----- Arrange -----
            const string cacheEntryKey = "another/key/name";
            var engine = new SimpleCacheEngine();

            // ----- Act -----
            engine.Cache(
                cacheEntryKey,
                new byte[] { 3, 4, 5, 6, 7, 8, 9, 0, 1, 2 },
                TimeSpan.FromSeconds(0.64d));

            Thread.Sleep(1100);

            var retrieved = engine.Retrieve(cacheEntryKey);

            // ----- Assert -----
            Assert.IsNull(retrieved);
        }

        [TestMethod]
        public void SimpleCacheEngine_Remove_WhenRemoveExistingItem_ThenRemovedFromCache()
        {
            // ----- Arrange -----
            const string cacheEntryKey = "key/for/remove/test/0001";
            var engine = new SimpleCacheEngine();

            engine.Cache(
                cacheEntryKey,
                new byte[] { 3, 4, 5, 6, 7, 8, 9, 0, 1, 2 },
                TimeSpan.FromMinutes(20.5d));

            // ----- Act -----
            var removed = engine.Remove(cacheEntryKey);

            var retrieved = engine.Retrieve(cacheEntryKey);

            // ----- Assert -----
            Assert.IsTrue(removed);
            Assert.IsNull(retrieved);
        }

        [TestMethod]
        public void SimpleCacheEngine_Remove_WhenRemoveNonexistentItem_ThenReturnsFalse()
        {
            // ----- Arrange -----
            const string cacheEntryKey = "key/for/remove/test/2222";
            var engine = new SimpleCacheEngine();

            // ----- Act -----
            var removed = engine.Remove(cacheEntryKey);

            var retrieved = engine.Retrieve(cacheEntryKey);

            // ----- Assert -----
            Assert.IsFalse(removed);
            Assert.IsNull(retrieved);
        }
    }
}
