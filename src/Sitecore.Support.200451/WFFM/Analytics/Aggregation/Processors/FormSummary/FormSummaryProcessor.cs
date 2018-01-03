using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Sitecore.Analytics.Aggregation.Pipeline;
using Sitecore.Analytics.Model;
using Sitecore.Diagnostics;
using Sitecore.WFFM.Abstractions.Analytics;
using Sitecore.WFFM.Analytics.Aggregation.Processors.FormSummary;

namespace Sitecore.Support.WFFM.Analytics.Aggregation.Processors.FormSummary
{
  /// <summary>
  /// Defines form summary processor class.
  /// </summary>
  public class FormSummaryProcessor : InteractionAggregationPipelineProcessor
  {
    /// <summary>
    /// Processes form summary.
    /// </summary>
    /// <param name="args">Aggregation pipeline arguments.</param>
    protected override void OnProcess(InteractionAggregationPipelineArgs args)
    {
      Assert.ArgumentNotNull(args, nameof(args));

      var contact = args.Context.Contact;
      var interaction = args.Context.Interaction;

      var pageEvents = args.Context.Interaction.Events.Where(e => e.DefinitionId == PageEventIds.SubmitSuccessEvent);

      if (!pageEvents.Any() || contact.Id == null || interaction.Id == null)
      {
        return;
      }

      foreach (var pageEvent in pageEvents)
      {
        var formSummaryItem = args.Context.Results.GetFact<Sitecore.WFFM.Analytics.Aggregation.Processors.FormSummary.FormSummary>();

        var formSummaryKey = new FormSummaryKey()
        {
          Id = Guid.NewGuid(),
          ContactId = contact.Id.Value,
          FormId = pageEvent.ItemId,
          InteractionId = interaction.Id.Value,
          Created = pageEvent.Timestamp
        };

        var formSummaryValue = new FormSummaryValue()
        {
          Count = 1
        };

        formSummaryItem.Emit(formSummaryKey, formSummaryValue);

        List<FieldData> fields;
        using (TextReader tr = new StringReader(pageEvent.Data))
        {
          fields = new XmlSerializer(typeof(List<FieldData>)).Deserialize(tr) as List<FieldData>;
        }

        if (fields == null)
        {
          continue;
        }

        foreach (var field in fields)
        {
          IEnumerable<string> fieldValues = field.Values != null && field.Values.Count > 0 ? field.Values :
              (!string.IsNullOrEmpty(field.Value) ? Enumerable.Repeat(field.Value, 1) : Enumerable.Repeat(string.Empty, 1));

          foreach (string fieldValue in fieldValues)
          {
            var formFieldValue = args.Context.Results.GetDimension<Sitecore.WFFM.Analytics.Aggregation.Processors.FormFieldValues.FormFieldValues>();
            formFieldValue.Add(formSummaryKey.Id, field.FieldId, field.FieldName, fieldValue);
          }
        }
      }
    }
  }
}