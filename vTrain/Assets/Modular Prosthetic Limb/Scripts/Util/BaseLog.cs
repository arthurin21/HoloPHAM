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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;


public abstract class BaseLog
{
    //---------------------------------------
    // VARIABLE DECLARATIONS
    //---------------------------------------
    #region Variable Declarations
    private StreamWriter stream;

    public static string LOG_DIR = "";

    private Queue<string> logQueue = new Queue<string>();
    private Queue<string> logItems = new Queue<string>();
    protected const int MAX_QUEUE = 50;     //1 sec assuming 50 Hz message rate
    protected AutoResetEvent writeEvent = new AutoResetEvent(false);

    private bool active = false;
    private bool running = false;
    protected Thread writeThread;

    protected StringBuilder strBld = new StringBuilder();
    protected static readonly object strBldLock = new object();

    #endregion //Variable Declarations


    //---------------------------------------
    // CONSTRUCTOR / DESTRUCTOR
    //---------------------------------------
    #region Constructor / Destructor

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="s"></param>
    public BaseLog(string s, bool append=false)
    {
#if UNITY_EDITOR
        writeThread = new Thread(new ThreadStart(Run));
        writeThread.IsBackground = true;

        if (append)
            Open(s, append);
        else
            Open(s);
#endif
    }

    #endregion //Constructor / Destructor


    //---------------------------------------
    // LOG FUNCTIONS
    //---------------------------------------
    #region Log Functions

    /// <summary>
    /// Open a file and start writing queue
    /// </summary>
    /// <param name="s"></param>
    public void Open(string s)
    {
#if UNITY_EDITOR
        stream = new StreamWriter(ConstructName(s));
        InitQueue();
        writeEvent.Set();   //trigger to write the first line
#endif
    }

    /// <summary>
    /// Open a file for appending
    /// </summary>
    /// <param name="s"></param>
    public void Open(string s, bool append)
    {
#if UNITY_EDITOR
        stream = new StreamWriter(s, append, System.Text.Encoding.UTF8);
        InitQueue();
#endif
    }

    /// <summary>
    /// Init the queue
    /// </summary>
    protected void InitQueue()
    {
        active = true;
#if UNITY_EDITOR
        writeThread.Start();
#endif
    }

    /// <summary>
    /// Stop writing and close the file
    /// </summary>
    public void Close()
    {
        //stop thread
        active = false;
        writeEvent.Set();

        //wait for thread to stop
#if UNITY_EDITOR
        while (running)
            Thread.Sleep(1);

        //clean up anything that snuck into the queue
        WriteToFile();

        //close file
        stream.Close();
#endif
    }

    /// <summary>
    /// Write the string to a queue before writing to file
    /// </summary>
    /// <param name="str"></param>
    public void WriteLine(string str)
    {
        if (!active)
            return;

        //add string to queue
        lock (logQueue)
        {
            logQueue.Enqueue(str);
        }

        //if queue is full, write to file
        if (logQueue.Count > MAX_QUEUE)
        {
            writeEvent.Set();
        }
    }

    /// <summary>
    /// Write queued up items to the file
    /// </summary>
    private void WriteToFile()
    {
        //move queue items
        lock (logQueue)
        {
            while (logQueue.Count > 0)
            {
                logItems.Enqueue(logQueue.Dequeue());
            }
            logQueue.TrimExcess();
        }

        //write them
        while (logItems.Count > 0)
        {
            string str = logItems.Dequeue();
            stream.WriteLine(str);
        }
        logItems.TrimExcess();

    }

    /// <summary>
    /// Write to file thread
    /// </summary>
    private void Run()
    {
        running = true;
        while (active)
        {
            //wait for a full queue then write it
            writeEvent.WaitOne();
            WriteToFile();
        }
        running = false;
    }

    /// <summary>
    /// Generate file name with a timestamp
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    protected static string ConstructName(string s)
    {
        //string ns = LOG_DIR + "\\" + s;
        //if (!Directory.Exists(LOG_DIR))
        //{
        //    Directory.CreateDirectory(LOG_DIR);
        //}
        string ns = s;
        ns += (LocalClock.TimeNow.ToString("yyMMddTHHmmss"));
        ns += ".txt";
        return ns;
    }

    /// <summary>
    /// Format current date and time
    /// </summary>
    /// <returns></returns>
    public static string NowTimeString()
    {
        return LocalClock.TimeNow.ToString("yyMMdd-HH:mm:ss.fff");
    }

    /// <summary>
    /// Format current time
    /// </summary>
    /// <returns></returns>
    public static string NowTimeOnlyString()
    {
        return LocalClock.TimeNow.ToString("HH:mm:ss.fff");
    }

    #endregion //Log Functions


}//class - BaseLog

