using Soneta.Business;
using System;
using System.Collections.Generic;
using System.Linq;
using Soneta.Kadry;
using Soneta.KadryPlace;
using Soneta.Types;
using Rekrutacja.Workers.Template;

[assembly: Worker(typeof(CustomAreaCalculatorWorker), typeof(Pracownicy))]

namespace Rekrutacja.Workers.Template
{
    public static class CustomParser
    {
        public static int ParseStringToInt(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                throw new ArgumentException("Input cannot be null or empty.");
            }

            int result = 0;
            bool isNegative = false;
            int startIndex = 0;

            if (input[0] == '-')
            {
                isNegative = true;
                startIndex = 1;
            }

            for (int i = startIndex; i < input.Length; i++)
            {
                char c = input[i];
                if (c < '0' || c > '9')
                {
                    throw new ArgumentException($"Invalid character '{c}' in input string '{input}'.");
                }

                result = result * 10 + (c - '0');
            }

            return isNegative ? -result : result;
        }
    }

    public class CustomAreaCalculatorWorker
    {
        public class CustomAreaCalculatorWorkerParams : ContextBase
        {
            [Caption("Zmienna A (r dla koła)")]
            public string ZmiennaA { get; set; }

            [Caption("Zmienna B (h dla trójkąta)")]
            public string ZmiennaB { get; set; }

            [Caption("Figura")]
            public ShapeType Figura { get; set; }

            [Caption("Data obliczeń")]
            public Date DataObliczen { get; set; }

            public CustomAreaCalculatorWorkerParams(Context context) : base(context)
            {
                this.DataObliczen = Date.Today;
            }
        }

        [Context]
        public Context Cx { get; set; }

        [Context]
        public CustomAreaCalculatorWorkerParams Parametry { get; set; }

        [Action("Oblicz pole figury (String to Int Parser)",
                Description = "Kalkulator pola powierzchni figur z własnym parserem string na int",
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

            // Logowanie wartości wejściowych
            Console.WriteLine($"ZmiennaA: {this.Parametry.ZmiennaA}, ZmiennaB: {this.Parametry.ZmiennaB}");

            int zmiennaA;
            int zmiennaB;

            try
            {
                zmiennaA = CustomParser.ParseStringToInt(this.Parametry.ZmiennaA);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException($"Error parsing ZmiennaA: {ex.Message}");
            }

            try
            {
                zmiennaB = CustomParser.ParseStringToInt(this.Parametry.ZmiennaB);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException($"Error parsing ZmiennaB: {ex.Message}");
            }

            int wynik = 0;
            switch (this.Parametry.Figura)
            {
                case ShapeType.Kwadrat:
                    wynik = zmiennaA * zmiennaA;
                    break;
                case ShapeType.Prostokat:
                    wynik = zmiennaA * zmiennaB;
                    break;
                case ShapeType.Trojkat:
                    wynik = (int)(0.5 * zmiennaA * zmiennaB);
                    break;
                case ShapeType.Kolo:
                    wynik = (int)(Math.PI * zmiennaA * zmiennaA);
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
