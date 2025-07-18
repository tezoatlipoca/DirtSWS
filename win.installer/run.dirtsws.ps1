# Example PowerShell script to run DirtSWS with all command line options

# Set the path to your DirtSWS executable
$exe = ".\DirtSWS.exe"

# Example command line options (update these to match your actual options)
# Replace with the actual options supported by DirtSWS
$wwwroot = "wwwroot"
$port = 8080
$bind = "0.0.0.0"
$loglevel = "Information"
$config = "config.json"
$index = "index.html"
$siteinfo = "My DirtSWS Instance"
$cookieDomain = "localhost"
$otherOption = "value"

# Build the argument list
$args = @(
    "--wwwroot", $wwwroot
    "--port", $port
    "--bind", $bind
    "--loglevel", $loglevel
    "--config", $config
    "--index", $index
    "--siteinfo", "`"$siteinfo`""
    "--cookieDomain", $cookieDomain
    # Add other options here as needed
    # "--otherOption", $otherOption
)

# Run DirtSWS with all options
Write-Host "Running DirtSWS with options:"
Write-Host "$exe $($args -join ' ')"
& $exe @args