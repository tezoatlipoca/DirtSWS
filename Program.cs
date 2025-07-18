using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using Microsoft.AspNetCore.Antiforgery;
//using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.HttpOverrides;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System.Text.Json.Serialization;



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


// builder.Services.AddDistributedMemoryCache(); // Stores session state in memory.

// builder.Services.AddSession(options =>
// {
//     options.IdleTimeout = TimeSpan.FromMinutes(30); // The session timeout.
// });
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
        GlobalStatic.GenerateUnAuthPage(sb, msg);
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
        GlobalStatic.GenerateUnAuthPage(sb, msg);
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
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = GlobalConfig.MaxUploadSize; 
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = GlobalConfig.MaxUploadSize; // e.g. 10GB
});

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
// app.UseSession(); // Add this line to enable session.
app.UseAuthentication(); // must be before authorization
app.UseAuthorization();

app.UseAntiforgery();

app.Use(async (context, next) =>
    {
        // var fn = "_Middleware.Use_"; //DBg.d(LogLevel.Trace, fn);


        var remoteIpAddress = context.Connection.RemoteIpAddress;
        var forwardedHeader = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        string? strOriginatorIpAddress = "";

        if (!string.IsNullOrEmpty(forwardedHeader))
        {
            // The X-Forwarded-For header can contain multiple IP addresses in case of multiple proxies.
            // The first IP address in the list is the original client's IP address.
            var originalIpAddress = forwardedHeader.Split(',').First().Trim();
            remoteIpAddress = System.Net.IPAddress.Parse(originalIpAddress);
        }
        strOriginatorIpAddress = remoteIpAddress!.ToString() ?? "unknown/localhost";
        //DBg.d(LogLevel.Trace, $"{fn} Request origin: {origin} - from {remoteIpAddress}");

        var path = context.Request.Path.Value;
        if( string.IsNullOrEmpty(path))
            path = "/"; // default to root if path is empty
        string msg = $"{path} <-- from {strOriginatorIpAddress}";
        DBg.d(LogLevel.Information, msg);

        // otherwise, do the normal thing
        try
        {
            await next.Invoke();

            // Check if the response status code is 404
            if (context.Response.StatusCode == 404)
            {
                StringBuilder custom404PageContent = GlobalStatic.Generate404Page(path!, strOriginatorIpAddress.ToString());
                context.Response.StatusCode = 404;
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(custom404PageContent.ToString());
            }
        }
        catch (Microsoft.AspNetCore.Http.BadHttpRequestException ex) when
 (ex.InnerException is AntiforgeryValidationException)
        {
            var antiForgeryEx = ex.InnerException as AntiforgeryValidationException;
            DBg.d(LogLevel.Error, $"AntiforgeryValidationException: {antiForgeryEx?.Message ?? "Unknown error"}");
            context.Response.Clear();
            context.Response.StatusCode = 400; 
            context.Response.ContentType = "application/json";

            // Use the new serializable classes
            var responseBody = new ErrorResponse
            {
                error = new ErrorDetail
                {
                    message = antiForgeryEx?.Message ?? "Unknown error",
                    type = antiForgeryEx?.GetType().Name ?? "Unknown"
                }
            };
            await context.Response.WriteAsync(
                System.Text.Json.JsonSerializer.Serialize(
                    responseBody, DirtSWSJsonContext.Default.ErrorResponse
                )
            );
            return;
        }
    });
app.UseDefaultFiles(new DefaultFilesOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(GlobalConfig.wwwroot!)),
    RequestPath = "" // host at root, same as your static files
});

//app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(GlobalConfig.wwwroot!)),
    RequestPath = "",
    ServeUnknownFileTypes = true, // <-- allow any extension
    DefaultContentType = "application/octet-stream" // fallback MIME type
});



app.MapGet("/about", (HttpContext httpContext) =>
{
    string fn = "/about"; DBg.d(LogLevel.Trace, fn);

    return Results.Text(GlobalStatic.staticAboutPage, "text/html");
}).AllowAnonymous();



