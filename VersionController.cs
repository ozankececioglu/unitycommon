using System;
using UnityEngine;

public class VersionController : MonoBehaviour
{
    public string Version;
    public string MajorBuild;
    public string MinorBuild;
    public string VersionDate;
    
    public void DrawVersionOnScreen(Rect rect)
    {
        string str = String.Format("v.{0}.{1}.{2}.{3}",Version,MajorBuild,MinorBuild,VersionDate);
        GUI.Label(rect, str);
    }

    public void DrawVersionOnLowerRight()
    {
        int widthOfText = 150;
        int heightOfText = 20;
        int margin = 10;
        Rect rect = new Rect(Screen.width - widthOfText - margin, Screen.height - heightOfText - margin, widthOfText, heightOfText);
        this.DrawVersionOnScreen(rect);
    }
}
