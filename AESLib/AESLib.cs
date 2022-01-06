using System;
using System.IO;

using AesEverywhere;
using LanguageExt;
using static LanguageExt.Prelude;

namespace AESLib {

    // Contains string to string static methods to encrypt/decrypt data
    public static class AES {
        public static Option<string> Encrypt(string plainText, string key) {
            try {
                return Some(new AES256().Encrypt(plainText, key));
            } catch {
                return None;
            }
        }
        public static Option<string> Decrypt(string cipherText, string key) {
            try {
                return Some(new AES256().Decrypt(cipherText, key));
            } catch {
                return None;
            }
        }
        public static Option<WriteEffect> Encrypt(string plainText, string key, string outputFilePath) {

            var cipherText = Encrypt(plainText, key);

            return cipherText.Match(
                Some: result => new WriteEffect(result, outputFilePath, () => Console.WriteLine("Wrote encrypted file to: {0}", outputFilePath)),
                None: () => Option<WriteEffect>.None);
        }

        public static Option<WriteEffect> Decrypt(string cipherText, string key, string outputFilePath) {

            var plaintext = Decrypt(cipherText, key);

            return plaintext.Match(
                Some: result => new WriteEffect(result, outputFilePath, () => Console.WriteLine("Wrote decrypted file to: {0}", outputFilePath)),
                None: () => Option<WriteEffect>.None
            );
        }


        // File to file encryption/decryption where outputPath can be a directory.
        // If outputPath is a directory, the output will be written into a file with the same name
        // as the input file inside that directory.
        public static Option<WriteEffect> ProcessFile(string key, bool isEncryptMode, string filePath, string outputPath) {

            Option<WriteEffect> effect = None;

            string fullOutputPath = getFullOutputPath(filePath, outputPath);
            string text = System.IO.File.ReadAllText(filePath);

            return isEncryptMode
                ? Encrypt(text, key, fullOutputPath)
                : Decrypt(text, key, fullOutputPath);
        }


        private static string getFullOutputPath(string filePath, string outputPath) {

            if (PathUtils.IsDirectory(outputPath)) {
                return Path.Join(outputPath, Path.GetFileName(filePath));
            }
            return outputPath;
        }

    }


    public class PathUtils {
        public static bool IsDirectory(string path) {
            return string.Empty == Path.GetFileName(path);
        }

        public static void CreateIfNotExists(string dirPath) {
            try {
                bool exists = System.IO.File.GetAttributes(dirPath).HasFlag(FileAttributes.Directory);
                if (exists) {
                    return;
                }
            } catch {
                Directory.CreateDirectory(dirPath);
            }
        }

        public static bool IsFile(string path) {
            return !string.IsNullOrEmpty(Path.GetFileName(path));
        }
    }

    public class WriteEffect {
        public readonly string data;
        public readonly string path;
        public readonly Action write;

        public WriteEffect(string data, string path) {
            this.data = data;
            this.path = path;
            write = () => {
                PathUtils.CreateIfNotExists(Path.GetDirectoryName(path));
                System.IO.File.WriteAllText(path, data);
            };
        }

        public WriteEffect(string data, string path, Action afterWriting) {
            this.data = data;
            this.path = path;
            write = () => {
                PathUtils.CreateIfNotExists(Path.GetDirectoryName(path));
                System.IO.File.WriteAllText(path, data);
                afterWriting();
            };
        }
    }
}