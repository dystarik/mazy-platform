namespace MazyPlatform.Service.Notification.IntegrationEventHandlers;

using System.Net;

internal static class EmailBodyTemplates
{
    public static string BuildCodeEmail(string title, string description, string code, string additionalInfo)
    {
        var encodedTitle = WebUtility.HtmlEncode(title);
        var encodedDescription = WebUtility.HtmlEncode(description);
        var encodedCode = WebUtility.HtmlEncode(code);
        var encodedAdditionalInfo = WebUtility.HtmlEncode(additionalInfo);

        return $$"""
                 <div style="font-family:Segoe UI,Arial,sans-serif;background-color:#f8fafc;padding:24px;">
                   <div style="max-width:560px;margin:0 auto;background:#ffffff;border:1px solid #e2e8f0;border-radius:12px;padding:24px;">
                     <h2 style="margin:0 0 12px 0;color:#0f172a;font-size:22px;">{{encodedTitle}}</h2>
                     <p style="margin:0 0 16px 0;color:#334155;font-size:15px;line-height:1.5;">{{encodedDescription}}</p>
                     <div style="margin:0 0 16px 0;padding:16px;background:#f1f5f9;border:1px dashed #94a3b8;border-radius:10px;text-align:center;">
                       <span style="display:block;color:#475569;font-size:13px;margin-bottom:8px;">Ваш код:</span>
                       <strong style="font-size:30px;letter-spacing:6px;color:#0f172a;">{{encodedCode}}</strong>
                     </div>
                     <p style="margin:0;color:#64748b;font-size:13px;line-height:1.5;">{{encodedAdditionalInfo}}</p>
                   </div>
                 </div>
                 """;
    }
}
