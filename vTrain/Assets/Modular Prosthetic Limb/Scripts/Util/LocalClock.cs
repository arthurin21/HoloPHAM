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
using System.Diagnostics;

public class LocalClock
{
    protected static LocalClock instance = null; /* singleton */
    protected static readonly object clockLock = new object();
    private DateTime clock;
    private Stopwatch watch;

    /// <summary>
    /// Returns current local time
    /// </summary>
    public static DateTime TimeNow
    {
        get
        {
            LocalClock lc = LocalClock.GetInstance();
            return lc.clock + lc.watch.Elapsed;
        }
    }

    /// <summary>
    /// constructor to start the clock
    /// </summary>
    private LocalClock()
    {
        //start the clock
        clock = DateTime.Now;
        watch = new Stopwatch();
        watch.Start();
    }

    /// <summary>
    /// singleton constructor
    /// </summary>
    /// <returns></returns>
    private static LocalClock GetInstance()
    {
        lock (clockLock)
        {
            if (instance == null)
            {
                instance = new LocalClock();
            }
            return instance;
        }
    }

}

