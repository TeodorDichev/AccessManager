using Microsoft.AspNetCore.Razor.TagHelpers;
using System;

namespace AccessManager.TagHelpers
{
    [HtmlTargetElement("filter")]
    public class FilterTagHelper : TagHelper
    {
        public string FormId { get; set; } = "";
        public string Action { get; set; } = "";
        public string Controller { get; set; } = "";

        public string HiddenName { get; set; } = "";
        public Guid? FilterId { get; set; }
        public string? FilterDescription { get; set; }

        public string WrapperClass { get; set; } = "filter-wrapper";
        public string? Label { get; set; }
        public bool SubmitOnSelect { get; set; } = true;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var inputId = FormId + "_Input";
            var hiddenId = FormId + "_Hidden";
            var resultsId = FormId + "_Results";
            var clearBtnId = FormId + "_Clear";

            output.TagName = "div";
            output.Attributes.SetAttribute("class", WrapperClass);
            output.Attributes.SetAttribute("data-input-id", inputId);
            output.Attributes.SetAttribute("data-hidden-id", hiddenId);
            output.Attributes.SetAttribute("data-results-id", resultsId);
            output.Attributes.SetAttribute("data-action", Action);
            output.Attributes.SetAttribute("data-url", $"/{Controller}/{Action}");
            output.Attributes.SetAttribute("data-submit-on-select", SubmitOnSelect.ToString().ToLower());


            output.Content.SetHtmlContent($@"
                <div class='position-relative'>
                    {(string.IsNullOrEmpty(Label) ? "" : $"<label for='{inputId}' class='form-label'>{Label}</label>")}
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
