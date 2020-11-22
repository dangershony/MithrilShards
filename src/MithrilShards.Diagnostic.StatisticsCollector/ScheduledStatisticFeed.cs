﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MithrilShards.Core.Statistics;
using MithrilShards.Logging.TableFormatter;

namespace MithrilShards.Diagnostic.StatisticsCollector
{
   public class ScheduledStatisticFeed
   {
      /// <summary>
      /// Gets the table builder used to generate a tabular output.
      /// </summary>
      /// <value>
      /// The table builder.
      /// </value>
      private readonly TableBuilder _tableBuilder;


      /// <summary>
      /// The string builder that will hold human readable output;
      /// </summary>
      private readonly StringBuilder _stringBuilder = new StringBuilder();

      /// <summary>
      /// Gets the source of the feed.
      /// </summary>
      /// <value>
      /// The source.
      /// </value>
      public IStatisticFeedsProvider Source { get; }

      /// <summary>
      /// Gets the statistic feed definition.
      /// </summary>
      /// <value>
      /// The statistic feed definition.
      /// </value>
      public StatisticFeedDefinition StatisticFeedDefinition { get; }

      /// <summary>
      /// Gets the next planned execution time.
      /// </summary>
      /// <value>
      /// The next planned execution.
      /// </value>
      public DateTimeOffset NextPlannedExecution { get; internal set; }

      /// <summary>Last obtained result.</summary>
      public List<string?[]> lastResults { get; } = new List<string?[]>();

      /// <summary>
      /// Gets the last result date.
      /// </summary>
      /// <value>
      /// The last result date.
      /// </value>
      public DateTimeOffset LastResultsDate { get; internal set; }

      public ScheduledStatisticFeed(IStatisticFeedsProvider source, StatisticFeedDefinition statisticFeedDefinition)
      {
         this.Source = source ?? throw new ArgumentNullException(nameof(source));
         this.StatisticFeedDefinition = statisticFeedDefinition ?? throw new ArgumentNullException(nameof(statisticFeedDefinition));
         this.NextPlannedExecution = DateTime.Now + statisticFeedDefinition.FrequencyTarget;

         this._tableBuilder = this.CreateTableBuilder();
      }

      /// <summary>
      /// Creates the table builder for a specific <see cref="StatisticFeedDefinition" />.
      /// </summary>
      /// <param name="definition">The definition.</param>
      /// <returns></returns>
      private TableBuilder CreateTableBuilder()
      {
         var tableBuilder = new TableBuilder(this._stringBuilder);

         foreach (FieldDefinition field in this.StatisticFeedDefinition.FieldsDefinition)
         {
            tableBuilder.AddColumn(new ColumnDefinition { Label = field.Label, Width = field.WidthHint, Alignment = ColumnAlignment.Left });
         }

         tableBuilder.Prepare();
         return tableBuilder;
      }


      /// <summary>
      /// Gets an anonymous object containing the feed dump.
      /// </summary>
      /// <returns></returns>
      public object GetLastResultsDump()
      {
         return System.Text.Json.JsonSerializer.Serialize(new
         {
            Title = this.StatisticFeedDefinition.Title,
            Time = this.LastResultsDate,
            Labels = from fieldDefinition in this.StatisticFeedDefinition.FieldsDefinition select fieldDefinition.Label,
            Values = this.lastResults
         });
      }

      public void SetLastResults(IEnumerable<string?[]> results)
      {
         this.lastResults.Clear();
         this.lastResults.AddRange(results);
         this.LastResultsDate = DateTime.Now;
      }


      /// <summary>
      /// Gets the feed in a textual tabular format.
      /// </summary>
      /// <returns></returns>
      public string GetTabularFeed()
      {
         this._stringBuilder.Clear();
         this._tableBuilder.Start($"{this.LastResultsDate.LocalDateTime} - {this.StatisticFeedDefinition.Title}");
         this.lastResults.ForEach(row => this._tableBuilder.DrawRow(row));
         this._tableBuilder.End();
         return this._stringBuilder.ToString();
      }
   }
}
