using AccessManager.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace AccessManager.TagHelpers
{
    [HtmlTargetElement(Attributes = "auth")]
    public class AuthTagHelper : TagHelper
    {
        [HtmlAttributeName("auth")]
        public bool Allowed { get; set; }

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; } = default!;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (ViewContext.ViewData.Model is not IAuthAwareViewModel)
                return;

            if (!Allowed)
            {
                ApplyDisabled(output);
            }
        }

        private void ApplyDisabled(TagHelperOutput output)
        {
            // Apply to the current element
            var tagName = output.TagName?.ToLower();

            if (tagName == "a")
            {
                output.Attributes.SetAttribute("style", "pointer-events:none; opacity:0.6; filter:saturate(50%); cursor:not-allowed;");
                output.Attributes.SetAttribute("aria-disabled", "true");
            }
            else if (tagName == "button" || tagName == "input" || tagName == "select" || tagName == "textarea")
            {
                output.Attributes.SetAttribute("disabled", "disabled");
                output.Attributes.SetAttribute("style", "opacity:0.6; cursor:not-allowed;");
            }
            else if (tagName == "div" || tagName == null)
            {
                // For container elements, wrap children in a <span> with pointer-events:none
                // and optionally adjust all interactive descendants via inline JS
                // Simplest is pointer-events:none on the container:
                var existingStyle = output.Attributes.ContainsName("style")
                    ? output.Attributes["style"].Value?.ToString() + ";"
                    : "";
                output.Attributes.SetAttribute("style", existingStyle + "pointer-events:none; opacity:0.6; filter:saturate(50%); cursor:not-allowed;");
            }
        }
    }
}
