# DirtSWS - the Dirt Simple WebServer
A dirt simple single binary standalone web server with integrated (and very basic) file upload & management capabilities. 
Written using .NET.Core 8

# To run

`./DirtSWS --help` to show usage: 

```
Options:
--port=PORT                     Port to listen on. Default is 5000
--bind=IP                       IP address to bind to. Default is *
--hostname=URL                  URL to use in links. Default is http://localhost
--wwwroot=PATH                  Path to the wwwroot directory. Default is <current directory>/wwwroot
--runlevel=LEVEL                        Log level. Default is info (Information)
--pwd=PASSWORD                  Admin password; REQUIRED for file management; leave empty for read-only static site
--sitecss=URL                   URL to the site stylesheet. Default is null
--sitepng=URL                   URL to the site favicon.ico. Default is null
--index=FILE                    Default site index page. Default is index.html
```
### Notes
- Port - make sure this is open in your firewall
- bind - `*` should be fine for all uses, but where you have multiple network adapters, specify the IP address (not interface) to bind to e.g. `192.168.17.4`
- hostname - this isn't used for anything at the moment (other than generated page footers) but there are future plans for it.
- wwwroot - can be any folder, but if specified, has to be absolute e.g. `D:\statichtml` or `/home/user/statichtml` not `~/static` or `../relative/`. The user DirtSWS is running as must have read access to this folder to serve pages from it, and must have write access if using the file management features. 
- runlevel - Microsoft log levels: trace, debug, info, warn, critical - only msgs at the specified loglevel or above are shown. `trace` is most verbose. All incoming URL and requesting IPs are logged at `info`
- pwd - there are no users or roles; if you specify a password, file management pages are enabled.. if not, they aren't. If you try to access a file management page and haven't provided the pwd, you're redirected to a login page where you can specify the pwd.
- sitecss - provide the filename (in your wwwroot or elsewhere) of the stylesheet you want to use for the administrative pages. 
- sitepng - provide the icon image file (in your wwwroot or elsewhere) to use for favicon.ico for the administrative pages. 
- index - what page/file requests for `/` are sent to. 

# Using
Run the executable as above. 

If the webserver has bound to the right IP address and port
AND that port is open in the computer's firewall
then pointing a browser to `http://<your hostname>/about` should show: 

> **About**
> This is a simple web server written in C# using ASP.NET Core.

(that's all that special endpoint does)

Other special endpoints:
`/files` - shows all files in the `wwwroot` folder with your static files
`/login` - lets you login to use `/files`

## File Management
If the `wwwroot` folder you gave to DirtSWS when it started already has static files in it then
DirtSWS should host them already. For example if your specified `wwwroot` folder is `D:\static`
and it contains a file called `foo.html` then DirtSWS should host that file at `http://<hostname>/foo.html`

If your `wwwroot` does not have any files, navigate to `http://<hostname>/files` then click on **Upload a file**. If successful, the upload screen redirects to the files listing. 
Likewise, the **Delete** link beside each filename deletes the file and the files listing refreshes. 

# Installation
**Remember** 
- if you run the program without specifying a password you can't get into the filemanagement screens
- if the wwwroot you specify doesn't have anything in it when you start DirtSWS you won't see anything
- if you don't specify an index page and/or that index page isn't in your wwwroot folder, that won't work either.

## Linux x64
1. Dump the exe somewhere and `chmod` it so its runnable by whatever user you want to run it.
2. create a script that be used to run it manually or as a service with all the parameters you want. Here's one:
```
#!/bin/bash
echo "running.."
cd /home/tezoatlipoca/bin/dirt
pwd
./DirtSWS --bind=10.0.0.55 --port=8039 --hostname=http://static.awadwatt.com --pwd=Foo --wwwroot=/home/tezoatlipoca/mystaticpage --runlevel=trace > output.log 2>&1
```
3. if you want to run as a service, create an `initd` or `systemd` entry for it the usual way. 

## Windows
1. Dump the program somewhere
2. create a powershell script or batch file that runs it with all the parameters you want.

No work has been done to make DirtSWS run as a Windows Service yet, but supposedly its possible
with the use of 3rd party tools such as https://nssm.cc/ (the Non-Sucking Service Manager (for Windows)).

## Reverse Proxy usage
Should work fine behind reverse proxies like NGINX (tested) and Apache (untested).. however it is 
very likely that you will have to adjust the max form body allowable to clients _through_ the proxy
in order to upload big files. 

NGINX's default is 1MB, change that to something bigger like 25MB with a `client_max_body_size 25M;` directive in your website `.conf` file. 

## What's appsettings.json for?
The ASP.NET.CORE runtimes look to this file for various non-default settings; in this case the 
```
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
```
tells the Kestrel webserver engine to not spam a bunch of `info` msgs about every. single. page. request to STDOUT. 

# Future work
0. get this working/cross-compiling for MacOS, linux-arm64 etc. etc. would be nice to have binaries for every platform .NET Core supports (I just don't have any way to test these)
1. Secure host (https) w/ SSL Certificates - although you can get this for cheap if you use NGINX _in front_ of DirtSWS
2. Automatic maintenance/renewal of SSL certficiates w/ LetsEncrypt.org
3. Provide a facility that lets you check to see if your site is reachable, ports are open in firewall etc. using an external talkback facility. 
