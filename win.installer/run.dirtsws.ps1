# PowerShell script to run DirtSWS with all actual command line options

$exe = ".\DirtSWS.exe"

# Set your desired values for each option below
$port = 5000
$bind = "*" # Use "*" for all IPs, or specify an IP
$hostname = "http://localhost"
$wwwroot = "wwwroot"
$runlevel = "Information" # Valid: trace, debug, info, warn, error, critical
$pwd = "Foo" # Leave empty for read-only static site
$sitecss = "dirt_default.css" # Path or URL to stylesheet, or leave empty
$sitepng = "dirt_default_icon.png" # Path or URL to favicon (PNG), or leave empty
$siteinfo = "Owner Name, Contact Info"
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