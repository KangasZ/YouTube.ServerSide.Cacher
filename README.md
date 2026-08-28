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

## Security

There is an optional protection package. I would highly recommend enabling this, for your own safety.

The way it works is designed for vr chat and not the safest as it puts the api key in the query, however this was done specifically to allow cookie-less authentication for users.

1. Server owner sets a password
2. That password can be used by users on the webui to obtain a persistant token
3. The persistant token is needed to create a apitoken

Notes:
1. The persistant token lasts for many days, and will refresh each time you view the site
2. The api token is shorter lived

### Setup

1.Generate two different signing keys and put them in your environment variables. Note that the algorithm used requires a certain length of key. Generate with any method (ie: `openssl rand -hex 32`)

| Variable                     | Value          |
|------------------------------|----------------|
| Protection__ApiSigningKey    | random value 1 |
| Protection__CookieSigningKey | random value 2 |

2. Set a password. This should be secure. If it is not particularly secure, put it behind a service like fail2ban.

## Reverse Proxy

Considerations:
- Reverse proxies may block large file transfers by default, make sure it does. Nginx has no limit on outbound files by default.
- Reverse proxies may have gateway timeouts if no response is sent. In the case of this app, while yt-dlp downloads the video, it will not be sending any information to the client. You may want to add a larger timeout to your proxy.
- You can setup a cache on your proxy such that videos are served from NGINX rather than .NET, however it's unclear how much of a benefit this is.
- A good safety net includes fail2ban or something similar on the `POST /api/login` endpoint

## Self-Notes

This is hosted both on my personal git server and on github. to push to both, use `git push github && git push gitea`

Update integration tests to support protection and new endpoints