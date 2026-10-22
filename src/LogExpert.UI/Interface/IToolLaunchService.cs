using LogExpert.UI.Services.ToolLaunchService;

namespace LogExpert.UI.Interface;

internal interface IToolLaunchService
{
    ToolLaunchResult Launch (ToolLaunchRequest request);
}
