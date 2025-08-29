using Microsoft.AspNetCore.Razor.TagHelpers;
using System;

namespace AccessManager.TagHelpers
{
    [HtmlTargetElement("filter")]
    public class FilterTagHelper : TagHelper
    {
        // Form & controller/action for fetching data
        public string FormId { get; set; } = "";
        public string Action { get; set; } = "";
        public string Controller { get; set; } = "";

        // Hidden input that binds to the model
        public string HiddenName { get; set; } = "";
        public Guid? FilterId { get; set; }
        public string? FilterDescription { get; set; }

        // Optional CSS wrapper class
        public string WrapperClass { get; set; } = "filter-wrapper";

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            // Generate unique IDs for JS hooks
            var inputId = FormId + "_Input";
            var hiddenId = FormId + "_Hidden";
            var resultsId = FormId + "_Results";
            var clearBtnId = FormId + "_Clear";

            // Outer container
            output.TagName = "div";
            output.Attributes.SetAttribute("class", WrapperClass);
            output.Attributes.SetAttribute("data-input-id", inputId);
            output.Attributes.SetAttribute("data-hidden-id", hiddenId);
            output.Attributes.SetAttribute("data-results-id", resultsId);
            output.Attributes.SetAttribute("data-action", Action);
            output.Attributes.SetAttribute("data-url", $"/{Controller}/{Action}");

            // Inner HTML
            output.Content.SetHtmlContent($@"
                <div class='position-relative'>
                    <label for='{inputId}' class='form-label'>Филтър:</label>
                    <div class='input-group'>
                        <input type='text' id='{inputId}' class='form-control' value='{FilterDescription ?? ""}' autocomplete='off' />
                        <button type='button' class='btn btn-sm btn-outline-danger' id='{clearBtnId}'>×</button>
                    </div>
                    <input type='hidden' id='{hiddenId}' name='{HiddenName}' value='{FilterId}' />
                    <div id='{resultsId}' class='list-group' style='position:absolute; top:100%; left:0; width:100%; z-index:1000; display:none;'></div>
                </div>
            ");
        }
    }
}
