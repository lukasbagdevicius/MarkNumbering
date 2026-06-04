using System;
using System.Collections.Generic;
using System.Linq;

namespace MarkNumbering
{
    /// <summary>
    /// Gryna numeravimo logika (be Revit priklausomybių – kad būtų galima testuoti atskirai).
    /// </summary>
    public static class Numbering
    {
        /// <summary>
        /// Kiekvienam ilgiui priskiria numerį nuo 1, didėjančia ilgio tvarka:
        /// trumpiausia ilgio grupė gauna 1, kita – 2 ir t. t.
        /// Jokio apvalinimo – lyginamos tikslios reikšmės.
        /// Du ilgiai patenka į tą pačią grupę tik tada, kai (surūšiavus) tarp gretimų
        /// reikšmių skirtumas yra MAŽESNIS už toleranciją. Jei skirtumas yra bent
        /// lygus tolerancijai (pvz., 0.1 mm) – priskiriamas kitas numeris.
        /// </summary>
        /// <param name="lengths">Ilgiai (bet kokiais vienetais, svarbu kad tolerancija būtų tais pačiais).</param>
        /// <param name="tolerance">Riba, nuo kurios ilgiai laikomi skirtingais.</param>
        /// <returns>Masyvas su numeriu kiekvienam įvesties elementui (ta pačia tvarka).</returns>
        public static int[] NumberByLength(IReadOnlyList<double> lengths, double tolerance)
        {
            int n = lengths.Count;
            var result = new int[n];
            if (n == 0) return result;

            int[] order = Enumerable.Range(0, n).OrderBy(i => lengths[i]).ToArray();

            int number = 0;
            double previous = double.NaN;
            foreach (int i in order)
            {
                if (number == 0 || lengths[i] - previous >= tolerance)
                    number++;
                previous = lengths[i];
                result[i] = number;
            }
            return result;
        }

        /// <summary>
        /// Nuima skaitmenis nuo eilutės galo: "STG12" -> "STG", "STG" -> "STG", "123" -> "".
        /// Naudojama, kad įrankį būtų galima saugiai paleisti pakartotinai.
        /// </summary>
        public static string StripTrailingDigits(string mark)
        {
            if (string.IsNullOrEmpty(mark)) return mark ?? string.Empty;
            int end = mark.Length;
            while (end > 0 && char.IsDigit(mark[end - 1])) end--;
            return mark.Substring(0, end);
        }
    }
}
