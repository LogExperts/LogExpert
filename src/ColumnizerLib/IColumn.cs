using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogExpert;

public interface IColumn : ITextValue
{
    #region Properties

    IColumnizedLogLine Parent { get; }

    string FullValue { get; }

    string DisplayValue { get; }

    #endregion
}