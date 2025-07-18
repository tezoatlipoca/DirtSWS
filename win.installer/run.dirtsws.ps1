# PowerShell script to run DirtSWS with all actual command line options

$exe = "C:\Program Files (x86)\DirtSWS\DirtSWS.exe"

# Set your desired values for each option below
$port = 5000
$bind = "*" # Use "*" for all IPs, or specify an IP
$hostname = "http://localhost:5000" 
$wwwroot = "c:\users\vboxuser\Desktop\wwwroot" # ABSOLUTE PATH; where the files are kept; somewhere the app has permission to write to. 
$runlevel = "Information" # Valid: Trace, Debug, Information (default), Warn, Error, Fatal
$pwd = "Foo" # Leave empty for read-only static site
$sitecss = "c:\Program Files (x86)\DirtSWS\dirt_default.css" # ABSOLUTE Path or URL to stylesheet, or leave empty
$sitepng = "c:\Program Files (x86)\DirtSWS\dirt_default_icon.png" # ABSOLUTE Path or URL to favicon (PNG), or leave empty
$siteinfo = "&lt;YOU PUT YOUR INFO HERE&gt;"
$index = "index.html"
$maxuploadsize = 104857600 # 100MB default

$args = @(
    "--port=$port"
    "--bind=$bind"
    "--hostname=$hostname"
    "--wwwroot=$wwwroot"
    "--runlevel=$runlevel"
    "--pwd=$pwd"
    "--sitecss=$sitecss"
    "--sitepng=$sitepng"
    "--siteinfo=$siteinfo"
    "--index=$index"
    "--maxuploadsize=$maxuploadsize"
)

Write-Host "Running DirtSWS with options:"
Write-Host "$exe $($args -join ' ')"
& $exe $args
