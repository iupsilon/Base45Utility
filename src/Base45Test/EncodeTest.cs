using Base45Utility;
using NUnit.Framework;

namespace Base45Test
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void SimpleEncodeStringTest()
        {
            var base45 = new Base45();
            var messageText = "Hello world";
            var messageBytes = System.Text.Encoding.UTF8.GetBytes(messageText);
            var base45Encoded = base45.Encode(messageBytes);
            
            Assert.AreEqual("%69 VD82EK4F.KEA2", base45Encoded);
            var base45Decoded = base45.DecodeAsString(base45Encoded);
            Assert.AreEqual(messageText, base45Decoded);
        }
        
        [Test]
        public void SimpleDecodeStringTest()
        {
            var base45 = new Base45();
            var base45Encoded = "%69 VD82EK4F.KEA2";
            var base45Decoded = base45.DecodeAsString(base45Encoded);
            Assert.AreEqual("Hello world", base45Decoded);
        }
    }
}