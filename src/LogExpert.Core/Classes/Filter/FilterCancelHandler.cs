using System.Globalization;

using LogExpert.Core.Interface;

using NLog;

namespace LogExpert.Core.Classes.Filter;

public class FilterCancelHandler : IBackgroundProcessCancelHandler
{
    private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();
    #region Fields

    private readonly FilterStarter _filterStarter;

    #endregion

    #region cTor

    public FilterCancelHandler(FilterStarter filterStarter)
    {
        _filterStarter = filterStarter;
    }

    #endregion

    #region Public methods

    public void EscapePressed()
    {
        _logger.Info(CultureInfo.InvariantCulture, "FilterCancelHandler called.");
        _filterStarter.CancelFilter();
    }

    #endregion
}