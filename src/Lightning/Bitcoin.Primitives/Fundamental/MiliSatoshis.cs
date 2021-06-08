using System;
using System.Collections.Generic;
using System.Text;

namespace Bitcoin.Primitives.Fundamental
{
   public class MiliSatoshis
   {
      private ulong _value;

      public MiliSatoshis(ulong value)
      {
         _value = value;
      }

      public static implicit operator ulong(MiliSatoshis sats) => sats._value;

      public static implicit operator MiliSatoshis(ulong sats) => new MiliSatoshis(sats);

      public static implicit operator Satoshis(MiliSatoshis msats) => new Satoshis(msats._value / 1000);

      public override string ToString() => $"msats={_value.ToString()}({(Satoshis)this})";
   }
}