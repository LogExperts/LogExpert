using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogExpert;

public interface IColumnizerPriority
{
    /// <summary>
    /// Get the priority for this columnizer so the up layer can decide which columnizer is the best fitted one.
    /// </summary>
    /// <param name="samples"></param>
    /// <param name="fileName"></param>
    /// <returns></returns>
    Priority GetPriority(string fileName, IEnumerable<ILogLine> samples);
}