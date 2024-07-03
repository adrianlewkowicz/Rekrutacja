using Soneta.Business;
using System;
using System.Collections.Generic;
using System.Linq;
using Soneta.Kadry;
using Soneta.KadryPlace;
using Soneta.Types;
using Rekrutacja.Workers.Template;

[assembly: Worker(typeof(FiguraWorker), typeof(Pracownicy))]

namespace Rekrutacja.Workers.Template
{
    public enum Figura
    {
        Kwadrat,
        Prostokat,
        Trojkat,
        Kolo
    }

    public class FiguraWorker
    {
        public class FiguraWorkerParametry : ContextBase
        {
            [Caption("Zmienna A (r dla koła)")]
            public double ZmiennaA { get; set; }

            [Caption("Zmienna B (h dla trójkąta)")]
            public double ZmiennaB { get; set; }

            [Caption("Figura")]
            public Figura Figura { get; set; }

            [Caption("Data obliczeń")]
            public Date DataObliczen { get; set; }

            public FiguraWorkerParametry(Context context) : base(context)
            {
                this.DataObliczen = Date.Today;
            }
        }

        [Context]
        public Context Cx { get; set; }

        [Context]
        public FiguraWorkerParametry Parametry { get; set; }

        [Action("Oblicz pole figury",
                Description = "Kalkulator pola powierzchni figur",
                Priority = 10,
                Mode = ActionMode.ReadOnlySession,
                Icon = ActionIcon.Accept,
                Target = ActionTarget.ToolbarWithText)]
        public void WykonajAkcje()
        {
            // Pobieranie danych z Contextu
            List<Pracownik> pracownicy = new List<Pracownik>();
            if (this.Cx.Contains(typeof(Pracownik[])))
            {
                pracownicy = ((Pracownik[])this.Cx[typeof(Pracownik[])]).ToList();
            }
            else if (this.Cx.Contains(typeof(Pracownik)))
            {
                pracownicy.Add((Pracownik)this.Cx[typeof(Pracownik)]);
            }

            int wynik = 0;
            switch (this.Parametry.Figura)
            {
                case Figura.Kwadrat:
                    wynik = (int)(this.Parametry.ZmiennaA * this.Parametry.ZmiennaA);
                    break;
                case Figura.Prostokat:
                    wynik = (int)(this.Parametry.ZmiennaA * this.Parametry.ZmiennaB);
                    break;
                case Figura.Trojkat:
                    wynik = (int)(0.5 * this.Parametry.ZmiennaA * this.Parametry.ZmiennaB);
                    break;
                case Figura.Kolo:
                    wynik = (int)(Math.PI * this.Parametry.ZmiennaA * this.Parametry.ZmiennaA);
                    break;
                default:
                    throw new InvalidOperationException("Nieznana figura.");
            }

            // Modyfikacja danych
            using (Session nowaSesja = this.Cx.Login.CreateSession(false, false, "ModyfikacjaPracownika"))
            {
                using (ITransaction trans = nowaSesja.Logout(true))
                {
                    foreach (var pracownik in pracownicy)
                    {
                        var pracownikZSesja = nowaSesja.Get(pracownik);
                        pracownikZSesja.Features["Wynik"] = (double)wynik;  // Konwersja int na double
                        pracownikZSesja.Features["DataObliczen"] = this.Parametry.DataObliczen;
                    }

                    trans.CommitUI();
                }
                nowaSesja.Save();
            }
        }
    }
}
