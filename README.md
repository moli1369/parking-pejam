# Parking Pejam · mpas pejam

Windows desktop app for **live parking-spot status** (Pejam site).

Built with **C# / WinForms** · .NET Framework 4.7.2

---

## Features

| Feature | Description |
|:--------|:------------|
| **Per-spot status** | Each parking panel is free (green) or occupied (red) independently |
| **Click to toggle** | Click a spot to switch free ↔ occupied |
| **Persistent state** | Status is saved under `%LocalAppData%\parking-pejam\spots.txt` and restored on next start |
| **Live counters** | Window title shows Free / Occupied / Total |
| **Tooltips** | Hover a spot to see its panel id |

---

## How to run

1. Open `mpas pejam.sln` in **Visual Studio 2019/2022**
2. Restore / build (**Build → Build Solution**)
3. Run (**F5**)

Requirements: Windows + .NET Framework 4.7.2 (usually already installed on modern Windows).

---

## Color legend

| Color | Meaning |
|:------|:--------|
| 🟢 Dark green | Free |
| 🔴 Red | Occupied |

---

## Project structure

```
parking-pejam/
├── Form1.cs              # Business logic (state, click, save/load)
├── Form1.Designer.cs     # UI layout (parking map panels)
├── Program.cs            # Entry point
├── mpas pejam.csproj
└── mpas pejam.sln
```

State file example:

```
panel1=1
panel2=0
panel3=1
```

`1` = occupied · `0` = free

---

## Deutsch (Kurz)

Desktop-Anwendung zur **Anzeige und Verwaltung von Parkplätzen** (Standort Pejam).

- Grün = frei · Rot = belegt  
- Klick wechselt den Status  
- Status wird lokal gespeichert und beim nächsten Start geladen  

## فارسی (خلاصه)

نرم‌افزار دسکتاپ برای **مانیتورینگ جای پارک** (سایت پژم).

- سبز = آزاد · قرمز = اشغال  
- با کلیک وضعیت عوض می‌شود  
- وضعیت در فایل محلی ذخیره و در اجرای بعدی بازیابی می‌شود  

---

## Author

**Mohammad Askari Dehestani** · [GitHub](https://github.com/moli1369)

Related portfolio: [nachweise](https://github.com/moli1369/nachweise)
