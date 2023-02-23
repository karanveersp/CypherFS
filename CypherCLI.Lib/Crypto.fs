namespace CypherCLI.Lib.Crypto

/// This module is a translation to F# of the C# class available at:
/// https://github.com/mervick/aes-everywhere/blob/master/net/src/aes256.cs
///
/// It provides functions to encrypt/decrypt data using AES.
module AES =
    open System
    open System.Security.Cryptography
    open System.IO
    open System.Text

    [<Literal>]
    let BlockSize = 16

    [<Literal>]
    let KeyLen = 32

    [<Literal>]
    let IvLen = 16

    /// Concatenates two byte arrays
    let Concat (a: byte []) (b: byte []) : byte [] =
        let output = Array.zeroCreate<byte> (a.Length + b.Length)

        for i in 0 .. (a.Length - 1) do
            output.[i] <- a.[i]

        for j in 0 .. (b.Length - 1) do
            output.[a.Length + j] <- b.[j]

        output

    /// Concatenates the given string and subsequent byte array
    let ConcatStr (a: string) (b: byte []) : byte [] = Concat (Encoding.UTF8.GetBytes(a)) b

    /// <summary>
    /// Derive key and iv.
    /// </summary>
    /// <param name="passphrase">Passphrase</param>
    /// <param name="salt">Salt</param>
    let DeriveKeyAndIv (passphrase: string) (salt: byte []) : (byte [] * byte []) =
        let md5 = MD5.Create()

        let key = Array.zeroCreate<byte> KeyLen
        let iv = Array.zeroCreate<byte> IvLen

        let mutable dx: byte [] = [||]
        let mutable salted: byte [] = [||]
        let pass = Encoding.UTF8.GetBytes(passphrase)

        for i in 0 .. (KeyLen + IvLen / 16) do
            dx <- Concat (Concat dx pass) salt
            dx <- md5.ComputeHash(dx)
            salted <- Concat salted dx

        Array.Copy(salted, 0, key, 0, KeyLen)
        Array.Copy(salted, KeyLen, iv, 0, IvLen)
        key, iv

    /// Encryptes the data bytes with the given key
    let EncryptBytes (data: byte []) (passphrase: string) : string =
        use random = RandomNumberGenerator.Create()
        let salt = Array.zeroCreate<byte> 8
        random.GetBytes(salt)

        let key, iv = DeriveKeyAndIv passphrase salt

        use aes = Aes.Create()
        aes.BlockSize <- BlockSize * 8
        aes.Mode <- CipherMode.CBC
        aes.Padding <- PaddingMode.PKCS7
        aes.Key <- key
        aes.IV <- iv
        let encryptor = aes.CreateEncryptor(aes.Key, aes.IV)
        use msEncrypt = new MemoryStream()
        use csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write)
        csEncrypt.Write(data, 0, data.Length)
        csEncrypt.FlushFinalBlock()
        let encrypted = msEncrypt.ToArray()

        System.Convert.ToBase64String(Concat (ConcatStr "Salted__" salt) encrypted)

    /// Encryptes the given text with the given key
    let Encrypt (text: string) (passphrase: string) : string =
        EncryptBytes (Encoding.UTF8.GetBytes(text)) passphrase

    /// Decrypts the given encrypted text with the key to a byte array
    let DecryptToBytes (encrypted: string) (passphrase: string) : byte [] =
        let ct = System.Convert.FromBase64String(encrypted)

        if ct = null || ct.Length <= 0 then
            Array.zeroCreate<byte> (0)
        else
            let salted = Array.zeroCreate<byte> (8)
            Array.Copy(ct, 0, salted, 0, 8)

            if Encoding.UTF8.GetString(salted) <> "Salted__" then
                Array.zeroCreate<byte> (0)
            else
                let salt = Array.zeroCreate<byte> (8)
                Array.Copy(ct, 8, salt, 0, 8)

                let cipherText = Array.zeroCreate<byte> (ct.Length - 16)
                Array.Copy(ct, 16, cipherText, 0, ct.Length - 16)

                let key, iv = DeriveKeyAndIv passphrase salt

                use aes = Aes.Create()
                aes.BlockSize <- BlockSize * 8
                aes.Mode <- CipherMode.CBC
                aes.Padding <- PaddingMode.PKCS7
                aes.Key <- key
                aes.IV <- iv
                let decryptor = aes.CreateDecryptor(aes.Key, aes.IV)
                use msDecrypt = new MemoryStream()
                use csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Write)
                csDecrypt.Write(cipherText, 0, cipherText.Length)
                csDecrypt.FlushFinalBlock()
                let decrypted = msDecrypt.ToArray()
                decrypted

    /// Decrypts the given encrypted text with the key
    let Decrypt (encrypted: string) (passphrase: string) : string =
        Encoding.UTF8.GetString(DecryptToBytes encrypted passphrase)

    /// Encrypts plaintext and returns an option
    let EncryptOption (plainText: string) (key: string) : string option =
        try
            Some(Encrypt plainText key)
        with
        | _ -> None

    /// Decrypts a ciphertext and returns an option
    let DecryptOption (cipherText: string) (key: string) : string option =
        try
            Some(Decrypt cipherText key)
        with
        | _ -> None
