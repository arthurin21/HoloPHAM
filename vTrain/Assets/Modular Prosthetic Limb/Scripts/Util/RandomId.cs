using System.Collections;
using System.Text;

namespace VIEUtil
{
    /// <summary>
    /// Generates a random Id consisting of two capitalized letters followed
    /// by a number in the range of 0 to 2^16-1.
    /// </summary>
    public class RandomId
    {
        static protected byte[] ms_letters = new byte[2];
        static protected System.Random ms_rnd = new System.Random();
        static protected UTF8Encoding ms_utfEncode = 
            new UTF8Encoding(false, true);

        static public string Get()
        {
            ms_letters[0] = (byte)ms_rnd.Next(65, 90);
            ms_letters[1] = (byte)ms_rnd.Next(65, 90);
            short number = (short)ms_rnd.Next(short.MaxValue);
            return ms_utfEncode.GetString(ms_letters) + number.ToString();
        }
    }
}
