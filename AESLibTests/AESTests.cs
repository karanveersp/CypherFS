using Xunit;
using AESLib;

namespace AESLibTests {
    public class AesTests {
        [Fact]
        public void EncodeReturnsNonNullWriteEffect() {
            var (plainText, key, outputPath) = ("Some data", "myKey", "OutputPath");
            var writeEffect = AES.Encrypt(plainText, key, outputPath);

            Assert.True(writeEffect.IsSome);
        }

        [Fact]
        public void EncryptReturnsEffectWithExpectedPath() {
            var (plainText, key, outputPath) = ("Some data", "myKey", "OutputPath\\Plain.txt");
            var e = AES.Encrypt(plainText, key, outputPath);

            string actualPath = e.Match<string>(
                Some: v => v.path, 
                None: () => ""
            );

            Assert.Equal("OutputPath\\Plain.txt", actualPath);
        }

        [Fact]
        public void DecryptReturnsEffectWithExpectedPathAndData() {
            var (cipher, key, outputPath) = ("U2FsdGVkX1920feeCwXEREEB8CQvZnqvkIR9ePjLtyY=", "myKey", "Path\\To\\Plain.txt");
            var e = AES.Decrypt(cipher, key, outputPath);
            
            WriteEffect actual = e.Match(
                Some: v => v,
                None: () => null
            );
            
            Assert.Equal("Path\\To\\Plain.txt", actual.path);
            Assert.Equal("Some data", actual.data);
        }
    }
}
