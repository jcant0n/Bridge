﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Bridge.Test;
using Bridge.ClientTest;

namespace Bridge.ClientTest.Exceptions
{
    [Category(Constants.MODULE_FORMATEXCEPTION)]
    [TestFixture(TestNameFormat = "FormatException - {0}")]
    public class FormatExceptionTests
    {
        [Test]
        public void TypePropertiesAreCorrect()
        {
            Assert.AreEqual("System.FormatException", typeof(FormatException).GetClassName(), "Name");
            object d = new FormatException();
            Assert.True(d is FormatException, "is FormatException");
            Assert.True(d is Exception, "is Exception");
        }

        [Test]
        public void DefaultConstructorWorks()
        {
            var ex = new FormatException();
            Assert.True((object)ex is FormatException, "is FormatException");
            Assert.AreEqual(null, ex.InnerException, "InnerException");
            Assert.AreEqual("Invalid format.", ex.Message);
        }

        [Test]
        public void ConstructorWithMessageWorks()
        {
            var ex = new FormatException("The message");
            Assert.True((object)ex is FormatException, "is FormatException");
            Assert.AreEqual(null, ex.InnerException, "InnerException");
            Assert.AreEqual("The message", ex.Message);
        }

        [Test]
        public void ConstructorWithMessageAndInnerExceptionWorks()
        {
            var inner = new Exception("a");
            var ex = new FormatException("The message", inner);
            Assert.True((object)ex is FormatException, "is FormatException");
            Assert.True(ReferenceEquals(ex.InnerException, inner), "InnerException");
            Assert.AreEqual("The message", ex.Message);
        }
    }
}
