using VectorRagDemo.Models.Entities;

namespace VectorRagDemo.Models.ViewModels
{
    public class StatistiekenViewModel
    {
        public Project Project { get; set; } = null!;

        /// <summary>True when ExtraCommunicationEnabled and at least one channel is active.</summary>
        public bool HeeftEscalatie { get; set; }

        // ── Tier 1: Basis ────────────────────────────────────────────────────
        public int TotaalGesprekken { get; set; }
        public int GesprekkendezeMaand { get; set; }
        public int GesprekkenVorigeMaand { get; set; }
        public int GesprekkenDezeWeek { get; set; }
        public int GesprekkenVorigeWeek { get; set; }
        public int TotaalBerichten { get; set; }

        /// <summary>Aantal dagen in de afgelopen 30 dagen waarop minstens 1 gesprek plaatsvond.</summary>
        public int ActieveDagen { get; set; }

        /// <summary>Per-dag telling voor de afgelopen 30 dagen (incl. dagen met 0 gesprekken).</summary>
        public List<DagTelling> GesprekkenPerDag { get; set; } = new();

        // ── Tier 2: Uitgebreid (aanvullend op Basis) ─────────────────────────
        public double GemiddeldBerichtenPerGesprek { get; set; }
        public double GemiddeldeDuurMinuten { get; set; }

        /// <summary>Index = uur (0–23), waarde = aantal gesprekken gestart in dat uur (laatste 90 d).</summary>
        public int[] GesprekkenPerUur { get; set; } = new int[24];

        /// <summary>Index 0–6 = ma–zo (ISO), waarde = aantal gesprekken (laatste 90 d).</summary>
        public int[] GesprekkenPerDagVanDeWeek { get; set; } = new int[7];

        public int TotaalGebruikerBerichten { get; set; }
        public int TotaalBotBerichten { get; set; }

        /// <summary>Gesprekken per maand voor de laatste 6 maanden (index 0 = oudste).</summary>
        public int[] GesprekkenPerMaand { get; set; } = new int[6];
        public string[] GesprekkenPerMaandLabels { get; set; } = new string[6];

        /// <summary>Berichten per dag voor de afgelopen 30 dagen.</summary>
        public List<DagTelling> BerichtenPerDag { get; set; } = new();

        /// <summary>Gesprekken met ≥ 5 berichten (indicatie van betrokkenheid).</summary>
        public int BetrokkenGesprekken { get; set; }
        public double BetrokkenGesprekkenPercent { get; set; }

        /// <summary>Het uur van de dag (0–23) met de meeste gesprekken. -1 als er geen data is.</summary>
        public int PiekUur => GesprekkenPerUur.Sum() == 0 ? -1 : Array.IndexOf(GesprekkenPerUur, GesprekkenPerUur.Max());

        // ── Effectiviteit ─────────────────────────────────────────────────────
        /// <summary>Gesprekken afgerond zonder escalatie (zelfbediening). Alleen zinvol wanneer HeeftEscalatie.</summary>
        public int GesprekkenMetEscalatie { get; set; }
        public double ZelfbedieningPercent => HeeftEscalatie && TotaalGesprekken > 0
            ? Math.Round((TotaalGesprekken - GesprekkenMetEscalatie) * 100.0 / TotaalGesprekken, 1) : 0;

        /// <summary>Gesprekken waarbij de gebruiker precies 1 bericht stuurde en daarna vertrok.</summary>
        public int VerlatenGesprekken { get; set; }
        public double VerlatenPercent { get; set; }

        /// <summary>Unieke widget-bezoekers (unieke sessiontokens) in de huidige maand.</summary>
        public int UniekeBezoekersDezeWeand { get; set; }

        /// <summary>Escalaties per maand voor de laatste 6 maanden (zelfde index als GesprekkenPerMaand). Tier 2 + heeftEscalatie.</summary>
        public int[] EscalatiesPerMaand { get; set; } = new int[6];

        /// <summary>Gem. berichtenaantal voor geëscaleerde gesprekken vs. afgehandelde gesprekken (Tier 2 + heeftEscalatie).</summary>
        public double GemBerichtenGeescaleerd { get; set; }
        public double GemBerichtenAfgehandeld { get; set; }

        // ── Prestaties ────────────────────────────────────────────────────────
        /// <summary>Gemiddelde bot-responstijd in seconden (Tier 1+).</summary>
        public double GemiddeldeResponstijdSec { get; set; }

        /// <summary>95e percentiel bot-responstijd in seconden (Tier 2).</summary>
        public double P95ResponstijdSec { get; set; }

        /// <summary>Gemiddelde responstijd per dag voor de afgelopen 30 dagen (Tier 2).</summary>
        public List<DagWaarde> ResponstijdPerDag { get; set; } = new();

        // ── Escalatie (zichtbaar wanneer HeeftEscalatie, alle tiers) ─────────
        public int TotaalEscalaties { get; set; }
        public int WhatsAppEscalaties { get; set; }
        public int EmailEscalaties { get; set; }
        public double EscalatieRatioPercent { get; set; }

        /// <summary>Gemiddeld aantal berichten vóór de eerste escalatie (alleen Tier 2).</summary>
        public double GemiddeldBerichtenVoorEscalatie { get; set; }
    }

    public record DagTelling(DateTime Datum, int Aantal);
    public record DagWaarde(DateTime Datum, double Waarde);
}
