using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Oxtail.Utils
{
    public class NumberFormatter
    {
        public static string FormatValue(float value)
        {
            string formatedText;

            value = Mathf.Ceil(value);

            if (value < 1000)
                formatedText = value.ToString();
            else if (value < 1000000)
                formatedText = (value / 1000).ToString("F1") + "K";
            else if (value < 1000000000000)
                formatedText = (value / 1000000).ToString("F") + "M";
            else
                formatedText = (value / 1000000000000).ToString("F1") + "B";

            return formatedText;
        }

        public static string FormatValue(int value)
        {
            return FormatValue((float)value);
        }
    }
}

