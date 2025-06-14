using CLTI.Diagnosis.Client.Algoritm.Services;

namespace CLTI.Diagnosis.Client.Services
{
    public interface ICltiCaseService
    {
        Task<bool> SaveCaseAsync(StateService stateService);
        Task<bool> LoadCaseAsync(int caseId, StateService stateService);
        Task<bool> DeleteCaseAsync(int caseId);
    }
}