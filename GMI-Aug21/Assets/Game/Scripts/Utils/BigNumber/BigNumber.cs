using System;
using UnityEngine;

namespace Oxtail.Utils
{
    [Serializable]
    public struct BigNumber : IComparable<BigNumber>
    {
        public double m_Mantissa;
        public int m_Exponent;

        [NonSerialized] private string m_CachedAbbreviation;
        [NonSerialized] private double m_CachedMantissa;
        [NonSerialized] private int m_CachedExponent;

        const double EPSILON = 1e-12;

        public BigNumber(double value)
        {
            m_CachedAbbreviation = null;
            m_CachedMantissa = 0;
            m_CachedExponent = 0;

            if (value == 0)
            {
                m_Mantissa = 0;
                m_Exponent = 0;
            }
            else
            {
                m_Exponent = (int)Math.Floor(Math.Log10(Math.Abs(value)));
                m_Mantissa = value / Math.Pow(10, m_Exponent);
                Normalize();
            }
        }

        public BigNumber(double m, int e)
        {
            m_Mantissa = m;
            m_Exponent = e;

            m_CachedAbbreviation = null;
            m_CachedMantissa = 0;
            m_CachedExponent = 0;

            Normalize();
        }

        public static BigNumber FromLog10(double logValue, bool negative = false)
        {
            if (double.IsNegativeInfinity(logValue))
                return new BigNumber(0);

            int exponent = (int)Math.Floor(logValue);
            double mantissa =
                Math.Pow(10, logValue - exponent);

            if (negative)
                mantissa = -mantissa;

            return new BigNumber(mantissa, exponent);
        }

        public void Normalize()
        {
            if (m_Mantissa == 0)
            {
                m_Exponent = 0;
                return;
            }

            double sign = Math.Sign(m_Mantissa);
            double abs = Math.Abs(m_Mantissa);

            while (abs >= 10.0)
            {
                abs /= 10.0;
                m_Exponent++;
            }

            while (abs < 1.0 && abs > EPSILON)
            {
                abs *= 10.0;
                m_Exponent--;
            }

            if (abs <= EPSILON)
            {
                m_Mantissa = 0;
                m_Exponent = 0;
                return;
            }

            m_Mantissa = abs * sign;
        }

        public int CompareTo(BigNumber other)
        {
            if (m_Mantissa == 0 && other.m_Mantissa == 0)
                return 0;

            bool negativeA = m_Mantissa < 0;
            bool negativeB = other.m_Mantissa < 0;

            if (negativeA != negativeB)
                return negativeA ? -1 : 1;

            if (m_Exponent != other.m_Exponent)
                return negativeA
                    ? other.m_Exponent.CompareTo(m_Exponent)
                    : m_Exponent.CompareTo(other.m_Exponent);

            return m_Mantissa.CompareTo(other.m_Mantissa);
        }

        public static BigNumber FromString(string number)
        {
            if (double.TryParse(number,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out double value))
            {
                return new BigNumber(value);
            }

            return new BigNumber(0);
        }

        public string ToFullString()
        {
            if (m_Mantissa == 0) return "0";

            double value = m_Mantissa * Math.Pow(10, m_Exponent);

            return value.ToString(
                "0.############################",
                System.Globalization.CultureInfo.InvariantCulture);
        }

        public static bool operator >(BigNumber a, BigNumber b) => a.CompareTo(b) > 0;
        public static bool operator <(BigNumber a, BigNumber b) => a.CompareTo(b) < 0;
        public static bool operator >=(BigNumber a, BigNumber b) => a.CompareTo(b) >= 0;
        public static bool operator <=(BigNumber a, BigNumber b) => a.CompareTo(b) <= 0;
        public static BigNumber operator -(BigNumber value)
        {
            return new BigNumber(-value.m_Mantissa, value.m_Exponent);
        }

        public static BigNumber operator +(BigNumber a, BigNumber b)
        {
            if (a.m_Mantissa == 0) return b;
            if (b.m_Mantissa == 0) return a;

            if (a.m_Exponent < b.m_Exponent)
            {
                var temp = a;
                a = b;
                b = temp;
            }

            int expDiff = a.m_Exponent - b.m_Exponent;

            if (expDiff > 15)
                return a;

            double scaledMantissa =
                b.m_Mantissa * Math.Pow(10, -expDiff);

            BigNumber result = new BigNumber
            {
                m_Mantissa = a.m_Mantissa + scaledMantissa,
                m_Exponent = a.m_Exponent
            };

            result.Normalize();
            return result;
        }

