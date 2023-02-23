namespace CypherCLI.Tests

open System
open Microsoft.VisualStudio.TestTools.UnitTesting

open CypherCLI.Lib.Crypto

[<TestClass>]
type TestClass() =

    [<TestMethod>]
    member this.AES_Encrypt_Decrypt_IsSuccessful() =
        let cypher = AES.Encrypt "foo" "bar"
        let plain = AES.Decrypt cypher "bar"
        Assert.AreEqual("foo", plain)
