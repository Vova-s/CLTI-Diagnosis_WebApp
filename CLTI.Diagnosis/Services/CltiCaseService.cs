using System;
using System.Threading.Tasks;
using CLTI.Diagnosis.Core.Domain.Entities;
using CLTI.Diagnosis.Data;
using CLTI.Diagnosis.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CLTI.Diagnosis.Services
{
    /// <summary>
    /// Service для збереження та отримання даних CLTI case
    /// </summary>
    public class CltiCaseService
    {
        private readonly ApplicationDbContext _context;

        public CltiCaseService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Зберігає або оновлює case з DTO даних
        /// </summary>
        /// <param name="stateDto">DTO з даними стану</param>
        /// <returns>ID збереженого case</returns>
        public async Task<int> SaveCaseAsync(StateServiceDto stateDto)
        {
            var entity = stateDto.CaseId.HasValue
                ? await _context.CltiCases.FirstOrDefaultAsync(c => c.Id == stateDto.CaseId.Value)
                : null;

            if (entity == null)
            {
                entity = new CltiCase
                {
                    CreatedAt = DateTime.UtcNow
                };
                _context.CltiCases.Add(entity);
            }

            // Маппінг основних даних
            entity.AbiKpi = stateDto.KpiValue;
            entity.FbiPpi = stateDto.PpiValue;

            // WiFI рівні
            var w = stateDto.WLevelValue ?? 0;
            entity.WifiCriteria.W1 = w == 1;
            entity.WifiCriteria.W2 = w == 2;
            entity.WifiCriteria.W3 = w == 3;

            var i = stateDto.ILevelValue ?? 0;
            entity.WifiCriteria.I0 = i == 0;
            entity.WifiCriteria.I1 = i == 1;
            entity.WifiCriteria.I2 = i == 2;
            entity.WifiCriteria.I3 = i == 3;

            var fi = stateDto.FILevelValue ?? 0;
            entity.WifiCriteria.FI0 = fi == 0;
            entity.WifiCriteria.FI1 = fi == 1;
            entity.WifiCriteria.FI2 = fi == 2;
            entity.WifiCriteria.FI3 = fi == 3;

            // CRAB та 2YLE
            entity.ClinicalStageWIfIEnumItemId = stateDto.ClinicalStage;
            entity.CrabPoints = stateDto.CRABTotalScore ?? 0;
            entity.TwoYLE = stateDto.YLETotalScore ?? 0.0;

            // GLASS дані
            entity.GlassCriteria.AidI = stateDto.GLASSSelectedStage == "Stage I";
            entity.GlassCriteria.AidII = stateDto.GLASSSelectedStage == "Stage II";
            entity.GlassCriteria.AidA = stateDto.GLASSSubStage == "A";
            entity.GlassCriteria.AidB = stateDto.GLASSSubStage == "B";

            // Парсинг стадій GLASS
            if (TryParseStageNumber(stateDto.GLASSFemoroPoplitealStage, out var fps))
                entity.GlassCriteria.Fps = fps;
            if (TryParseStageNumber(stateDto.GLASSInfrapoplitealStage, out var ips))
                entity.GlassCriteria.Ips = ips;

            // Фінальна стадія GLASS
            entity.GlassCriteria.Iid = !string.IsNullOrEmpty(stateDto.GLASSFinalStage);
            entity.GlassCriteria.IidI = stateDto.GLASSFinalStage == "I";
            entity.GlassCriteria.IidII = stateDto.GLASSFinalStage == "II";
            entity.GlassCriteria.IidIII = stateDto.GLASSFinalStage == "III";

            // Підкісточкова хвороба
            entity.GlassCriteria.ImdP0 = stateDto.SubmalleolarDescriptor == "P0";
            entity.GlassCriteria.ImdP1 = stateDto.SubmalleolarDescriptor == "P1";
            entity.GlassCriteria.ImdP2 = stateDto.SubmalleolarDescriptor == "P2";

            await _context.SaveChangesAsync();
            return entity.Id;
        }

        /// <summary>
        /// Отримує case за ID та конвертує в DTO
        /// </summary>
        /// <param name="caseId">ID case</param>
        /// <returns>DTO з даними case або null якщо не знайдено</returns>
        public async Task<StateServiceDto?> GetCaseAsync(int caseId)
        {
            var entity = await _context.CltiCases
                .FirstOrDefaultAsync(c => c.Id == caseId);

            if (entity == null)
                return null;

            return new StateServiceDto
            {
                CaseId = entity.Id,
                KpiValue = entity.AbiKpi,
                PpiValue = entity.FbiPpi ?? 0,

                // Відновлюємо WiFI рівні
                WLevelValue = GetWLevel(entity),
                ILevelValue = GetILevel(entity),
                FILevelValue = GetFILevel(entity),

                ClinicalStage = entity.ClinicalStageWIfIEnumItemId,
                CRABTotalScore = entity.CrabPoints,
                YLETotalScore = entity.TwoYLE,

                // GLASS дані
                GLASSSelectedStage = entity.GlassCriteria.AidI ? "Stage I" :
                                   entity.GlassCriteria.AidII ? "Stage II" : null,
                GLASSSubStage = entity.GlassCriteria.AidA ? "A" :
                               entity.GlassCriteria.AidB ? "B" : null,
                GLASSFemoroPoplitealStage = $"Stage{entity.GlassCriteria.Fps}",
                GLASSInfrapoplitealStage = $"Stage{entity.GlassCriteria.Ips}",
                GLASSFinalStage = GetGLASSFinalStage(entity),
                SubmalleolarDescriptor = GetSubmalleolarDescriptor(entity)
            };
        }

        /// <summary>
        /// Видаляє case
        /// </summary>
        /// <param name="caseId">ID case для видалення</param>
        /// <returns>true якщо успішно видалено</returns>
        public async Task<bool> DeleteCaseAsync(int caseId)
        {
            var entity = await _context.CltiCases.FirstOrDefaultAsync(c => c.Id == caseId);
            if (entity == null)
                return false;

            _context.CltiCases.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Отримує список всіх cases (для майбутнього використання)
        /// </summary>
        /// <returns>Список DTO з основними даними</returns>
        public async Task<List<StateServiceDto>> GetAllCasesAsync()
        {
            var entities = await _context.CltiCases
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return entities.Select(entity => new StateServiceDto
            {
                CaseId = entity.Id,
                KpiValue = entity.AbiKpi,
                PpiValue = entity.FbiPpi ?? 0,
                WLevelValue = GetWLevel(entity),
                ILevelValue = GetILevel(entity),
                FILevelValue = GetFILevel(entity),
                ClinicalStage = entity.ClinicalStageWIfIEnumItemId
            }).ToList();
        }

        #region Private Helper Methods

        private static bool TryParseStageNumber(string? stageString, out int stageNumber)
        {
            stageNumber = 0;
            if (string.IsNullOrEmpty(stageString))
                return false;

            if (stageString.StartsWith("Stage") &&
                int.TryParse(stageString.Substring(5), out stageNumber))
            {
                return true;
            }

            return false;
        }

        private static int? GetWLevel(CltiCase entity)
        {
            if (entity.WifiCriteria.W1) return 1;
            if (entity.WifiCriteria.W2) return 2;
            if (entity.WifiCriteria.W3) return 3;
            return 0;
        }

        private static int? GetILevel(CltiCase entity)
        {
            if (entity.WifiCriteria.I0) return 0;
            if (entity.WifiCriteria.I1) return 1;
            if (entity.WifiCriteria.I2) return 2;
            if (entity.WifiCriteria.I3) return 3;
            return null;
        }

        private static int? GetFILevel(CltiCase entity)
        {
            if (entity.WifiCriteria.FI0) return 0;
            if (entity.WifiCriteria.FI1) return 1;
            if (entity.WifiCriteria.FI2) return 2;
            if (entity.WifiCriteria.FI3) return 3;
            return null;
        }

        private static string? GetGLASSFinalStage(CltiCase entity)
        {
            if (entity.GlassCriteria.IidI) return "I";
            if (entity.GlassCriteria.IidII) return "II";
            if (entity.GlassCriteria.IidIII) return "III";
            return null;
        }

        private static string? GetSubmalleolarDescriptor(CltiCase entity)
        {
            if (entity.GlassCriteria.ImdP0) return "P0";
            if (entity.GlassCriteria.ImdP1) return "P1";
            if (entity.GlassCriteria.ImdP2) return "P2";
            return null;
        }

        #endregion
    }
}