        public static BigNumber operator -(BigNumber a, BigNumber b)
        {
            return a + new BigNumber(-b.m_Mantissa, b.m_Exponent);
        }

        public static BigNumber operator *(BigNumber a, double b)
        {
            if (b == 0) return Zero;
            return new BigNumber(a.m_Mantissa * b, a.m_Exponent);
        }

        public static BigNumber operator *(BigNumber a, BigNumber b)
        {
            if (a.m_Mantissa == 0 || b.m_Mantissa == 0)
                return new BigNumber(0);

            double logResult = a.Log10() + b.Log10();

            bool negative =
                (a.m_Mantissa < 0) ^ (b.m_Mantissa < 0);

            return FromLog10(logResult, negative);
        }

        public static BigNumber operator /(BigNumber a, double b)
        {
            if (b == 0)
                return Zero;

            return new BigNumber(a.m_Mantissa / b, a.m_Exponent);
        }

        public static BigNumber operator /(BigNumber a, BigNumber b)
        {
            if (b.m_Mantissa == 0)
                return Zero;

            return new BigNumber(
                a.m_Mantissa / b.m_Mantissa,
                a.m_Exponent - b.m_Exponent
            );
        }

        public static bool operator ==(BigNumber a, BigNumber b)
        {
            if (a.m_Mantissa == 0 && b.m_Mantissa == 0)
                return true;

            return
                a.m_Exponent == b.m_Exponent &&
                Math.Abs(a.m_Mantissa - b.m_Mantissa) < 1e-9;
        }

        public static bool operator !=(BigNumber a, BigNumber b)
        {
            return !(a == b);
        }

        public static BigNumber operator %(BigNumber a, BigNumber b)
        {
            if (b.m_Mantissa == 0)
                throw new DivideByZeroException("BigNumber modulo by zero");

            BigNumber division = a / b;

            double flooredMantissa = Math.Floor(division.m_Mantissa);

            BigNumber floored = new BigNumber(
                flooredMantissa,
                division.m_Exponent
            );

            BigNumber result = a - (floored * b);

            return result;
        }

        public static BigNumber operator ++(BigNumber value)
        {
            return value + new BigNumber(1);
        }

        public static BigNumber operator --(BigNumber value)
        {
            return value - new BigNumber(1);
        }

        private bool IsInteger()
        {
            if (m_Mantissa == 0)
                return true;

            if (m_Exponent >= 15)
                return true;

            double scaled =
                m_Mantissa * Math.Pow(10, m_Exponent);

            return Math.Abs(scaled - Math.Round(scaled)) < 1e-9;
        }

        public double ToDouble()
        {
            if (m_Mantissa == 0)
                return 0.0;

            if (m_Exponent > 308)
                return double.PositiveInfinity;

            if (m_Exponent < -308)
                return 0.0;

            return m_Mantissa * Math.Pow(10, m_Exponent);
        }

        public float ToFloat()
        {
            if (m_Mantissa == 0)
                return 0f;

            int finalExponent = m_Exponent;

            if (finalExponent > 38)
                return m_Mantissa > 0 ? float.PositiveInfinity : float.NegativeInfinity;

            if (finalExponent < -38)
                return 0f;

            return (float)(m_Mantissa * Math.Pow(10, finalExponent));
        }

