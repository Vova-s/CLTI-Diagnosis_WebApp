using CLTI.Diagnosis.Client.Infrastructure.State;

namespace CLTI.Diagnosis.Client.Core.Abstractions
{
    public interface ICltiCaseService
    {
        Task<bool> SaveCaseAsync(StateService stateService);
        Task<bool> LoadCaseAsync(int caseId, StateService stateService);
        Task<bool> DeleteCaseAsync(int caseId);
    }
}