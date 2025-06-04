using System;
using System.Threading.Tasks;
using CLTI.Diagnosis.Data;
using CLTI.Diagnosis.Data.Entities;
using CLTI.Diagnosis.Client.Algoritm.Services;
using Microsoft.EntityFrameworkCore;

namespace CLTI.Diagnosis.Services
{
    /// <summary>
    /// Service for persisting <see cref="CltiCase"/> data.
    /// </summary>
    public class CltiCaseService
    {
        private readonly ApplicationDbContext _context;

        public CltiCaseService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Creates or updates a <see cref="CltiCase"/> record from the current <see cref="StateService"/> values.
        /// </summary>
        public async Task<int> SaveCaseAsync(StateService state)
        {
            var entity = state.CaseId.HasValue
                ? await _context.CltiCases.FirstOrDefaultAsync(c => c.Id == state.CaseId.Value)
                : null;

            if (entity == null)
            {
                entity = new CltiCase
                {
                    CreatedAt = DateTime.UtcNow
                };
                _context.CltiCases.Add(entity);
            }

            entity.AbiKpi = state.KpiValue;
            entity.FbiPpi = state.PpiValue;

            var w = state.WLevelValue ?? 0;
            entity.W1 = w == 1;
            entity.W2 = w == 2;
            entity.W3 = w == 3;

            var i = state.ILevelValue ?? 0;
            entity.I0 = i == 0;
            entity.I1 = i == 1;
            entity.I2 = i == 2;
            entity.I3 = i == 3;

            var fi = state.FILevelValue ?? 0;
            entity.FI0 = fi == 0;
            entity.FI1 = fi == 1;
            entity.FI2 = fi == 2;
            entity.FI3 = fi == 3;

            entity.ClinicalStageWIfIEnumItemId = state.ClinicalStage;
            entity.CrabPoints = state.CRABTotalScore ?? 0;
            entity.TwoYLE = state.YLETotalScore ?? 0.0;

            entity.GlassAidI = state.GLASSSelectedStage == "Stage I";
            entity.GlassAidII = state.GLASSSelectedStage == "Stage II";
            entity.GlassAidA = state.GLASSSubStage == "A";
            entity.GlassAidB = state.GLASSSubStage == "B";

            if (int.TryParse(state.GLASSFemoroPoplitealStage, out var fps))
                entity.GlassFps = fps;
            if (int.TryParse(state.GLASSInfrapoplitealStage, out var ips))
                entity.GlassIps = ips;

            entity.GlassIid = !string.IsNullOrEmpty(state.GLASSFinalStage);
            entity.GlassIidI = state.GLASSFinalStage == "I";
            entity.GlassIidII = state.GLASSFinalStage == "II";
            entity.GlassIidIII = state.GLASSFinalStage == "III";

            entity.GlassImdP0 = state.SubmalleolarDescriptor == "P0";
            entity.GlassImdP1 = state.SubmalleolarDescriptor == "P1";
            entity.GlassImdP2 = state.SubmalleolarDescriptor == "P2";

            await _context.SaveChangesAsync();
            state.CaseId = entity.Id;
            return entity.Id;
        }
    }
}
