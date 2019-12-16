﻿using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MithrilShards.Core.Forge {
   public enum ForgeState {
      /// <summary>Assigned when <see cref="IForge"/> instance is created.</summary>
      Created,

      /// <summary>Assigned when <see cref="IForge.Start"/> is called.</summary>
      Starting,

      /// <summary>Assigned when <see cref="IForge.Start"/> finished executing.</summary>
      Started,

      /// <summary>Assigned when <see cref="IForge"/> is shutting down.</summary>
      ShuttingDown,

      /// <summary>Assigned when <see cref="IForge"/> has been shutted down.</summary>
      ShuttedDown
   }
}