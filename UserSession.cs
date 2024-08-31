using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Newtonsoft.Json;
using System.Text; // Add this directive for Encoding
using Microsoft.AspNetCore.Identity;


public static class UserSessionService
{

    

    // This function is the sole arbiter of "what does 'logged in' mean?"
    // A logged in user session for our purposes has a username, is authenticated, and has a role in the claims. 
    // Returns a tuple of username, isAuthenticated and first/default role claims
    // TODO: should unpack/investigate ALL claims, not just the first one. 
    // TODO: handle missing context.User/Identity gracefully
    // TODO: in all uses, see if we check for username == null and isAuthenticated == false; if so, 
    //       can we even have .isAuthenticated with a non-nul username? i.e. can we just check isAuthenticated?
    public static UserDto amILoggedIn(HttpContext context)
    {
        UserDto sessionUser = new UserDto();
        //GlobalStatic.dumpRequest(httpContext);
        sessionUser.Id = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        sessionUser.UserName = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        sessionUser.IsAuthenticated = context.User.Identity?.IsAuthenticated ?? false;
        sessionUser.Role = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
        return sessionUser;

    }




    public static async Task<string?> dumpSession(HttpContext context)
    {
        var fn = "dumpSession"; DBg.d(LogLevel.Trace, fn);
        IdentityUser? user = context.User?.Identity as IdentityUser;
        var claims = context.User?.Claims.Select(c => new { c.Type, c.Value });
        var cookies = context.Request.Headers["Cookie"].ToString();
        var prettyCookies = PrettifyCookieHeader(cookies);
        context.Request.Headers["Cookie"] = "have been prettified - see below";

        // also get the session data:
        var sessionData = new Dictionary<string, string>();
        foreach (var key in context.Session.Keys)
        {
            sessionData[key] = context.Session.GetString(key);
        }


        var serializableContext = new
        {
            Request = new
            {
                context.Request.Method,
                context.Request.Scheme,
                context.Request.Host,
                context.Request.Path,
                context.Request.QueryString,
                context.Request.Headers,
                context.Request.ContentType,
                context.Request.ContentLength,
                context.Request.Protocol
            },
            Response = new
            {
                context.Response.StatusCode,
                Headers = context.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())
            },
            User = new
            {
                user,
                claims
            },
            context.TraceIdentifier,
            context.Connection.Id,
            prettyCookies,
            Session = sessionData
        };


        var session = JsonConvert.SerializeObject(serializableContext, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented
        });

        //DBg.d(LogLevel.Trace, $"{fn}: {session}");
        return session;
    }

    private static List<KeyValuePair<string, string>> PrettifyCookieHeader(string cookieHeader)
    {
        var cookies = cookieHeader.Split(";", StringSplitOptions.RemoveEmptyEntries);
        var prettyCookies = new List<KeyValuePair<string, string>>();
        foreach (var cookie in cookies)
        {
            // split it into name and value
            var parts = cookie.Split("=", StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                prettyCookies.Add(new KeyValuePair<string, string>(parts[0].Trim(), parts[1].Trim()));
            }
        }
        return prettyCookies;
    }
}


