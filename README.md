# Mark numeravimas pagal ilgį — Revit 2026 įskiepis

Įskiepis automatiškai sunumeruoja vienodą **Mark** turinčius elementus pagal jų **Cut Length** parametrą.

Pavyzdys: pažymėtos lentos su Mark `STG` →
- 1000.0 mm ilgio lentos gauna `STG1`
- 1200.0 mm ilgio lentos gauna `STG2`
- 2400.0 mm ilgio lentos gauna `STG3`

Numeris didėja ilgėjant elementui. Vienodo ilgio elementai gauna tą patį numerį.

## Diegimas

1. Paleiskite `install.bat` (dukart spustelėkite). Jis nukopijuos `dist\MarkNumbering.dll` ir `MarkNumbering.addin` į `%APPDATA%\Autodesk\Revit\Addins\2026`.
2. Paleiskite Revit 2026. Pirmą kartą pasirodžius saugumo klausimui spauskite **Always Load**.

Jei nenorite naudoti `install.bat`, abu failus galite nukopijuoti į tą aplanką rankiniu būdu.

## Naudojimas

1. Modelyje **pažymėkite elementus**, kuriuos norite sunumeruoti (pvz., visas lentas su Mark `STG`). Galima žymėti kelias Mark grupes iš karto — kiekviena numeruojama atskirai.
2. Paleiskite: **Add-Ins → External Tools → Mark numeravimas pagal ilgį**.
3. Atsidarys patvirtinimo langas su numeravimo planu (kiek grupių, kokie ilgiai, kiek elementų). Spauskite **Yes**.
4. Baigus parodoma ataskaita. Jei rezultatas netinka — **Ctrl+Z** viską atšaukia.

## Veikimo taisyklės

- **Ilgis** imamas iš `Cut Length` parametro pilnu vidiniu Revit tikslumu — **jokio apvalinimo**.
- Du ilgiai laikomi **vienodais**, jei skiriasi mažiau nei **0.1 mm**. Jei skiriasi bent 0.1 mm — priskiriamas kitas numeris.
- Numeravimas vyksta kiekvienai Mark grupei atskirai, **didėjančia ilgio tvarka** (trumpiausi = 1).
- Prieš numeruojant nuo Mark galo **nuimami esami skaičiai** (`STG1` → `STG`), todėl įrankį saugu leisti pakartotinai. Dėl to bazinis Mark neturėtų pats baigtis skaičiumi (pvz., `S1`).
- Praleidžiami: elementai be Mark reikšmės, elementai be `Cut Length` parametro (pvz., durys, sienos) ir elementai, kurių Mark sudarytas vien iš skaičių. Praleistų skaičius parodomas lange.
- Revit įspėjimai apie pasikartojančias Mark reikšmes komandos metu nuslopinami automatiškai.

## Smulkmena dėl 0.1 mm taisyklės

Grupavimas vyksta lyginant gretimus surūšiuotus ilgius. Jei modelyje būtų ilgių „grandinėlė" mažesniais nei 0.1 mm žingsniais (pvz., 1000.00 / 1000.05 / 1000.09), visa grandinėlė gautų vieną numerį. Realiems gaminiams, kur ilgių skirtumai ≥ 0.1 mm, tai įtakos neturi — taisyklė veikia tiksliai taip, kaip aprašyta.

## Failai

| Failas | Paskirtis |
|---|---|
| `dist/MarkNumbering.dll` | Sukompiliuotas įskiepis |
| `MarkNumbering.addin` | Revit manifesto failas |
| `install.bat` | Diegimas vienu paspaudimu |
| `src/` | Atvirasis kodas (C#) |

## Kodo keitimas (nebūtina)

Norint ką nors pakeisti (pvz., toleranciją — `ToleranceMm` konstanta `src/Command.cs` viršuje), reikia perkompiliuoti:

1. Įdiekite [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
2. Komandinėje eilutėje `src` aplanke paleiskite: `dotnet build -c Release`
3. Naują DLL rasite `src/bin/Release/MarkNumbering.dll` — nukopijuokite jį į `dist` ir paleiskite `install.bat`.

## Licencija

MIT (atvirasis kodas) — galite laisvai naudoti, keisti ir dalintis.
