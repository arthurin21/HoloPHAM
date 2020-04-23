//******************************************************************************|
//                                                                              |
//                         E X P O R T   C O N T R O L                          |
//                                                                              |
//  The information we are providing contains proprietary software/technology   |
//  and is therefore exportcontrolled. The specific Export Control              |
//  Classification Number (ECCN) applied to this software, 3D991, is currently  |
//  controlled to only 5 countries: N. Korea, Syria, Sudan, Cuba, or Iran.      |
//  Before providing this software or data to any foreign person, you should    |
//  consult with your organization’s export compliance or legal office.         |
//  Of course, the nature of our contractual relationship requires that only    |
//  people associated with Revolutionizing Prosthetics Phase 3 may have access  |
//  to this information.                                                        |
//                                                                              |
//      The Johns Hopkins University / Applied Physics Laboratory 2011          |
//                                                                              |
//******************************************************************************|

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class ForceLog : BaseLog
{
    //---------------------------------------
    // VARIABLE DECLARATIONS
    //---------------------------------------
    #region Variable Declarations
    #endregion //Variable Declarations


    //---------------------------------------
    // CONSTRUCTOR / DESTRUCTOR
    //---------------------------------------
    #region Constructor / Destructor
    public ForceLog()
        : base("ForceLog")
    {
        //Clear out the "strBld" or "String Builder" (inherited from "BaseLog"
        strBld.Remove(0, strBld.Length);
        strBld.Append("Time(1), thContact(2), indContact(3), midContact(4), thNormX(5), thNormY(6), thNormZ(7), thVelX(8), thVelY(9), thVelZ(10)");
        strBld.Append("indNormX(11), indNormY(12), indNormZ(13), indVelX(14), indVelY(15), indVelZ(16), ");
        strBld.Append("midNormX(17), midNormY(18), midNormZ(19), midVelX(20), midVelY(21), midVelZ(22), ");
        strBld.Append("shFE_cmd(23), shFE_act(24), shAA_cmd(25), shAA_act(26), humRot_cmd(27), humRot_act(28), ");
        strBld.Append("elbFE_cmd(29), elbFE_act(30), wrRot_cmd(31), wrRot_act(32), wrDev_cmd(33), wrDev_act(34), wrFE_cmd(35), wrFE_act(36), ");
        strBld.Append("thAA_cmd(37), thAA_act(38), thMcp_cmd(39), thMpc_act(40), thProx_cmd(41), thProx_act(42), thMed_cmd(43), thMed_act(44), ");
        strBld.Append("indAA_cmd(45), indAA_act(46), indMcp_cmd(47), indMpc_act(48), indProx_cmd(49), indProx_act(50), indMed_cmd(51), indMed_act(52), ");
        strBld.Append("midAA_cmd(53), midAA_act(54), midMcp_cmd(55), midMpc_act(56), midProx_cmd(57), midProx_act(58), midMed_cmd(59), midMed_act(60), ");
        WriteLine(strBld.ToString());
    }//function - 
    #endregion //Constructor / Destructor


    //---------------------------------------
    // LOG FUNCTIONS
    //---------------------------------------
    #region Log Functions
    public void WriteJoints(bool thmbContact, bool indContact, bool midContact,
                            float[] thmbCol, float[] indCol, float[] midCol,
                            float[] cmdArm, float[] actArm,
                            float[] cmdThmb, float[] cmdInd, float[] cmdMid,
                            float[] actThmb, float[] actInd, float[] actMid)
    {
        //StringBuilder sb = new StringBuilder(mudMsg.MessageTime.ToString("yyMMdd-HH:mm:ss.fff"));
        lock (strBldLock)
        {
            //strBld.Clear();  //method not in .NET 3.5
            strBld.Remove(0, strBld.Length);
            strBld.Append(NowTimeOnlyString());

            //contact/collisions
            strBld.AppendFormat(",{0},{1},{2}", Convert.ToByte(thmbContact), Convert.ToByte(indContact), Convert.ToByte(midContact));

            //thumb normal & relative velocity
            for (int i = 0; i < thmbCol.Length; i++)
                strBld.AppendFormat(",{0}", thmbCol[i]);

            //index normal & relative velocity
            for (int i = 0; i < indCol.Length; i++)
                strBld.AppendFormat(",{0}", indCol[i]);

            //middle normal & relative velocity
            for (int i = 0; i < midCol.Length; i++)
                strBld.AppendFormat(",{0}", midCol[i]);

            //arm joints
            for (int i=0; i<cmdArm.Length; i++)
                strBld.AppendFormat(",{0},{1}", cmdArm[i], actArm[i]);

            //thumb joints
            for (int i=0; i<cmdThmb.Length; i++)
                strBld.AppendFormat(",{0},{1}", cmdThmb[i], actThmb[i]);

            //index joints
            for (int i=0; i<cmdInd.Length; i++)
                strBld.AppendFormat(",{0},{1}", cmdInd[i], actInd[i]);

            //middle joints
            for (int i = 0; i < cmdMid.Length; i++)
                strBld.AppendFormat(",{0},{1}", cmdMid[i], actMid[i]);

            WriteLine(strBld.ToString());
        }//lock - stringbuild for each sensor

    }//function - WriteJoints
    #endregion //Log Functions


}//class - ForceLog

