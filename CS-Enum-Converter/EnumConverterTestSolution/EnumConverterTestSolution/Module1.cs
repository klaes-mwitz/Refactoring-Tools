using Microsoft.VisualBasic.CompilerServices;

namespace EnumConverterTestSolution
{
    static class Module1
    {
        /*public static void Main(string[] args)
        {
        }*/

        public static int ALLG_SetzeBit(int psKz, int piBitwert, int piModus)
        {
            // piModus = 1 - Bit setzen
            // piModus = 0 - Bit zurücksetzen

            if (piModus == 1)
            {
                return psKz | piBitwert;
            }
            else
            {
                return psKz & ~piBitwert;
            }
        }

        public static void FuncGlob()
        {
            GlobalClass.globalVar = 0; // 0
            GlobalClass.globalVar = (EnumClass.Enu_Test)(1 + 2); // 1 + 2

            if (((int)GlobalClass.globalVar & Conversions.ToInteger(2)) > 0) // 2 > 0
            {
            }

            if (((int)GlobalClass.globalVar & Conversions.ToInteger(2 + 4)) == 0) // 2 + 4 = 0
            {
            }

            GlobalClass.Prop = (EnumClass.Enu_Test)1;
            GlobalClass.Prop = (EnumClass.Enu_Test)1 + 2;
            if (((int)GlobalClass.Prop & Conversions.ToInteger(2 + 4)) == 0) // 2 + 4 = 0
            {
            }
        }

        public static void Func1()
        {
            int i = 2;

            EnumClass.Enu_Test enuVar = 0; // 0
            EnumClass.Enu_Test enuVar2 = (EnumClass.Enu_Test)63; // 63
            EnumClass.Enu_Test enuVar3 = (EnumClass.Enu_Test)(1 + 2 + 3 + i); // 1 + 2 + 3 + i
            EnumClass.Enu_Test enuVar4 = (EnumClass.Enu_Test)(1 + 2 * i); // 1 + (2 * i)
            EnumClass.Enu_Test enuVar5 = (EnumClass.Enu_Test)(1 + 2 + 4 + 248); // 1 + 2 + 4 + 248

            enuVar = (EnumClass.Enu_Test)2; // 2
            enuVar = (EnumClass.Enu_Test)(2 + 4); // 2 + 4
            enuVar = (enuVar + 2 + 4 + 8); // 2 + 4 + 8
            enuVar = (EnumClass.Enu_Test)((int)enuVar | 2); // 2
            enuVar = (EnumClass.Enu_Test)((int)enuVar | 248); // 248
            enuVar = (EnumClass.Enu_Test)((int)enuVar | 2 + 4); // 2 + 4
            enuVar = (EnumClass.Enu_Test)((int)enuVar | 2 | 4 | 8); // 2 Or 4 Or 8
            enuVar = (EnumClass.Enu_Test)((int)enuVar | 2 | 4 + 8); // 2 Or 4 + 8
            enuVar = (EnumClass.Enu_Test)((int)enuVar | 2 | 3); // 2 Or 3
            enuVar += 3; // 3
            enuVar = (EnumClass.Enu_Test)((int)enuVar + 3 + 2); // 3 + 2

            enuVar = (EnumClass.Enu_Test)(i * 2); // i * 2

            enuVar = (EnumClass.Enu_Test)(2 + 4 * 8); // 2 + 4 * 8
            enuVar = (EnumClass.Enu_Test)(2 + 4 * i + 8); // 2 + 4 * i + 8
            enuVar = (EnumClass.Enu_Test)((2 + 4) * 8); // (2 + 4) * 8

            enuVar = (EnumClass.Enu_Test)((int)enuVar & ~2); // Not 2
            enuVar = (EnumClass.Enu_Test)((int)enuVar & ~249); // Not 249
            enuVar = (EnumClass.Enu_Test)((int)enuVar & ~(2 + 4)); // Not (2 + 4)

            enuVar = (EnumClass.Enu_Test)ALLG_SetzeBit((int)enuVar, 2, 1); // 2
            enuVar = (EnumClass.Enu_Test)ALLG_SetzeBit((int)enuVar, 7, 1); // 7
            enuVar = (EnumClass.Enu_Test)ALLG_SetzeBit((int)enuVar, 2 + 4, 1); // 2 + 4
        }

