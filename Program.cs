using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using Microsoft.AspNetCore.Antiforgery;
//using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.HttpOverrides;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;



bool cliOK = GlobalConfig.CommandLineParse(args);

var builder = WebApplication.CreateBuilder(args);
if (!cliOK)
{
    DBg.d(LogLevel.Critical, "Command line parsing failed. Exiting.");
    Environment.Exit(1);
}
else
{

}

DBg.d(LogLevel.Information, $"DirtSWS:{GlobalConfig.bldVersion}");


builder.Services.AddDistributedMemoryCache(); // Stores session state in memory.

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // The session timeout.
});
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.Name = GlobalStatic.sessionCookieName;

    options.Cookie.HttpOnly = true; // prevent client from accessing the cookie
    options.Cookie.IsEssential = true; //user must accept this cookie
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.Domain = GlobalConfig.CookieDomain;
    options.LoginPath = "/login"; // Change this to your desired login path
    // this redirects any failure from the .RequireAuthorization() on endpoints.
    options.Events.OnRedirectToAccessDenied = async context =>
    {
        var fn = "cookie middleware"; DBg.d(LogLevel.Trace, $"{fn} - OnRedirectToAccessDenied");
        
        // we want to differentiate between requests from our javascript front end
        // logic vs. requests on the endpoints directly. our javascript gets a code
        // and it will figure out how to handle it/present to user. 
        //
        // 403 is "i know who you are you just can't do this"
        // 401 is "i don't know who you are, go log in"
        
            var sb = new StringBuilder();
            string requestedUrl = context.Request.Path + context.Request.QueryString;
            string msg = $"403 -You are not authorized to access {requestedUrl}";
            await GlobalStatic.GenerateUnAuthPage(sb, msg);
            DBg.d(LogLevel.Trace, $"Cookie - OnRedirectToAccessDenied [web] {msg}");
            var result = Results.Content(sb.ToString(), "text/html");
            await result.ExecuteAsync(context.HttpContext);
        
    };
    // this is what fires when the user has not logged in yet; 401 Unauthorized
    // rationale for 401 when unauth, but a redirect when insufficiently authed
    // is the client side js needs an easy prompt to go log in. 
    options.Events.OnRedirectToLogin = async context =>
    {
        var fn = "cookie middleware"; DBg.d(LogLevel.Trace, $"{fn} - OnRedirectToLogin");
        
            var sb = new StringBuilder();
            string requestedUrl = context.Request.Path + context.Request.QueryString;
            string msg = $"401 - You need to <a href=\"/login\">LOGIN</a> to access {requestedUrl}";
            await GlobalStatic.GenerateUnAuthPage(sb, msg);
            DBg.d(LogLevel.Trace, $"{fn} - OnRedirectToLogin [web] {msg}");
            var result = Results.Content(sb.ToString(), "text/html");
            await result.ExecuteAsync(context.HttpContext);
        
    };
});



builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperUser", policy => policy.RequireRole("SuperUser"));

});

//builder.Services.AddControllers().AddNewtonsoftJson();

//builder.Services.AddControllersWithViews();

builder.Services.AddAntiforgery(options => 
{
    options.HeaderName = "X-CSRF-TOKEN";
});

builder.WebHost.UseUrls($"http://{GlobalConfig.Bind}:{GlobalConfig.Port}");

var app = builder.Build();
// this configures the middleware to respect the X-Forwarded-For and X-Forwarded-Proto headers
// that are set by any reverse proxy server (nginx, apache, etc.)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});



// setup session middleware ---------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error"); // improve this. actually define that route for one. 
    app.UseHsts();
}

app.UseRouting();
app.UseSession(); // Add this line to enable session.
app.UseAuthentication(); // must be before authorization
app.UseAuthorization();

app.UseAntiforgery();

app.Use(async (context, next) =>
    {
        var fn = "_Middleware.Use_"; //DBg.d(LogLevel.Trace, fn);

        
        var remoteIpAddress = context.Connection.RemoteIpAddress;
        //DBg.d(LogLevel.Trace, $"{fn} Request origin: {origin} - from {remoteIpAddress}");

        var path = context.Request.Path.Value;
        string msg = $"{path} <-- from {remoteIpAddress}";
        DBg.d(LogLevel.Information, msg);
        
        // otherwise, do the normal thing
        try
        {
            await next.Invoke();

            // Check if the response status code is 404
            if (context.Response.StatusCode == 404)
            {
                StringBuilder custom404PageContent = await GlobalStatic.Generate404Page(path, remoteIpAddress.ToString());
                context.Response.StatusCode = 404;
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(custom404PageContent.ToString());
            }
        }
        catch (Microsoft.AspNetCore.Http.BadHttpRequestException ex) when
         (ex.InnerException is AntiforgeryValidationException)
        {
            var antiForgeryEx = ex.InnerException as AntiforgeryValidationException;
            // Log the error if needed
            // _logger.LogError(ex);
            DBg.d(LogLevel.Error, $"AntiforgeryValidationException: {antiForgeryEx.Message}");
            context.Response.Clear();
            context.Response.StatusCode = 400; // Or any status code you want to return
            context.Response.ContentType = "application/json";

            var responseBody = new
            {
                error = new
                {
                    message = antiForgeryEx.Message,
                    type = antiForgeryEx.GetType().Name
                }
            };

            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(responseBody));

            return;
        }
    });


