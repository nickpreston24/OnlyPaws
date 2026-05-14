using Drip.UI;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace OnlyPaws.Pages;

[HtmlTargetElement("signin")]
public class SignIn : IslandTagHelper
{
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null; // or "div" depending on desired wrapper

        output.TagMode = TagMode.StartTagAndEndTag;

        string content = """
                         <div class="hero bg-base-200 min-h-screen">
                             <div class="hero-content flex-col lg:flex-row-reverse">
                                 <div class="text-center lg:text-left">
                                     <h1 class="text-5xl font-bold">Login now!</h1>
                                     <p class="py-6">
                                         Together, we'll find homes for <b class='text-xl text-rose-600'>ALL</b> shelter pets!
                                     </p>
                                 </div>
                                 <div class="card bg-base-100 w-full max-w-sm shrink-0 shadow-2xl">
                                     <div class="card-body">
                                         <fieldset class="fieldset">
                                             <label class="label">Email</label>
                                             <input type="email" class="input" placeholder="Email" />
                                             <label class="label">Password</label>
                                             <input type="password" class="input" placeholder="Password" />
                                             <div><a class="link link-hover">Forgot password?</a></div>
                                             <button class="btn btn-neutral mt-4">Login</button>
                                         </fieldset>
                                     </div>
                                 </div>
                             </div>
                         </div>
                         """;

        // Using serialized html technique
        output.Content.SetHtmlContent(new HtmlString(content));
    }
}
