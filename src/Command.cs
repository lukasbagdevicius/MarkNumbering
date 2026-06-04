using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace MarkNumbering
{
    /// <summary>
    /// Sunumeruoja vienodą Mark turinčius pažymėtus elementus pagal Cut Length parametrą:
    /// trumpiausia ilgio grupė gauna 1, kita – 2 ir t. t. (STG -> STG1, STG2, ...).
    /// Vienodo ilgio (skirtumas mažesnis nei 0.1 mm) elementai gauna tą patį numerį.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class NumberMarksCommand : IExternalCommand
    {
        /// <summary>Tolerancija milimetrais: ilgiai, besiskiriantys bent tiek, laikomi skirtingais.</summary>
        private const double ToleranceMm = 0.1;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                return Run(commandData, ref message);
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Mark numeravimas - klaida", "Įvyko nenumatyta klaida:\n" + ex.Message);
                return Result.Failed;
            }
        }

        private static Result Run(ExternalCommandData commandData, ref string message)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            if (uidoc == null)
            {
                message = "Nėra atidaryto dokumento.";
                return Result.Failed;
            }

            Document doc = uidoc.Document;
            if (doc.IsReadOnly)
            {
                message = "Dokumentas atidarytas tik skaitymui.";
                return Result.Failed;
            }

            double toleranceFt = UnitUtils.ConvertToInternalUnits(ToleranceMm, UnitTypeId.Millimeters);

            // 1. Tik pažymėti elementai
            ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
            if (selectedIds == null || selectedIds.Count == 0)
            {
                TaskDialog.Show("Mark numeravimas",
                    "Pirmiausia pažymėkite elementus, kuriuos norite sunumeruoti, ir paleiskite komandą dar kartą.");
                return Result.Cancelled;
            }

            // 2. Tinkamų elementų atranka
            var entries = new List<MarkEntry>();
            int noMark = 0;          // be Mark reikšmės
            int onlyDigitsMark = 0;  // Mark vien iš skaičių (nuėmus skaičius nieko neliktų)
            int noCutLength = 0;     // turi Mark, bet neturi Cut Length

            foreach (ElementId id in selectedIds)
            {
                Element el = doc.GetElement(id);
                if (el == null || el is ElementType) continue;

                Parameter markParam = el.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
                if (markParam == null || markParam.IsReadOnly) { noMark++; continue; }

                string mark = markParam.AsString();
                if (string.IsNullOrWhiteSpace(mark)) { noMark++; continue; }

                // Nuimame skaičius nuo galo, kad įrankį būtų galima leisti pakartotinai (STG1 -> STG)
                string baseMark = Numbering.StripTrailingDigits(mark.Trim());
                if (string.IsNullOrWhiteSpace(baseMark)) { onlyDigitsMark++; continue; }

                double? cutLength = GetCutLength(el);
                if (!cutLength.HasValue) { noCutLength++; continue; }

                entries.Add(new MarkEntry(el.Id, baseMark, cutLength.Value));
            }

            if (entries.Count == 0)
            {
                TaskDialog.Show("Mark numeravimas",
                    "Tarp pažymėtų elementų nerasta nė vieno, kuris turėtų ir užpildytą Mark, ir Cut Length parametrą.\n\n"
                    + "Praleista: be Mark - " + noMark
                    + ", Mark tik iš skaičių - " + onlyDigitsMark
                    + ", be Cut Length - " + noCutLength + ".");
                return Result.Cancelled;
            }

            // 3. Numeravimo planas: grupuojame pagal bazinį Mark, numeruojame pagal ilgį
            var assignments = new List<Assignment>();
            var reportLines = new List<string>();
            int lengthGroupsTotal = 0;

            foreach (var group in entries
                         .GroupBy(e => e.BaseMark)
                         .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
            {
                List<MarkEntry> list = group.ToList();
                double[] lengths = list.Select(e => e.CutLength).ToArray();
                int[] numbers = Numbering.NumberByLength(lengths, toleranceFt);

                for (int i = 0; i < list.Count; i++)
                {
                    string newMark = group.Key + numbers[i].ToString(CultureInfo.InvariantCulture);
                    assignments.Add(new Assignment(list[i].Id, newMark));
                }

                int clusterCount = numbers.Max();
                lengthGroupsTotal += clusterCount;
                reportLines.Add(group.Key + ": ilgio grupių - " + clusterCount + ", elementų - " + list.Count);

                for (int n = 1; n <= clusterCount; n++)
                {
                    var lens = new List<double>();
                    for (int i = 0; i < list.Count; i++)
                        if (numbers[i] == n) lens.Add(lengths[i]);

                    double minMm = UnitUtils.ConvertFromInternalUnits(lens.Min(), UnitTypeId.Millimeters);
                    double maxMm = UnitUtils.ConvertFromInternalUnits(lens.Max(), UnitTypeId.Millimeters);
                    string lenText = (maxMm - minMm) < 0.0005
                        ? minMm.ToString("F2", CultureInfo.InvariantCulture) + " mm"
                        : minMm.ToString("F3", CultureInfo.InvariantCulture) + " - "
                          + maxMm.ToString("F3", CultureInfo.InvariantCulture) + " mm";

                    reportLines.Add("      " + group.Key + n + "   |   " + lenText + "   |   " + lens.Count + " vnt.");
                }
            }

            // 4. Patvirtinimas prieš įrašant
            string skippedInfo = "";
            if (noMark + onlyDigitsMark + noCutLength > 0)
            {
                skippedInfo = "\nPraleista: be Mark - " + noMark
                              + ", Mark tik iš skaičių - " + onlyDigitsMark
                              + ", be Cut Length - " + noCutLength + ".";
            }

            int baseMarkCount = entries.Select(e => e.BaseMark).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            var confirm = new TaskDialog("Mark numeravimas")
            {
                MainInstruction = "Mark grupių: " + baseMarkCount
                                  + ", ilgio grupių: " + lengthGroupsTotal
                                  + ", elementų: " + entries.Count + ". Priskirti numerius?",
                MainContent = "Pažymėta elementų: " + selectedIds.Count + "." + skippedInfo
                              + "\n\nIšskleiskite (Show details), kad pamatytumėte numeravimo planą.",
                ExpandedContent = BuildPreview(reportLines),
                CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No,
                DefaultButton = TaskDialogResult.Yes,
                TitleAutoPrefix = false
            };
            if (confirm.Show() != TaskDialogResult.Yes)
                return Result.Cancelled;

            // 5. Įrašymas vienoje transakcijoje
            int changed = 0, alreadyOk = 0, failed = 0;
            using (var t = new Transaction(doc, "Mark numeravimas pagal Cut Length"))
            {
                FailureHandlingOptions fho = t.GetFailureHandlingOptions();
                fho.SetFailuresPreprocessor(new WarningSwallower());
                t.SetFailureHandlingOptions(fho);

                t.Start();
                foreach (Assignment a in assignments)
                {
                    try
                    {
                        Element el = doc.GetElement(a.Id);
                        Parameter p = el?.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
                        if (p == null || p.IsReadOnly) { failed++; continue; }

                        if (string.Equals(p.AsString(), a.NewMark, StringComparison.Ordinal))
                        {
                            alreadyOk++;
                        }
                        else if (p.Set(a.NewMark))
                        {
                            changed++;
                        }
                        else
                        {
                            failed++;
                        }
                    }
                    catch
                    {
                        failed++;
                    }
                }
                t.Commit();
            }

            // 6. Rezultatas
            var done = new TaskDialog("Mark numeravimas - atlikta")
            {
                MainInstruction = "Pakeista: " + changed + ", jau buvo teisingi: " + alreadyOk
                                  + (failed > 0 ? ", nepavyko: " + failed : "") + ".",
                MainContent = "Išskleiskite (Show details), kad pamatytumėte priskirtus numerius.",
                ExpandedContent = BuildPreview(reportLines),
                TitleAutoPrefix = false
            };
            done.Show();

            return Result.Succeeded;
        }

        /// <summary>Sutrumpina peržiūrą, jei eilučių labai daug.</summary>
        private static string BuildPreview(List<string> lines)
        {
            const int maxLines = 400;
            if (lines.Count <= maxLines) return string.Join("\n", lines);
            return string.Join("\n", lines.Take(maxLines))
                   + "\n... (dar " + (lines.Count - maxLines) + " eilučių)";
        }

        /// <summary>
        /// Grąžina Cut Length vidiniais Revit vienetais (pėdomis) be jokio apvalinimo, arba null.
        /// Pirmiausia bandomas built-in parametras (nepriklauso nuo kalbos), tada ieškoma pagal pavadinimą.
        /// </summary>
        private static double? GetCutLength(Element el)
        {
            Parameter p = el.get_Parameter(BuiltInParameter.STRUCTURAL_FRAME_CUT_LENGTH);
            if (!IsUsableLength(p)) p = el.LookupParameter("Cut Length");
            if (!IsUsableLength(p)) return null;
            return p.AsDouble();
        }

        private static bool IsUsableLength(Parameter p)
        {
            return p != null && p.StorageType == StorageType.Double && p.HasValue;
        }

        private readonly struct MarkEntry
        {
            public MarkEntry(ElementId id, string baseMark, double cutLength)
            {
                Id = id;
                BaseMark = baseMark;
                CutLength = cutLength;
            }

            public ElementId Id { get; }
            public string BaseMark { get; }
            public double CutLength { get; }
        }

        private readonly struct Assignment
        {
            public Assignment(ElementId id, string newMark)
            {
                Id = id;
                NewMark = newMark;
            }

            public ElementId Id { get; }
            public string NewMark { get; }
        }
    }

    /// <summary>
    /// Transakcijos metu automatiškai nuryja įspėjimus (pvz., apie pasikartojančias Mark reikšmes),
    /// kad vartotojui nereikėtų spaudinėti daugybės pranešimų.
    /// </summary>
    public class WarningSwallower : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor accessor)
        {
            foreach (FailureMessageAccessor f in accessor.GetFailureMessages())
            {
                if (f.GetSeverity() == FailureSeverity.Warning)
                    accessor.DeleteWarning(f);
            }
            return FailureProcessingResult.Continue;
        }
    }
}
