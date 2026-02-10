using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowCore.Domain.Routing;

public readonly record struct Destination(int Floor, double PlannedStaySeconds);