        public static BigNumber Clamp(BigNumber value, BigNumber min, BigNumber max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static BigNumber Min(BigNumber value, BigNumber min)
        {
            return value < min ? value : min;
        }

        public static BigNumber Max(BigNumber value, BigNumber max)
        {
            return value > max ? value : max;
        }

        public static BigNumber Pow(BigNumber baseValue, BigNumber power)
        {
            if (baseValue.m_Mantissa == 0)
                return new BigNumber(0);

            double baseLog =
                Math.Log10(Math.Abs(baseValue.m_Mantissa))
                + baseValue.m_Exponent;

            double powerValue = power.ToDouble();

            double resultLog = baseLog * powerValue;

            bool negative =
                baseValue.m_Mantissa < 0 &&
                Math.Abs(powerValue % 2) > 1e-9;

            return FromLog10(resultLog, negative);
        }

        public static BigNumber Abs(BigNumber value)
        {
            if (value.m_Mantissa < 0)
            {
                value.m_Mantissa = -value.m_Mantissa;
            }

            return value;
        }

        public static float Ratio(BigNumber current, BigNumber max)
        {
            if (max.m_Mantissa == 0)
                return 0f;

            if (current <= Zero)
                return 0f;

            if (current >= max)
                return 1f;

            int expDiff = current.m_Exponent - max.m_Exponent;

            if (expDiff < -20)
                return 0f;

            if (expDiff > 20)
                return 1f;

            double value =
                (current.m_Mantissa / max.m_Mantissa) *
                Math.Pow(10, expDiff);

            return Mathf.Clamp01((float)value);
        }

        public double Log10()
        {
            if (IsZero) return double.NegativeInfinity;
            return Math.Log10(Math.Abs(m_Mantissa)) + m_Exponent;
        }

        public BigNumber Floor()
        {
            if (m_Mantissa == 0)
                return this;

            if (m_Exponent >= 15)
                return this;

            double value =
                m_Mantissa * Math.Pow(10, m_Exponent);

            double floored = Math.Floor(value);

            return new BigNumber(floored);
        }

        public BigNumber Ceil()
        {
            if (m_Mantissa == 0)
                return this;

            if (m_Exponent >= 15)
                return this;

            double value =
                m_Mantissa * Math.Pow(10, m_Exponent);

            double ceiled = Math.Ceiling(value);

            return new BigNumber(ceiled);
        }

        public static BigNumber Ceil(BigNumber value)
        {
            return value.Ceil();
        }

        public static BigNumber Floor(BigNumber value)
        {
            return value.Floor();
        }

        public int Sign => Math.Sign(m_Mantissa);
        public static BigNumber Zero => new BigNumber(0);
        public bool IsZero => m_Mantissa == 0;

        public static implicit operator BigNumber(int value)
        {
            return new BigNumber((double)value);
        }

        public static implicit operator BigNumber(float value)
        {
            return new BigNumber((double)value);
        }

        public static implicit operator BigNumber(double value)
        {
            return new BigNumber(value);
        }

        private static readonly string[] SUFFIXES = { "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc" };

        private string ToAbbreviation()
        {
            if (m_CachedAbbreviation != null &&
                m_CachedMantissa == m_Mantissa &&
                m_CachedExponent == m_Exponent)
            {
                return m_CachedAbbreviation;
            }

            string result;

            if (m_Mantissa == 0)
            {
                result = "0";
            }
            else
            {
                double absMantissa = Math.Abs(m_Mantissa);
                string sign = m_Mantissa < 0 ? "-" : "";

                if (m_Exponent < 3)
                {
                    double fullValue =
                        m_Mantissa * Math.Pow(10, m_Exponent);

                    result = Math.Round(fullValue).ToString("0");
                }
                else
                {
                    int suffixIndex = m_Exponent / 3;

                    int remainder = m_Exponent % 3;

                    double shortened =
                        absMantissa * Math.Pow(10, remainder);

                    int decimals =
                        shortened >= 100 ? 0 :
                        shortened >= 10 ? 1 : 2;

                    double rounded =
                        Math.Round(shortened, decimals);

                    string number =
                        rounded.ToString("F" + decimals);

                    if (number.Contains("."))
                    {
                        number = number.TrimEnd('0');
                        number = number.TrimEnd('.');
                    }

                    if (suffixIndex >= SUFFIXES.Length)
                    {
                        result = $"{sign}{number}e{m_Exponent}";
                    }
                    else
                    {
                        result = $"{sign}{number}{SUFFIXES[suffixIndex]}";
                    }
                }
            }

            m_CachedMantissa = m_Mantissa;
            m_CachedExponent = m_Exponent;
            m_CachedAbbreviation = result;

            return result;
        }

        public string Serialize()
        {
            return m_Mantissa.ToString("R") + "|" + m_Exponent;
        }

        public static BigNumber Deserialize(string data)
        {
            if (string.IsNullOrEmpty(data))
                return Zero;

            string[] split = data.Split('|');

            double m = double.Parse(split[0]);
            int e = int.Parse(split[1]);

            return new BigNumber(m, e);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is BigNumber))
                return false;

            return this == (BigNumber)obj;
        }

        public override int GetHashCode()
        {
            return m_Mantissa.GetHashCode() ^ m_Exponent.GetHashCode();
        }

        public override string ToString()
        {
            return ToAbbreviation();
        }
    }
}