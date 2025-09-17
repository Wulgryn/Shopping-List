using CommunityToolkit.Mvvm.Input;
using SL.Sample.Models;

namespace SL.Sample.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}