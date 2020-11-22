﻿using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Extensions;

namespace MithrilShards.Example
{
   public class DateTimeProvider : IDateTimeProvider
   {
      const int MAX_SAMPLES = 200;
      const string WARNING_MESSAGE = @"
**************************************************************
Please check that your computer's date and time are correct!
If your clock is wrong, the node will not work properly.
To recover from this state fix your time and restart the node.
**************************************************************
";
      /// <summary>
      /// Represents the number of ticks that are in 1 microsecond.
      /// </summary>
      const long TICKS_PER_MICROSECOND = TimeSpan.TicksPerMillisecond / 1000;
      const long UNIX_EPOCH_TICKS = 719_162 * TimeSpan.TicksPerDay;
      const long UNIX_EPOCH_MICROSECONDS = UNIX_EPOCH_TICKS / TimeSpan.TicksPerMillisecond;

      readonly ILogger<DateTimeProvider> _logger;
      private readonly ExampleSettings _settings;


      private readonly MedianFilter<long> _medianFilter;
      private readonly HashSet<IPAddress> _knownPeers;
      private bool _autoAdjustingTimeEnabled = true;
      private bool _showWarning = false;

      /// <summary>UTC adjusted timestamp, or null if no adjusted time is set.</summary>
      protected TimeSpan adjustedTimeOffset { get; set; }

      /// <summary>
      /// Initializes instance of the object.
      /// </summary>
      public DateTimeProvider(ILogger<DateTimeProvider> logger, IOptions<ExampleSettings> options)
      {
         this._logger = logger;
         this._settings = options.Value;

         this.adjustedTimeOffset = TimeSpan.Zero;

         this._medianFilter = new MedianFilter<long>(
            size: MAX_SAMPLES,
            initialValue: 0,
            medianComputationOnEvenElements: args => (args.lowerItem + args.higherItem) / 2
            );

         this._knownPeers = new HashSet<IPAddress>(MAX_SAMPLES);
      }

      /// <inheritdoc />
      public virtual long GetTime()
      {
         return DateTime.UtcNow.ToUnixTimestamp();
      }

      public virtual long GetTimeMicros()
      {
         // Truncate sub-millisecond precision before offsetting by the Unix Epoch to avoid
         // the last digit being off by one for dates that result in negative Unix times
         long microseconds = DateTimeOffset.UtcNow.Ticks / TICKS_PER_MICROSECOND;
         return microseconds - UNIX_EPOCH_MICROSECONDS;
      }


      /// <inheritdoc />
      public virtual DateTime GetUtcNow()
      {
         return DateTime.UtcNow;
      }

      /// <inheritdoc />
      public virtual DateTimeOffset GetTimeOffset()
      {
         return DateTimeOffset.UtcNow;
      }

      /// <inheritdoc />
      public DateTime GetAdjustedTime()
      {
         return this.GetUtcNow().Add(this.adjustedTimeOffset);
      }

      /// <inheritdoc />
      public long GetAdjustedTimeAsUnixTimestamp()
      {
         return new DateTimeOffset(this.GetAdjustedTime()).ToUnixTimeSeconds();
      }

      /// <inheritdoc />
      public void SetAdjustedTimeOffset(TimeSpan adjustedTimeOffset)
      {
         this.adjustedTimeOffset = adjustedTimeOffset;
      }

      public void AddTimeData(TimeSpan timeoffset, IPEndPoint remoteEndPoint)
      {
         if (!_autoAdjustingTimeEnabled)
         {
            this._logger.LogDebug("Automatic time adjustment is disabled.");
            if (_showWarning)
            {
               this._logger.LogCritical(WARNING_MESSAGE);
            }
            return;
         }

         /// note: this behavior mimic bitcoin core but it's broken as it is on bitcoin core.
         /// something better should be implemented.

         if (this._knownPeers.Count == MAX_SAMPLES)
         {
            this._logger.LogDebug("Ignored AddTimeData: max peer tracked.");
            return;
         }

         if (this._knownPeers.Contains(remoteEndPoint.Address))
         {
            this._logger.LogDebug("Ignored AddTimeData: peer already contributed.");
            return;
         }

         this._knownPeers.Add(remoteEndPoint.Address);
         this._medianFilter.AddSample((long)timeoffset.TotalSeconds);

         // There is a known issue here (see issue #4521):
         //
         // - The structure vTimeOffsets contains up to 200 elements, after which
         // any new element added to it will not increase its size, replacing the
         // oldest element.
         //
         // - The condition to update nTimeOffset includes checking whether the
         // number of elements in vTimeOffsets is odd, which will never happen after
         // there are 200 elements.
         //
         // But in this case the 'bug' is protective against some attacks, and may
         // actually explain why we've never seen attacks which manipulate the
         // clock offset.
         //
         // So we should hold off on fixing this and clean it up as part of
         // a timing cleanup that strengthens it in a number of other ways.
         //
         if (this._medianFilter.Count >= 5 && this._medianFilter.Count % 2 == 1)
         {
            long median = this._medianFilter.GetMedian();

            // Only let other nodes change our time by so much
            if (Math.Abs(median) <= Math.Max(0, this._settings.MaxTimeAdjustment))
            {
               this.SetAdjustedTimeOffset(TimeSpan.FromSeconds(median));
            }
            else
            {
               this._autoAdjustingTimeEnabled = false;
               this._showWarning = true;
               this.SetAdjustedTimeOffset(TimeSpan.Zero);
            }
         }
      }
   }
}
