using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace RTLSDR.Common
{
    public class AmpCalculation
    {
        /// Computing peek amplitude
        ///  (I²+Q²)½
        /// </summary>
        /// <param name="I"></param>
        /// <param name="Q"></param>
        /// <returns></returns>
        public static double GetAmplitude(int I, int Q)
        {
            if (I == 0 && Q == 0) return 0;
            return Math.Sqrt(I * I + Q * Q);
        }

        /// <summary>
        /// Computing phase angle
        /// ϕ = tan⁻¹(Q/I)
        /// </summary>
        /// <returns></returns>
        public static double GetPhaseAngle(int I, int Q)
        {
            if (I == 0)
                return 0;

            return Math.Atan(Q / I);
        }
    }
}