app.MapGet("/login", (HttpContext httpContext) =>
{
    string fn = "/login"; DBg.d(LogLevel.Trace, fn);
    StringBuilder sb = new StringBuilder();
    GlobalStatic.GenerateHTMLHead(sb, "Login");
    if (GlobalConfig.messagebox != null)
    {
        sb.AppendLine($"<span style=\"color: red;\">{GlobalConfig.messagebox}</span>");
        GlobalConfig.messagebox = null;
    }
    sb.AppendLine("<form action=\"/login\" method=\"post\">");
    sb.AppendLine("<label for=\"password\">Password:</label><br>");
    sb.AppendLine("<input type=\"password\" id=\"password\" name=\"password\"><br><br>");
    sb.AppendLine("<input type=\"submit\" value=\"Submit\">");
    sb.AppendLine("</form>");
    GlobalStatic.GeneratePageFooter(sb);
    return Results.Content(sb.ToString(), "text/html");
}).AllowAnonymous();

// now map the endpoint that handles the login form submission
app.MapPost("/login", async (HttpContext httpContext) =>
{
    string fn = "/login"; DBg.d(LogLevel.Trace, fn);
    string? password = httpContext.Request.Form["password"];
    string msg = $"password: {password}";
    DBg.d(LogLevel.Information, msg);

    // check the username and password
    if (!string.IsNullOrEmpty(password) && 
        (password == GlobalConfig.backdoorAdminPassword))
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




app.MapGet("/files", (HttpContext httpContext) =>
{
    string fn = "/files (GET)"; DBg.d(LogLevel.Trace, fn);
    StringBuilder sb = new StringBuilder();
    GlobalStatic.GenerateHTMLHead(sb, "Files");

    sb.AppendLine("<p><a href=\"/upload\">Upload Files</a></p>");
    sb.AppendLine("<ul style='list-style-type:none;padding-left:0;'>");

    // Helper to format file sizes
    string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    // Recursively get all files and directories in wwwroot
    void RenderDirectory(string dir, string relativePath = "")
    {
        var directories = Directory.GetDirectories(dir);
        foreach (var subdir in directories)
        {
            var subdirName = Path.GetFileName(subdir);
            var subdirRelative = Path.Combine(relativePath, subdirName);

            // Directory delete link with confirmation and red color
            sb.AppendLine($@"
<li style='display:flex;justify-content:space-between;align-items:center;'>
  <div><b>📁 {subdirRelative}/</b></div>
  <div>
    <a href=""/delete/{subdirRelative}"" style=""color:red;float:right;"" onclick=""return confirm('Are you sure? All contents will be deleted?');"">Delete</a>
  </div>
</li>");
            sb.AppendLine("<ul style='list-style-type:none;padding-left:2em;'>");
            RenderDirectory(subdir, subdirRelative);
            sb.AppendLine("</ul>");
        }

        var files = Directory.GetFiles(dir);
        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            var fileRelative = Path.Combine(relativePath, fileName);
            var fileModificationDate = File.GetLastWriteTime(file).ToString("yyyy-MM-dd HH:mm:ss");
            var fileSize = FormatSize(new FileInfo(file).Length);

            sb.AppendLine($@"
<li style='display:flex;justify-content:space-between;align-items:center;'>
  <div>
    <a href=""{fileRelative}"">{fileName}</a>
  </div>
  <div style='text-align:right;min-width:300px;'>
    <span style='margin-right:1em;'>{fileModificationDate}</span>
    <span style='margin-right:1em;'>{fileSize}</span>
    <a href=""/delete/{fileRelative}"" style=""color:red;"" onclick=""return confirm('Are you sure?');"">Delete</a>
  </div>
</li>");
        }
    }

    if (!Directory.Exists(GlobalConfig.wwwroot) || 
        (Directory.GetFiles(GlobalConfig.wwwroot, "*", SearchOption.AllDirectories).Length == 0 &&
         Directory.GetDirectories(GlobalConfig.wwwroot).Length == 0))
    {
        sb.AppendLine("<li><b>No files or directories found.</b></li>");
    }
    else
    {
        RenderDirectory(GlobalConfig.wwwroot);
    }

    sb.AppendLine("</ul>");
    GlobalStatic.GeneratePageFooter(sb);

    return Results.Content(sb.ToString(), "text/html");
}).RequireAuthorization(new AuthorizeAttribute
{
    AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme,
    Roles = "SuperUser"
});

app.MapGet("/delete/{**filename}", (string filename, HttpContext httpContext) =>
{
    // Decode and sanitize the filename
    var decodedPath = Uri.UnescapeDataString(filename);
    var filePath = Path.Combine(GlobalConfig.wwwroot!, decodedPath);

    // Prevent deletion outside wwwroot
    var fullRoot = Path.GetFullPath(GlobalConfig.wwwroot!);
    var fullTarget = Path.GetFullPath(filePath);
    if (!fullTarget.StartsWith(fullRoot))
    {
        return Results.BadRequest();
    }

    if (File.Exists(filePath))
    {
        File.Delete(filePath);
        return Results.Redirect("/files");
    }
    else if (Directory.Exists(filePath))
    {
        // Recursively delete directory and all contents
        Directory.Delete(filePath, true);
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
if (!string.IsNullOrEmpty(tokens.RequestToken))
{
    await context.Response.WriteAsync(tokens.RequestToken);
}
else
{
    context.Response.StatusCode = 400;
    await context.Response.WriteAsync("No antiforgery token available.");
}
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
    GlobalStatic.GenerateHTMLHead(sb, "Upload a file");
    var html = $@"
    <form id='uploadForm' enctype='multipart/form-data' onsubmit='event.preventDefault(); uploadFiles();'>
      <input type='hidden' name='__RequestVerificationToken' id='antiforgeryToken' value='{token}' />
      <label>Choose File(s)</label>
      <input type='file' name='file' id='fileInputFiles' multiple />
      <label>.. or a directory:</label>
      <input type='file' name='file' id='fileInputDir' webkitdirectory />
      <button type='submit'>Upload</button>
    </form>
    <progress id='progressBar' value='0' max='100' style='width:300px;'></progress>
    <span id='progressLabel' style='margin-left:1em;'></span>
    <div id='status'></div>
    <script>{GlobalStatic.uploadJS}</script>";

    sb.AppendLine(html);
    GlobalStatic.GeneratePageFooter(sb);
    context.Response.ContentType = "text/html";
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

    // Get webkitRelativePath from the form data if present
    var relativePath = form["webkitRelativePath"].ToString();

    if (file != null && file.Length > 0)
    {
        // If webkitRelativePath is present, use it to preserve directory structure
        string targetPath;
        if (!string.IsNullOrEmpty(relativePath))
        {
            // Sanitize and combine with wwwroot
            targetPath = Path.Combine(GlobalConfig.wwwroot!, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        }
        else
        {
            targetPath = Path.Combine(GlobalConfig.wwwroot!, file.FileName);
        }

        DBg.d(LogLevel.Information, $"Uploading file to {targetPath}");
        using (var stream = new FileStream(targetPath, FileMode.Create))
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



app.MapGet("/session", (HttpContext httpContext) =>
{
    string fn = "/session"; DBg.d(LogLevel.Trace, fn);

    StringBuilder sb = new StringBuilder();
    GlobalStatic.GenerateHTMLHead(sb, "Session DEBUG");

    string? msg = null;

    if (httpContext.User.Identity?.IsAuthenticated == true)
    {
        msg = $"{fn} --> Authorized User (knows the secret password)";
    }
    else
    {
        msg = $"{fn} --> Anonymous guest session.";
    }
    sb.AppendLine($"<p>{msg}</p>");
    DBg.d(LogLevel.Information, msg);

    GlobalStatic.GeneratePageFooter(sb);
    return Results.Content(sb.ToString(), "text/html");
}).AllowAnonymous()
.RequireAuthorization(new AuthorizeAttribute
{ AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme });

app.MapGet("/killsession", async (HttpContext httpContext) =>
{
    string fn = "/killsession"; DBg.d(LogLevel.Trace, fn);

    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/session");
}).AllowAnonymous();

//redirect pathless requests to to GlobalConfig.index
app.MapGet("/",  (HttpContext httpContext) =>
{
    string fn = "/"; DBg.d(LogLevel.Trace, fn);
    return Results.Redirect(GlobalConfig.index!);
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







