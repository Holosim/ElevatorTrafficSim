using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowCore.Contracts.Common;

public static class ContractVersion
{
    public const int Major = 1;
    public const int Minor = 0;

    public static string AsString() => $"{Major}.{Minor}";
}

