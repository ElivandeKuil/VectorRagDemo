using VectorRagDemo.DAL;
using VectorRagDemo.Models.Entities;
using VectorRagDemo.Models.ViewModels;

namespace VectorRagDemo.BLL
{
    /// <summary>
    /// Assembles the StatistiekenViewModel from repository data.
    /// All date calculations and tier-based branching live here, not in the controller.
    /// </summary>
    public class StatistiekenService
    {
        private readonly StatistiekenRepository _repo;

        public StatistiekenService(StatistiekenRepository repo)
        {
            _repo = repo;
        }

        public async Task<StatistiekenViewModel> BuildAsync(Project project, bool heeftEscalatie)
        {
            var pid  = project.ID;
            var tier = project.StatistiekenTier;
            var now  = DateTime.Now;

            var startOfMonth     = new DateTime(now.Year, now.Month, 1);
            var startOfLastMonth = startOfMonth.AddMonths(-1);
            var day30Ago         = now.AddDays(-30);
            var day90Ago         = now.AddDays(-90);
            var sixMonthsAgo     = new DateTime(now.Year, now.Month, 1).AddMonths(-5);

            // ISO Monday-based week start
            var daysFromMonday  = (int)now.DayOfWeek == 0 ? 6 : (int)now.DayOfWeek - 1;
            var startOfWeek     = now.Date.AddDays(-daysFromMonday);
            var startOfLastWeek = startOfWeek.AddDays(-7);

            var vm = new StatistiekenViewModel
            {
                Project        = project,
                HeeftEscalatie = heeftEscalatie
            };

            // ── Tier 1: Basis ─────────────────────────────────────────────────

            vm.TotaalGesprekken      = await _repo.GetTotaalGesprekkenAsync(pid);
            vm.GesprekkendezeMaand   = await _repo.GetGesprekkenVanafAsync(pid, startOfMonth);
            vm.GesprekkenVorigeMaand = await _repo.GetGesprekkenTussenAsync(pid, startOfLastMonth, startOfMonth);
            vm.GesprekkenDezeWeek    = await _repo.GetGesprekkenVanafAsync(pid, startOfWeek);
            vm.GesprekkenVorigeWeek  = await _repo.GetGesprekkenTussenAsync(pid, startOfLastWeek, startOfWeek);
            vm.TotaalBerichten       = await _repo.GetTotaalBerichtenAsync(pid);

            vm.VerlatenGesprekken = await _repo.GetVerlatenGesprekkenAsync(pid);
            vm.VerlatenPercent    = vm.TotaalGesprekken > 0
                ? Math.Round(vm.VerlatenGesprekken * 100.0 / vm.TotaalGesprekken, 1) : 0;
            vm.UniekeBezoekersDezeWeand = await _repo.GetUniekeBezoekersAsync(pid, startOfMonth);

            var dagData = await _repo.GetGesprekkenPerDagAsync(pid, day30Ago);
            for (int i = 29; i >= 0; i--)
            {
                var d = now.Date.AddDays(-i);
                vm.GesprekkenPerDag.Add(new DagTelling(d, dagData.GetValueOrDefault(d, 0)));
            }
            vm.ActieveDagen = dagData.Values.Count(v => v > 0);

            // ── Tier 2: Uitgebreid ────────────────────────────────────────────
            if (tier >= 2)
            {
                vm.GemiddeldBerichtenPerGesprek = await _repo.GetGemiddeldBerichtenPerGesprekAsync(pid);
                vm.GemiddeldeDuurMinuten        = await _repo.GetGemiddeldeDuurMinutenAsync(pid);
                vm.GesprekkenPerUur             = await _repo.GetGesprekkenPerUurAsync(pid, day90Ago);
                vm.GesprekkenPerDagVanDeWeek    = await _repo.GetGesprekkenPerDagVanDeWeekAsync(pid, day90Ago);

                (vm.TotaalGebruikerBerichten, vm.TotaalBotBerichten) =
                    await _repo.GetBerichtenPerSenderTypeAsync(pid);

                var maandData = await _repo.GetGesprekkenPerMaandAsync(pid, sixMonthsAgo);
                var nl = new System.Globalization.CultureInfo("nl-NL");
                for (int i = 0; i < 6; i++)
                {
                    var m = sixMonthsAgo.AddMonths(i);
                    vm.GesprekkenPerMaand[i]       = maandData.GetValueOrDefault(m, 0);
                    vm.GesprekkenPerMaandLabels[i] = m.ToString("MMM yy", nl);
                }

                var berichtenData = await _repo.GetBerichtenPerDagAsync(pid, day30Ago);
                for (int i = 29; i >= 0; i--)
                {
                    var d = now.Date.AddDays(-i);
                    vm.BerichtenPerDag.Add(new DagTelling(d, berichtenData.GetValueOrDefault(d, 0)));
                }

                vm.BetrokkenGesprekken = await _repo.GetBetrokkenGesprekkenAsync(pid);
                vm.BetrokkenGesprekkenPercent = vm.TotaalGesprekken > 0
                    ? Math.Round(vm.BetrokkenGesprekken * 100.0 / vm.TotaalGesprekken, 1) : 0;
            }

            // ── Prestaties ────────────────────────────────────────────────────
            vm.GemiddeldeResponstijdSec = await _repo.GetGemiddeldeResponstijdSecAsync(pid);

            if (tier >= 2)
            {
                vm.P95ResponstijdSec = await _repo.GetP95ResponstijdSecAsync(pid);

                var rtData = await _repo.GetResponstijdPerDagAsync(pid, day30Ago);
                for (int i = 29; i >= 0; i--)
                {
                    var d = now.Date.AddDays(-i);
                    vm.ResponstijdPerDag.Add(new DagWaarde(d, rtData.GetValueOrDefault(d, 0)));
                }
            }

            // ── Escalatie (all tiers when heeftEscalatie) ────────────────────
            if (heeftEscalatie)
            {
                vm.TotaalEscalaties = await _repo.GetTotaalEscalatiesAsync(pid);

                (vm.WhatsAppEscalaties, vm.EmailEscalaties) =
                    await _repo.GetEscalatiesPerKanaalAsync(pid);

                vm.GesprekkenMetEscalatie = await _repo.GetGesprekkenMetEscalatieAsync(pid);
                vm.EscalatieRatioPercent  = vm.TotaalGesprekken > 0
                    ? Math.Round(vm.GesprekkenMetEscalatie * 100.0 / vm.TotaalGesprekken, 1) : 0;

                if (tier >= 2)
                {
                    var escalatiesMaand = await _repo.GetEscalatiesPerMaandAsync(pid, sixMonthsAgo);
                    for (int i = 0; i < 6; i++)
                    {
                        var m = sixMonthsAgo.AddMonths(i);
                        vm.EscalatiesPerMaand[i] = escalatiesMaand.GetValueOrDefault(m, 0);
                    }

                    (vm.GemBerichtenGeescaleerd, vm.GemBerichtenAfgehandeld) =
                        await _repo.GetGemBerichtenPerUitkomstAsync(pid);
                }

                if (tier >= 2 && vm.TotaalEscalaties > 0)
                    vm.GemiddeldBerichtenVoorEscalatie =
                        await _repo.GetGemiddeldBerichtenVoorEscalatieAsync(pid);
            }

            return vm;
        }
    }
}
