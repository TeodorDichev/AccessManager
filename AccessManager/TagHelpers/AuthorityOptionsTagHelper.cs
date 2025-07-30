using Microsoft.AspNetCore.Razor.TagHelpers;
using AccessManager.Data.Enums;

namespace AccessManager.TagHelpers
{
    [HtmlTargetElement("authority-options")]
    public class AuthorityOptionsTagHelper : TagHelper
    {
        public AuthorityType MaxLevel { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "select";
            output.Attributes.SetAttribute("class", "form-select");

            foreach (AuthorityType level in Enum.GetValues(typeof(AuthorityType)))
            {
                if (level <= MaxLevel)
                {
                    output.Content.AppendHtml($"<option value='{(int)level}'>{level}</option>");
                }
            }
        }
    }
}
