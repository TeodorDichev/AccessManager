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
            if (ViewContext.ViewData.Model is not IAuthAwareViewModel authModel)
                return;

            if (!Allowed)
                output.Attributes.SetAttribute("disabled", "disabled");
        }
    }
}
