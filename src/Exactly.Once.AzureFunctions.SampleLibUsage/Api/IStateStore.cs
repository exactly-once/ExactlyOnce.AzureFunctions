using System.Threading.Tasks;

namespace Exactly.Once.AzureFunctions.SampleLibUsage.Api
{
    public interface IStateStore
    {
        Task<(TState, string)> Load<TState>(string stateId) where TState : State, new();

        Task<string> Upsert<TState>(string stateId, TState value, string version) where TState : State;
    }
}