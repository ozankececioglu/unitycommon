using UnityEngine;
using System;
using System.IO;

public enum DebugLevel
{
    Trace = 0,
    Debug,
    Info,
    Warn,
    Error,
    Fatal
};
//A debugger by MSI
public class Debugger
{
    public static bool DebugON = false;
    public static DebugLevel Level = DebugLevel.Info;
    public static bool PrintOnConsole = false;
    public static string LogDirectory = "";
    //some should init debugger once, this part reads debug.config file...
    public static void InitDebugger()
    {
        try
        {
            //check if there is a debug.config file
            using(StreamReader reader = new StreamReader(new FileStream(LogDirectory+@"debug.config",FileMode.Open,FileAccess.Read,FileShare.Read)))
            {
                string s = reader.ReadLine();
                if ( s.Contains("true") )//if first line contains true, bring the rain here...
                {
                    Debugger.DebugON = true;
                }
                else
                {
                    Debugger.DebugON = false;
                }
                s = reader.ReadLine();
                string[] sList = s.Split('=');
                s = sList [1].Trim();
                object o = Enum.Parse(typeof(DebugLevel),s);//convert second line to log level enum
                Debugger.Level = ( DebugLevel )o;
                s = reader.ReadLine();
                if ( s.Contains("true") )//name says a lot...
                {
                    Debugger.PrintOnConsole = true;
                }
                else
                {
                    Debugger.PrintOnConsole = false;
                }
                Debugger.Log("Debug is on, Debug Level is = " + Debugger.Level);
            }
        }
        catch(Exception)//init default values, actually it is not necessary to add them as they are already initial values...anyway...
        {
            Debugger.DebugON = false;
            Debugger.Level = DebugLevel.Info;
            Debugger.PrintOnConsole = false;
        }
    }


    public static void Fatal(object o)
    {
        Debugger.Log(o, DebugLevel.Fatal);
    }

    public static void Error(object o)
    {
        Debugger.Log(o, DebugLevel.Error);
    }

    public static void Warn(object o)
    {
        Debugger.Log(o, DebugLevel.Warn);
    }

    public static void Info(object o)
    {
        Debugger.Log(o, DebugLevel.Info);
    }

    public static void Debug(object o)
    {
        Debugger.Log(o, DebugLevel.Debug);
    }

    public static void Trace(object o)
    {
        Debugger.Log(o, DebugLevel.Trace);
    }
    //it assumes Info level as the default log level...
    public static void Log(object obj, DebugLevel level = DebugLevel.Info)
    {
        if ( !Debugger.DebugON )
            return;
        int lInt = ( int )level;
        int dLInt = ( int )Debugger.Level;

        if ( lInt < dLInt )//if current log level is lower than usage level, don't add it to the log file
            return;
        try
        {
            //Opens a new file stream which allows asynchronous reading and writing
            using ( StreamWriter sw = new StreamWriter(new FileStream(LogDirectory + @"simlog.txt", FileMode.Append, FileAccess.Write, FileShare.ReadWrite)) )
            {
                //Writes the log level name with the log, date and time parameters
                string log = String.Format("{0} ({1}) - {3}: {2}", DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString(), obj.ToString(), level);
                sw.WriteLine(log);
                sw.WriteLine("");//add a new line
                if ( Debugger.PrintOnConsole )
                {
                    UnityEngine.Debug.Log(log);//print on console too
                }
                sw.Flush();
            }
        }
        catch ( IOException )
        {
            if ( !File.Exists(LogDirectory + @"simlog.txt") )
            {
                File.Create(LogDirectory + @"simlog.txt");
            }
        }
    }
}
