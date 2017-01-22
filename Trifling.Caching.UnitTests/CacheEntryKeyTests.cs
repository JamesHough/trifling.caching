namespace Common.UnitTests.Caching
{
    using System;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Trifling.Caching;

    [TestClass]
    public class CacheEntryKeyTests
    {
        [TestMethod]
        public void CacheEntryKey_ToString_WhenOnlyTypeGiven_ThenStringDoesntContainSeparator()
        {
            // ----- Arrange -----
            var c = new CacheEntryKey("my-type");

            // ----- Act -----
            var t = c.ToString();

            // ----- Assert -----
            Assert.AreEqual(-1, t.IndexOf('/'));
        }

        [TestMethod]
        public void CacheEntryKey_ToString_WhenNullKeyElementsGiven_ThenStringDoesntContainSeparator()
        {
            // ----- Arrange -----
            var c = new CacheEntryKey("my-type", null);

            // ----- Act -----
            var t = c.ToString();

            // ----- Assert -----
            Assert.AreEqual(-1, t.IndexOf('/'));
        }

        [TestMethod]
        public void CacheEntryKey_ToString_WhenKeyElementsContainNumerics_ThenNumericStringsInOutput()
        {
            // ----- Arrange -----
            var c = new CacheEntryKey("a-key-name", 23, 45.5m, 1.0025f, 131410099101L);

            // ----- Act -----
            var t = c.ToString();

            // ----- Assert -----
            Assert.AreEqual("a-key-name/23/45.5/1.0025/131410099101", t);
        }

        [TestMethod]
        public void CacheEntryKey_ToString_WhenKeyElementsContainStrings_ThenStringsInOutput()
        {
            // ----- Arrange -----
            var c = new CacheEntryKey("key", "tested here", 'T', (string)null, "MORE/");

            // ----- Act -----
            var t = c.ToString();

            // ----- Assert -----
            Assert.AreEqual("key/tested here/T//MORE/", t);
        }

        [TestMethod]
        public void CacheEntryKey_ToString_WhenKeyElementsContainDates_ThenDateStringsInOutput()
        {
            // ----- Arrange -----
            var c = new CacheEntryKey(
                "a-key-with-mixed-dates", 
                new DateTime(2017, 1, 13),
                new DateTime?(),
                new DateTime?(new DateTime(2014, 12, 4, 17, 13, 41, 155)),
                new DateTimeOffset(new DateTime(2017, 1, 9)),
                new DateTimeOffset(new DateTime(2015, 10, 6), TimeSpan.FromHours(-3d)),
                new DateTimeOffset(new DateTime(2016, 3, 29, 4, 28, 0), TimeSpan.FromHours(6.5)));

            // ----- Act -----
            var t = c.ToString();

            // ----- Assert -----
            Assert.AreEqual("a-key-with-mixed-dates/2017-01-13//2014-12-04T17:13:41.1550000/2017-01-09 (+02:00)/2015-10-06 (-03:00)/2016-03-29T04:28:00.0000000+06:30", t);
        }

        [TestMethod]
        public void CacheEntryKey_ToString_WhenKeyElementsContainBooleans_ThenBooleanStringsInOutput()
        {
            // ----- Arrange -----
            var c = new CacheEntryKey("key-name", true, (bool?)null, false);

            // ----- Act -----
            var t = c.ToString();

            // ----- Assert -----
            Assert.AreEqual("key-name/True//False", t);
        }

        [TestMethod]
        public void CacheEntryKey_ToString_WhenKeyElementsContainTimeSpans_ThenTimeSpanStringsInOutput()
        {
            // ----- Arrange -----
            var c = new CacheEntryKey("key-name", (TimeSpan?)null, TimeSpan.Zero, TimeSpan.FromMinutes(856.666667d));

            // ----- Act -----
            var t = c.ToString();

            // ----- Assert -----
            Assert.AreEqual("key-name//00:00:00/14:16:40", t);
        }

        [TestMethod]
        public void CacheEntryKey_CompareTo_WhenTargetIsNull_ThenAfter()
        {
            // ----- Arrange -----
            var a = new CacheEntryKey("key-name");
            CacheEntryKey b = null;

            // ----- Act -----
            var comp = a.CompareTo(b);

            // ----- Assert -----
            Assert.IsTrue(comp > 0);
        }

        [TestMethod]
        public void CacheEntryKey_CompareTo_WhenBothOnlyHaveSameKeyType_ThenSame()
        {
            // ----- Arrange -----
            var a = new CacheEntryKey("key-name");
            var b = new CacheEntryKey("key-name");

            // ----- Act -----
            var comp = a.CompareTo(b);

            // ----- Assert -----
            Assert.AreEqual(0, comp);
        }

        [TestMethod]
        public void CacheEntryKey_CompareTo_WhenBothOnlyHaveKeyType_ThenStringCompareKeysResultBefore()
        {
            // ----- Arrange -----
            var a = new CacheEntryKey("key-mame");
            var b = new CacheEntryKey("key-name");

            // ----- Act -----
            var comp = a.CompareTo(b);

            // ----- Assert -----
            Assert.IsTrue(comp < 0);
        }

        [TestMethod]
        public void CacheEntryKey_CompareTo_WhenBothOnlyHaveKeyType_ThenStringCompareKeysResultAfter()
        {
            // ----- Arrange -----
            var a = new CacheEntryKey("key-named");
            var b = new CacheEntryKey("key-name");

            // ----- Act -----
            var comp = a.CompareTo(b);

            // ----- Assert -----
            Assert.IsTrue(comp > 0);
        }

        [TestMethod]
        public void CacheEntryKey_CompareTo_WhenBothHaveSameKeyType_AndTargetHasMoreElements_ThenBefore()
        {
            // ----- Arrange -----
            var a = new CacheEntryKey("key-name");
            var b = new CacheEntryKey("key-name", 13);

            // ----- Act -----
            var comp = a.CompareTo(b);

            // ----- Assert -----
            Assert.IsTrue(comp < 0);
        }

        [TestMethod]
        public void CacheEntryKey_CompareTo_WhenBothHaveSameKeyType_AndTargetHasFewerElements_ThenAfter()
        {
            // ----- Arrange -----
            var a = new CacheEntryKey("key-name", DateTime.Today);
            var b = new CacheEntryKey("key-name");

            // ----- Act -----
            var comp = a.CompareTo(b);

            // ----- Assert -----
            Assert.IsTrue(comp > 0);
        }

        [TestMethod]
        public void CacheEntryKey_CompareTo_WhenBothAreSameDeepIntoElements_AndTargetHasHigherValueInElement_ThenBefore()
        {
            // ----- Arrange -----
            var a = new CacheEntryKey("key-name", new DateTime(2017, 6, 13), 344.55d, "client", 9552, 0);
            var b = new CacheEntryKey("key-name", new DateTime(2017, 6, 13), 344.55d, "client", 9882, 0);

            // ----- Act -----
            var comp = a.CompareTo(b);

            // ----- Assert -----
            Assert.IsTrue(comp < 0);
        }

        [TestMethod]
        public void CacheEntryKey_CompareTo_WhenBothAreSameDeepIntoElements_AndTargetHasLowerValueInElement_ThenAfter()
        {
            // ----- Arrange -----
            var a = new CacheEntryKey("key-name", new DateTime(2017, 6, 13), 344.55d, "client", 9552, 0);
            var b = new CacheEntryKey("key-name", new DateTime(2017, 6, 13), 344.55d, "client", 4552, 0);

            // ----- Act -----
            var comp = a.CompareTo(b);

            // ----- Assert -----
            Assert.IsTrue(comp > 0);
        }

        [TestMethod]
        public void CacheEntryKey_CompareTo_WhenBothAreSameDeepIntoElements_ThenSame()
        {
            // ----- Arrange -----
            var a = new CacheEntryKey("key-name", new DateTime(2017, 6, 13), 344.55d, "client", 9552, 0);
            var b = new CacheEntryKey("key-name", new DateTime(2017, 6, 13), 344.55d, "client", 9552, 0);

            // ----- Act -----
            var comp = a.CompareTo(b);

            // ----- Assert -----
            Assert.AreEqual(0, comp);
        }

        [TestMethod]
        public void CacheEntryKey_ImplicitConvert_WhenConverted_ThenStringProduced()
        {
            // ----- Arrange -----
            var c = new CacheEntryKey("my-type", 4, new DateTime(2010, 6, 10));

            // ----- Act -----
            string t = c;

            // ----- Assert -----
            Assert.AreEqual("my-type/4/2010-06-10", t);
        }

        [TestMethod]
        public void CacheEntryKey_ExplicitConvert_WhenConverted_ThenStringProduced()
        {
            // ----- Arrange -----
            var c = new CacheEntryKey("my-type", 4, new DateTime(2010, 6, 10));

            // ----- Act -----
            var t = ((string)c).ToUpperInvariant();

            // ----- Assert -----
            Assert.AreEqual("MY-TYPE/4/2010-06-10", t);
        }

        [TestMethod]
        public void CacheEntryKey_GetHashCode_WhenTwoSameCacheKeys_ThenSameHashCodesReturned()
        {
            // ----- Arrange -----
            var a = new CacheEntryKey("keytype", 4, 3, 2, 1, "0", null, 'Y', 0.9, 1.9, 2.9, long.MaxValue / 3L);
            var b = new CacheEntryKey("keytype", 4, 3, 2, 1, "0", null, 'Y', 0.9, 1.9, 2.9, long.MaxValue / 3L);

            // ----- Act -----
            var hash1 = a.GetHashCode();
            var hash2 = b.GetHashCode();

            // ----- Assert -----
            Assert.AreEqual(hash1, hash2, "These identical cache entry keys should have identical hash codes.");
        }

        [TestMethod]
        public void CacheEntryKey_GetHashCode_WhenTwoDifferentCacheKeys_ThenDifferentCodesReturned()
        {
            // ----- Arrange -----
            var a = new CacheEntryKey("keytype", 4, 3, 2, 1, "0", null, 'Y', 0.9, 1.9, 2.9, long.MaxValue / 3L);
            var b = new CacheEntryKey("keytype", 4, 3, 8, 7, "c", null, 'N', 0.9, 1.9, 2.9, long.MaxValue / 3L);

            // ----- Act -----
            var hash1 = a.GetHashCode();
            var hash2 = b.GetHashCode();

            // ----- Assert -----
            Assert.AreNotEqual(hash1, hash2, "These different cache entry keys should not have identical hash codes.");
        }

        [TestMethod]
        public void CacheEntryKey_GetHashCode_WhenTwoDifferentCacheKeysWithoutElements_ThenDifferentCodesReturned()
        {
            // ----- Arrange -----
            var a = new CacheEntryKey("key-type");
            var b = new CacheEntryKey("keytype");

            // ----- Act -----
            var hash1 = a.GetHashCode();
            var hash2 = b.GetHashCode();

            // ----- Assert -----
            Assert.AreNotEqual(hash1, hash2, "These different cache entry keys should not have identical hash codes.");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidCastException))]
        public void CacheEntryKey_Equals_WhenComparedToInvalidObject_ThenException()
        {
            // ----- Arrange -----
            var c = new CacheEntryKey("test-type", 14);

            // ----- Act -----
            var result = c.Equals(14);

            // ----- Assert -----
            Assert.Fail("The expected exception did not occur.");
        }

        [TestMethod]
        public void CacheEntryKey_Equals_WhenSameInstance_ThenReturnsTrue()
        {
            // ----- Arrange -----
            var a = new CacheEntryKey("test-type", 14);
            var b = a;

            // ----- Act -----
            var result = a.Equals(b);

            // ----- Assert -----
            Assert.IsTrue(result);
        }
    }
}
