using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using NUnitTests;

namespace NUnitTests.Test
{
    [TestFixture]
    public class SecondClassTests
    {
        [Test]
        public void TwoTest()
        {
            Assert.Fail("auto");
        }
    }
}