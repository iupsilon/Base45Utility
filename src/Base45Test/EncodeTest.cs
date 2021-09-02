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
            var plainText = "Hello world";
            var messageBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            var b45Encoded = base45.Encode(messageBytes);
            
            Assert.AreEqual("%69 VD82EK4F.KEA2", b45Encoded);

            var b45Decoded = base45.DecodeAsString(b45Encoded);

            Assert.AreEqual(plainText, b45Decoded);

        }
    }
}