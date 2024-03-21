using System;

//Fusion Connection Token Utility methods
public class ConnectionTokenUtils 
{
    /// <summary>
    /// create new random token
    /// </summary>
    public static byte[] NewToken() => Guid.NewGuid().ToByteArray();

    /// <summary>
    /// Converts a token into a Hash format
    /// </summary>
    /// <param name = "token"> Token to be hashed</param>
    /// <return>Token Hash</return>
    public static int HashToken(byte[] token) => new Guid(token).GetHashCode();

    ///<summary>
    ///Converts a Token into a String
    ///</summary>
    ///<param name = "token">Token to be parsed</param>
    ///<returns>Token as a string</returns>
    public static string TokenToString(byte[] token) => new Guid(token).ToString();
}
