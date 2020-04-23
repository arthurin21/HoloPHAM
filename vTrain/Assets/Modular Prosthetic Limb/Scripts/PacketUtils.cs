//
// README: IMPORTANT WARNING - EXPORT CONTROL LANGUAGE
// 
// This information, software, technology being shared MUST be 
// handled in accordance with the statement below.  All documentation
// related to Software and Technology Development associated with 
// this shared information must include this statement:
//
// “The information we are providing contains proprietary software/
// technology and is therefore export controlled.   The specific 
// Export Control Classification Number (ECCN) applied to this 
// software, 3D991, is currently controlled to only 5 countries: 
// N. Korea, Syria, Sudan, Cuba, or Iran.  Before providing this 
// software or data to any foreign person, you should consult with 
// your organization’s export compliance or legal office.  Of course,
// the nature of our contractual relationship requires that only 
// people associated with Revolutionizing Prosthetics Phase 3 may 
// have access to this information.”
//

using System.Collections;
using System.Runtime.InteropServices;

namespace VIEUtil
{
    public class PacketUtils
    {
        static char[] chArray = new char[255];

        /// <summary>
        /// Extracts a variable length string of up to 255 characters from the
        /// given byte buffer.
        /// </summary>
        /// <param name="buffer">The first byte of the buffer contains the size
        /// of the string.</param>
        /// <param name="startInd"></param>
        /// <param name="str">Set to value of string contained in buffer.</param>
        /// <param name="endInd">Set to index of first byte after string.</param>
        static public void GetString(byte[] buffer, int startInd, 
            out string str, out int endInd)
        {
            str = string.Empty;
            byte strLen = buffer[startInd];
            endInd = startInd + 1;

            if (strLen > 0)
            {
                for (int i = 0; i < strLen; i++, endInd++)
                {
                    chArray[i] = (char) buffer[endInd];
                }
                str = new string(chArray, 0, strLen);
            }
        }

        /// <summary>
        /// This structure is used to trick C# into converting a float into
        /// four contiguous bytes and vice versa.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct FloatIntUnion
        {
            [FieldOffset(0)]
            private int i;

            [FieldOffset(0)]
            private float f;

            public static int ToInt32(float value)
            {
                FloatIntUnion u = new FloatIntUnion();
                u.f = value;

                return u.i;
            }

            public static float ToFloat32(int value)
            {
                FloatIntUnion u = new FloatIntUnion();
                u.i = value;

                return u.f;
            }
        }

        /// <summary>
        /// Prepares a float for network transmission by placing it in the
        /// given byte buffer.  Does not allocate memory like 
        /// BitConverter.GetBytes(float).
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="startInd">Where in the buffer to write the float.</param>
        /// <param name="value">The float to place in the buffer.</param>
        static public void PutFloat(byte[] buffer, int startInd, float value)
        {
            int flt = FloatIntUnion.ToInt32(value);
            buffer[startInd] = (byte)flt;
            buffer[startInd + 1] = (byte)(flt >> 8);
            buffer[startInd + 2] = (byte)(flt >> 16);
            buffer[startInd + 3] = (byte)(flt >> 24);
        }

        /// <summary>
        /// Prepares a short for network transmission by placing it in the
        /// given byte buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="startInd"></param>
        /// <param name="value"></param>
        static public void PutShort(byte[] buffer, int startInd , short value)
        {
            buffer[startInd] = (byte)value;
            buffer[startInd + 1] = (byte)(value >> 8);
        }

    }
}
