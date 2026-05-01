# YT.Cacher

## What Is This?

This is a basic program to get and cache videos on some local storage and them serve them via a basic web server.
The purpose of this is much the same as [VRCVideoCacher](https://github.com/EllyVR/VRCVideoCacher) and was inspired by by what that tool allows users to do.
This instead moves the caching and serving to a completely separate program, with the intention of hosting it on a type of server and then serving it to anyone as necessary

## Why was it made?

Because there wasn't a tool that did this in the way I wanted, so I made my own.

## How it works

All this app really does is use yt-dlp (with ffmpeg and deno as prereqs) to download webm videos then spits that back out with a basic webserver via .NET.

There's some complexity in how it handles the index page and queuing downloads, however if you're interested just look at the code.

Beside that, cached videos stick around for 1-2 days depending on when the service that cleans them runs.

## Usage

Host the dockerfile behind a reverse proxy and serve videos. If there's desire for more documentation on this please just make a issue or pr.

A couple notes:
You should define the following ENV Variables:

| Variable         | Value                |
|------------------|----------------------|
| Path__CookiePath | /path/to/cookies.txt |
| Path__CachePath  | /path/to/cache/      |

You can create a cookies.txt with yt-dlp with the following command: `yt-dlp --cookies-from-browser {browser} --cookies cookies.txt`.

Generally I would recommend not using your 'main' account for this. The security should be fine but anyone can call your server and download videos, which if on a public website may cause a lot of churn that alphabet might not like.

You don't necessarily *have* to pass in cookies into YT-DLP, and the app *should* work without them (with some warnings) however for best quality and less worries, pass them in.

The app is designed to run under docker, however should have no worries running outside a docker container.

The dockerfile installs the latest Deno and yt-dlp, then whatever ffmpeg package was most recent on ubuntu (which I believe the dotnet base image is based off). Do not that dockerbuilds will contact github for that info on yt-dlp.

### Reverse Proxy

Considerations:
- Reverse proxies may block large file transfers by default, make sure it does. Nginx has no limit on outbound files by default.
- Reverse proxies may have gateway timeouts if no response is sent. In the case of this app, while yt-dlp downloads the video, it will not be sending any information to the client. You may want to add a larger timeout to your proxy.

Personally I use NGINX and use the following configuration to make sure nothing is cached. NGINX doesn't care how large of files the host is sending so there's no max size we need here specifically.

```nginx configuration
location / {
    ...otherconfiguration

    add_header Cache-Control no-cache;
    types {
        video/webm webm;
    }
    add_header 'Access-Control-Allow-Origin' '*' always;
    add_header 'Access-Control-Expose-Headers' 'Content-Length';
    if ($request_method = 'OPTIONS') {
        add_header 'Access-Control-Allow-Origin' '*';
        add_header 'Access-Control-Max-Age' 1728000;
        add_header 'Content-Type' 'text/plain charset=UTF-8';
        add_header 'Content-Length' 0;
        return 204;
    }
}
```
## Self-Notes

This is hosted both on my personal git server and on github. to push to both, use `git push github && git push gitea`
