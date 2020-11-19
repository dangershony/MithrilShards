using System.Collections.Generic;
using MithrilShards.Core.MithrilShards;

namespace Network.Settings
{
   public class LightningNodeSettings : MithrilShardSettingsBase
   {
      private const long DEFAULT_MAX_TIME_ADJUSTMENT = 70 * 60;

      public long MaxTimeAdjustment { get; set; } = DEFAULT_MAX_TIME_ADJUSTMENT;

      public List<LightningNodeBinding> Connections { get; } = new List<LightningNodeBinding>();
   }
}