        public static void Func2(EnumClass.Enu_Test enuArg1 = 0, EnumClass.Enu_Test enuArg2 = (EnumClass.Enu_Test)2 + 4)
        {
            int i = 2;
            EnumClass.Enu_Test enuVal = 0;

            if (i > 2) // i > 2
            {
            }

            if (enuVal > (EnumClass.Enu_Test)2)
            {
            }
            else if (enuVal > (EnumClass.Enu_Test)2 + 4 * 2)
            {
            }
            else
            {
            }

            if (i > 2 && (int)enuVal == 2 && (int)enuArg2 > 2 + 4) // i > 2 AndAlso enuVal = 2 AndAlso enuArg2 > 2 + 4
            {
            }

            if ((int)enuVal == 2) // 2
            {
            }

            if ((int)enuVal == 2 + 4) // 2 + 4
            {
            }

            if ((int)enuVal == 2 + 4 + i) // 2 + 4 + i
            {
            }

            if ((int)enuVal != 2) // 2
            {
            }

            if ((int)enuVal != 2 + 4) // 2 + 4
            {
            }

            if (((int)enuVal & 2) > 0) // 2 > 0
            {
            }

            if (((int)enuVal & 2) == 0) // 2 = 0
            {
            }

            if (((int)enuVal & ~2) == 0) // 2 = 0
            {
            }

            if (Conversions.ToBoolean(((int)enuVal & ~(2 + 4)) == 6) && 1 < 2 && ((int)enuVal & ~3) > 4) // (enuVal And Not (2 + 4) = 6) AndAlso 1 < 2 AndAlso (enuVal And Not 3) > 4
            {
            }

            if (((int)enuArg1 & 248) > 0) // 248 > 0
            {
            }

            if (((int)enuArg1 & 248) == 0) // 248 = 0
            {
            }

            if (((int)enuArg1 & 4) == 4) // 4 = 4
            {
            }

            if (((int)enuArg1 & 1 << 2) > 0) // 1 << 2 > 0
            {
            }

            if (((int)enuArg1 & 1 << 2) > 1 << 2) // 1 << 2 > 1 << 2
            {
            }

            if (((int)enuArg1 & 1) == 0 && ((int)enuArg1 & 2) == 0) // enuArg1 And 1 = 0 AndAlso enuArg1 And 2 = 0
            {
            }

            if (((int)enuArg1 & 1) > 0 && ((int)enuArg1 & 2) > 0) // enuArg1 And 1 > 0 AndAlso enuArg1 And 2 > 0
            {
            }

            if (((int)enuArg1 & 1) > 0 && ((int)enuArg1 & 2) == 0) // enuArg1 And 1 > 0 AndAlso enuArg1 And 2 = 0
            {
            }

            Func3((EnumClass.Enu_Test)1 + 2, 2, (EnumClass.Enu_Test)7, 4); // 1 + 2, 2, 0, 4

            switch (enuVal)
            {
                case (EnumClass.Enu_Test)1: break;
                case (EnumClass.Enu_Test)2: break;
                case (EnumClass.Enu_Test)3: break;
                case (EnumClass.Enu_Test)(2 + 4): break;
            }
        }

        public static void Func3(EnumClass.Enu_Test arg1, short arg2, EnumClass.Enu_Test enuArg, int arg4)
        {
            enuArg = 0; // 0
            enuArg = (EnumClass.Enu_Test)2; // 2
            enuArg = (EnumClass.Enu_Test)(1 + 2 + 4 + 248); // 1 + 2 + 4 + 248
        }

        public static EnumClass.Enu_Test Func4()
        {
            EnumClass.Enu_Test Func4Ret = default;
            int i = 0;

            Func4Ret = (EnumClass.Enu_Test)3; // 3
            Func4Ret = (EnumClass.Enu_Test)(2 + 4); // 2 + 4
            Func4Ret = (EnumClass.Enu_Test)31; // 31
            Func4Ret = (EnumClass.Enu_Test)(i + 2); // i + 2

            Func3((EnumClass.Enu_Test)1, 2, 0, 4); // 1, 2, 0, 4
            Func3((EnumClass.Enu_Test)2, 2, (EnumClass.Enu_Test)2, 4); // 2, 2, 2, 4
            Func3((EnumClass.Enu_Test)3, 2, (EnumClass.Enu_Test)(1 + 2), 4); // 3, 2, 1 + 2, 4
            Func3((EnumClass.Enu_Test)4, 2, (EnumClass.Enu_Test)(31 + 128), 4); // 4, 2, 31 + 128, 4
            Func3((EnumClass.Enu_Test)5, 2, (EnumClass.Enu_Test)(2 | 4), 4); // 5, 2, 2 Or 4, 4
            Func3((EnumClass.Enu_Test)6, 2, (EnumClass.Enu_Test)(2 * 4 + 8), 4); // 6, 2, 2 * 4 + 8, 4
            Func3((EnumClass.Enu_Test)7, 2, (EnumClass.Enu_Test)(2 * i + 8), 4); // 7, 2, 2 * i + 8, 4

            return (EnumClass.Enu_Test)3; // 3
            return (EnumClass.Enu_Test)(2 + 4); // 2 + 4
            return (EnumClass.Enu_Test)31; // 31
            return (EnumClass.Enu_Test)(i + 2); // i + 2
        }
    }
}