//app.UseHttpsRedirection();
app.UseStaticFiles();






app.MapGet("/login", async (HttpContext httpContext) => {
    string fn = "/login"; DBg.d(LogLevel.Trace, fn);
    StringBuilder sb = new StringBuilder();
    await GlobalStatic.GenerateHTMLHead(sb, "Login");
    sb.AppendLine("<h1>Login</h1>");
    if(GlobalConfig.messagebox != null)
    {
        sb.AppendLine($"<span style=\"color: red;\">{GlobalConfig.messagebox}</span>");
        GlobalConfig.messagebox = null;
    }
    sb.AppendLine("<form action=\"/login\" method=\"post\">");
    sb.AppendLine("<label for=\"password\">Password:</label><br>");
    sb.AppendLine("<input type=\"password\" id=\"password\" name=\"password\"><br><br>");
    sb.AppendLine("<input type=\"submit\" value=\"Submit\">");
    sb.AppendLine("</form>");
    await GlobalStatic.GeneratePageFooter(sb);
    return Results.Content(sb.ToString(), "text/html");
}).AllowAnonymous();

// now map the endpoint that handles the login form submission
app.MapPost("/login", async (HttpContext httpContext) =>
{
    string fn = "/login"; DBg.d(LogLevel.Trace, fn);
    string password = httpContext.Request.Form["password"];
    string msg = $"password: {password}";
    DBg.d(LogLevel.Information, msg);

    // check the username and password
    if (password == GlobalConfig.backdoorAdminPassword)
    {
        // create the claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "SuerUser"),
            new Claim(ClaimTypes.Role, "SuperUser")
        };

        // create the identity
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        // create the principal
        var principal = new ClaimsPrincipal(identity);

        // sign in
        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return Results.Redirect("/files");
    }
    else
    {
        DBg.d(LogLevel.Warning, "Login failed from " + httpContext.Connection.RemoteIpAddress);
        GlobalConfig.messagebox = "Login failed";
        return Results.Redirect("/login");
    }
}).AllowAnonymous();




app.MapGet("/files", async (HttpContext httpContext) =>
{
    string fn = "/files (GET)"; DBg.d(LogLevel.Trace, fn);
    StringBuilder sb = new StringBuilder();
    await GlobalStatic.GenerateHTMLHead(sb, "Files");
    sb.AppendLine("<h1>Files</h1>");
    sb.AppendLine("<p><a href=\"/upload\">Upload a file</a></p>");
    sb.AppendLine("<ul>");
    // get a list of files in GlobalConfig.wwwroot
    var files = Directory.GetFiles(GlobalConfig.wwwroot);
    // for each file, strip off the root path and add a link to the file; also add a
    // link to delete the file.  
    foreach (var file in files)
    {
        var fileName = Path.GetFileName(file);
        var fileModificationDate = File.GetLastWriteTime(file);
        sb.AppendLine($"<li>{fileModificationDate} <a href=\"/{fileName}\">{fileName}</a> <a href=\"/delete/{fileName}\">Delete</a></li>");
    }
    sb.AppendLine("</ul>");
    await GlobalStatic.GeneratePageFooter(sb);

    return Results.Content(sb.ToString(), "text/html");

    
}).RequireAuthorization(new AuthorizeAttribute
{
    AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme,
    Roles = "SuperUser"
});

app.MapGet("/delete/{filename}", async (string filename, HttpContext httpContext) =>
{
    Uri.UnescapeDataString(filename);
    string fn = $"/delete/{filename} (GET)"; DBg.d(LogLevel.Trace, fn);
    // is filename a valid file in folder wwwroot? if so, delete it
    var filePath = Path.Combine(GlobalConfig.wwwroot, filename);
    if (File.Exists(filePath))
    {
        File.Delete(filePath);
        return Results.Redirect("/files");
    }
    else
    {
        return Results.NotFound();
    }
    
}).RequireAuthorization(new AuthorizeAttribute
{
    AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme,
    Roles = "SuperUser"
});



