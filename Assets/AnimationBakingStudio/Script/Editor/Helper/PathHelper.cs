using System;
using System.IO;

namespace ABS
{
    public class PathHelper
    {
        public static string MakeDateTimeString()
        {
            DateTime now = DateTime.Now;
            string year = now.Year.ToString();
            string month = now.Month >= 10 ? now.Month.ToString() : "0" + now.Month;
            string day = now.Day >= 10 ? now.Day.ToString() : "0" + now.Day;
            string hour = now.Hour >= 10 ? now.Hour.ToString() : "0" + now.Hour;
            string minute = now.Minute >= 10 ? now.Minute.ToString() : "0" + now.Minute;
            string second = now.Second >= 10 ? now.Second.ToString() : "0" + now.Second;
            return year.Substring(2, 2) + month + day + "_" + hour + minute + second;
        }

        public static void CorrectPathString(ref string pathString)
        {
            if (pathString.Length == 0)
                return;

            char[] invalidPathChars = Path.GetInvalidPathChars();

            string validPathString = "";
            for (int i = 0; i < pathString.Length; ++i)
            {
                bool invalid = false;
                foreach (char invalidChar in invalidPathChars)
                {
                    if (pathString[i] == invalidChar)
                    {
                        invalid = true;
                        break;
                    }
                }
                validPathString += invalid ? '_' : pathString[i];
            }

            pathString = validPathString;
        }
    }
}
