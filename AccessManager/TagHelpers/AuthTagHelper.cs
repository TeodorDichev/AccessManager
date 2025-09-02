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
        public string RequiresAuth { get; set; } = string.Empty;

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; } = default!;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (ViewContext.ViewData.Model is not IAuthAwareViewModel authModel)
                return;

            bool allow = RequiresAuth switch
            {
                "write" => authModel.HasWriteAccess,
                "read" => authModel.HasReadAccess,
                _ => true
            };

            if (!allow)
                output.Attributes.SetAttribute("disabled", "disabled");
        }
    }
}
