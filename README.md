# DirtSWS - the Dirt Simple WebServer
A single binary standalone web server with integraded (and very basic) file upload capabilities. 

# To run

`./DirtSWS --help` to show usage: 

```
2024-08-31T14:36:07 DEBUG | [CommandLineParse//GlobalConfig.cs:37] Startup
2024-08-31T14:36:07 TRACE | [CommandLineParse//GlobalConfig.cs:40] Startup command line argument 0 is --help
Usage: dotnet run -- [options]
Options:
--port=PORT                     Port to listen on. Default is 5000
--bind=IP                       IP address to bind to. Default is *
--hostname=URL                  URL to use in links. Default is http://localhost
--wwwroot=PATH                  Path to the wwwroot directory. Default is <current directory>/wwwroot
--runlevel=LEVEL                        Log level. Default is Information
--pwd=PASSWORD                  Admin password; REQUIRED for file management; leave empty for read-only static site
--sitecss=URL                   URL to the site stylesheet. Default is null
--sitepng=URL                   URL to the site favicon.ico. Default is null
--index=FILE                    Default site index page. Default is index.html
```
### Notes
- Port - make sure this is open in your firewall
- bind - `*` should be fine for all uses, but where you have multiple network adapters, specify the IP address (not interface) to bind to e.g. `192.168.17.4`
- hostname - this isn't used for anything at the moment but there are future plans for it.
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

# Future work
1. Secure host (https) w/ SSL Certificates - although you can get this for cheap if you use NGINX _in front_ of DirtSWS
2. Automatic maintenance/renewal of SSL certficiates w/ LetsEncrypt.org
3. Provide a facility that lets you check to see if your site is reachable, ports are open in firewall etc. using an external talkback facility. 