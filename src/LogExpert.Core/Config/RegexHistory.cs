using System;
using System.Collections.Generic;

namespace LogExpert.Core.Config;

[Serializable]
public class RegexHistory
{
    public List<string> ExpressionHistoryList { get; set; } = [];

    public List<string> TesttextHistoryList { get; set; } = [];
}