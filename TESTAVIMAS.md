# Testavimo instrukcija

Įskiepis dar **nebuvo testuotas realiame Revit** (kūrėjo aplinkoje Revit nėra). Numeravimo logika patikrinta 16 automatinių testų, bet prieš naudojant darbe būtina patikrinti realiame modelyje.

**Testuokite kopijoje arba bandomajame projekte, ne darbiniame faile.** Bet kuriuo atveju po komandos viską atšaukia Ctrl+Z.

## Diegimas

1. Dukart spustelėkite `install.bat` (nukopijuos `dist\MarkNumbering.dll` ir `MarkNumbering.addin` į `%APPDATA%\Autodesk\Revit\Addins\2026`).
2. Paleiskite Revit 2026 → saugumo lange spauskite **Always Load**.
3. Komanda: **Add-Ins → External Tools → Mark numeravimas pagal ilgį**.

## Patikros sąrašas

| # | Testas | Laukiamas rezultatas |
|---|--------|----------------------|
| 1 | Pažymėkite kelias sijas/lentas su Mark `STG` ir skirtingais Cut Length, paleiskite komandą | Patvirtinimo langas rodo teisingą grupių skaičių ir ilgius; po Yes trumpiausios gauna `STG1`, ilgesnės `STG2`, `STG3`... |
| 2 | Tarp pažymėtų — kelios visiškai vienodos lentos | Vienodos gauna tą patį numerį |
| 3 | Paleiskite komandą antrą kartą ant tų pačių elementų | Rezultatas nepasikeičia (STG1 lieka STG1), langas rodo „jau buvo teisingi" |
| 4 | Pažymėkite dvi Mark grupes iš karto (pvz., `STG` ir `KOL`) | Kiekviena grupė numeruojama atskirai: STG1..., KOL1... |
| 5 | Tarp pažymėtų — elementas be Cut Length (pvz., durys) ir be Mark | Jie praleidžiami, langas parodo praleistų skaičių |
| 6 | Sukurkite dvi lentas, kurių ilgis skiriasi ~0.5 mm | Jos gauna skirtingus numerius (riba — 0.1 mm) |
| 7 | Nieko nepažymėjus paleiskite komandą | Pranešimas, kad pirmiausia reikia pažymėti elementus |
| 8 | Ctrl+Z po komandos | Visi Mark grįžta į pradinę būseną |

## Jei kažkas neveikia

- Užsirašykite tikslų klaidos tekstą (ekrano nuotrauka tinka) ir prie kokių elementų tai nutiko.
- Dažniausia priežastis: elementai neturi `Cut Length` parametro (jį turi Structural Framing kategorijos elementai).
- Toleranciją (0.1 mm) galima pakeisti `src/Command.cs` faile (`ToleranceMm` konstanta) — žr. README skyrių „Kodo keitimas".