app.MapGet("/antiforgerytoken", async context =>
{
    var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
    var tokens = antiforgery.GetAndStoreTokens(context);
    await context.Response.WriteAsync(tokens.RequestToken);
}).RequireAuthorization(new AuthorizeAttribute
{
    AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme,
    Roles = "SuperUser"
});

app.MapGet("/upload", async context =>
{
    var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
    var tokens = antiforgery.GetAndStoreTokens(context);
    var token = tokens.RequestToken;
    StringBuilder sb = new StringBuilder();
    await GlobalStatic.GenerateHTMLHead(sb, "Upload");
    var html = $@"
    <h1>Upload a file</h1>
    <script>
        async function uploadFile() {{
            const form = document.getElementById('uploadForm');
            const formData = new FormData(form);
            
            // Append the token to the form data
            formData.append('__RequestVerificationToken', '{token}');
            
            // Submit the form data
            const uploadResponse = await fetch('/fileuploadxfer', {{
                method: 'POST',
                body: formData,
                headers: {{
                    'X-CSRF-TOKEN': '{token}'
                }}
            }});
            
            if (uploadResponse.ok) {{
                window.location.href = '/files';
            }} else {{
                document.querySelector('.results').innerHTML = 'File upload failed';
            }}
        }}
    </script>

    <form id='uploadForm' enctype='multipart/form-data' onsubmit='event.preventDefault(); uploadFile();'>
        <input type='file' name='file' required />
        <input type='hidden' name='__RequestVerificationToken' value='{token}' />
        <button type='submit'>Upload</button>
    </form>";
    sb.AppendLine(html);
    await GlobalStatic.GeneratePageFooter(sb);    
    await context.Response.WriteAsync(sb.ToString());
}).RequireAuthorization(new AuthorizeAttribute
{
    AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme,
    Roles = "SuperUser"
});

app.MapPost("/fileuploadxfer", async context =>
{
    var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
    await antiforgery.ValidateRequestAsync(context);

    var form = await context.Request.ReadFormAsync();
    var file = form.Files["file"];

    if (file != null && file.Length > 0)
    {
        var filePath = Path.Combine(GlobalConfig.wwwroot, file.FileName);
        DBg.d(LogLevel.Information, $"Uploading file to {filePath}");
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }
        context.Response.StatusCode = 200;
        await context.Response.WriteAsync("File uploaded successfully");
    }
    else
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("No file uploaded");
    }
}).RequireAuthorization(new AuthorizeAttribute
{
    AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme,
    Roles = "SuperUser"
});



app.MapGet("/session", async (HttpContext httpContext) =>
{
    string fn = "/session"; DBg.d(LogLevel.Trace, fn);

    StringBuilder sb = new StringBuilder();
    sb.AppendLine("<!DOCTYPE html><html><body>");

    var sessionUser = UserSessionService.amILoggedIn(httpContext);
    string? niceSession = null;
    niceSession = await UserSessionService.dumpSession(httpContext);

    string? msg = null;

    if (sessionUser.IsAuthenticated)
    {
        //that's fine, that may just mean they weren't in the database. 
        msg = $"{fn} --> username: {sessionUser.UserName} role: {sessionUser.Role}";
    }
    else
    {
        msg = $"{fn} --> Anonymous guest session.";
    }
    sb.AppendLine($"SuperUser?: {httpContext.User.IsInRole("SuperUser")}");
    sb.AppendLine("<br>");
    sb.AppendLine($"<p>{msg}</p><pre>{niceSession}</pre>");
    DBg.d(LogLevel.Information, msg);

    sb.AppendLine("</body></html>");
    return Results.Content(sb.ToString(), "text/html");
}).AllowAnonymous()
.RequireAuthorization(new AuthorizeAttribute
{ AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme });

app.MapGet("/killsession", async (HttpContext httpContext) =>
{
    string fn = "/killsession"; DBg.d(LogLevel.Trace, fn);

    // just destroy the browser session
    httpContext.Session.Clear();
    httpContext.Session.Remove(GlobalStatic.sessionCookieName);
    httpContext.Session.Remove(GlobalStatic.sessionCookieName + ".UserName");
    httpContext.Session.Remove(GlobalStatic.sessionCookieName + ".Role");
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/session");
}).AllowAnonymous();

//redirect pathless requests to to GlobalConfig.index
app.MapGet("/", async (HttpContext httpContext) =>
{
    string fn = "/"; DBg.d(LogLevel.Trace, fn);
    return Results.Redirect(GlobalConfig.index);
}).AllowAnonymous();


// Mutex to ensure only one of us is running

bool createdNew;
using (var mutex = new Mutex(true, GlobalStatic.applicationName, out createdNew))
{
    if (createdNew)
    {
        app.Run();
    }
    else
    {
        Console.WriteLine("Another instance of the application is already running.");
    }
}







