using System.Text;


public static class GlobalStatic
{
    public static string applicationName = "DirtSWS";
    public static string sessionCookieName = "DirtSWSSessionCookie";
    public static string antiForgeryCookieName = "DirtSWSAntiForgeryCookie";
    public static string webSite = "https://awadwatt.com/dirtsws";


    // generates everything from the footer to the closing html tag
    // including the closing body tag
    public static async Task GeneratePageFooter(StringBuilder sb)
    {
        sb.AppendLine("<footer>");
        sb.AppendLine($"<div class=\"byline\">Generated by {GlobalStatic.applicationName} {GlobalConfig.bldVersion} at {DateTime.Now}</div>");
        sb.AppendLine("</footer>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
    }

    // generates everything up to and including the opening body tag
    public static async Task GenerateHTMLHead(StringBuilder sb, string title = "")
    {
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\">");
        if(GlobalConfig.sitecss != null)
        {
            sb.AppendLine($"<link rel=\"stylesheet\" type=\"text/css\" href=\"/{GlobalConfig.sitecss}\">");
        }
        if(GlobalConfig.sitepng != null)
        {
            sb.AppendLine($"<link rel=\"icon\" href=\"/{GlobalConfig.sitepng}\" type=\"image/x-icon\">");
        }
        sb.AppendLine($"<title>{title}</title>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body >");
        sb.AppendLine("<span class=\"results\" style=\"color: red;\"></span>");
    }

    public static async Task GenerateUnAuthPage(StringBuilder sb, string msg)
    {
        DBg.d(LogLevel.Trace, "GenerateUnAuthPage");
        // get all the lists

        await GenerateHTMLHead(sb);

        sb.AppendLine($"<h1 class=\"indextitle\">WHUPS</h1>");
        sb.AppendLine($"<p style=\"color: red;\">{msg}</p>");
        sb.AppendLine("<p>Go back to <a href=\"/login\">the login page?</a></p>");
        await GeneratePageFooter(sb);
    }



    public static async Task<StringBuilder> Generate404Page(string requestPath, string userInfo)
    {
        DBg.d(LogLevel.Trace, "Generate404Page");
        // get all the lists
        StringBuilder sb = new StringBuilder();
        await GenerateHTMLHead(sb);

        sb.AppendLine($"<h1 class=\"indextitle\">404</h1>");
        sb.AppendLine($"<p style=\"color: red;\">Page not found</p>");
        sb.AppendLine($"<p>Requested path: {requestPath}</p>");
        sb.AppendLine($"<p>You are: {userInfo}</p>");
        sb.AppendLine($"<p>Go back to <a href=\"{GlobalConfig.index}\">the index page?</a></p>");
        await GeneratePageFooter(sb);
        return sb;
    }


}

