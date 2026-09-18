# YT.Cacher

## What Is This?

This is a basic program to get and cache videos on some local storage and them serve them via a basic web server.
The purpose of this is much the same as [VRCVideoCacher](https://github.com/EllyVR/VRCVideoCacher) and was inspired by by what that tool allows users to do.
This instead moves the caching and serving to a completely separate program, with the intention of hosting it on a type of server and then serving it to anyone as necessary

## Why was it made?

Because there wasn't a tool that did this in the way I wanted, so I made my own. This is essentially just a wrapper on top of YT-DLP. It serves a very specific usecase and requirements. It is not meant to be a one-stop-shop solution for all your download needs, it serves one purpose and serves it well.

## How it works

All this app really does is use yt-dlp (with ffmpeg and deno as prereqs) to download webm videos then spits that back out with a basic webserver via .NET.

There's some complexity in how it handles the index page and queuing downloads, however if you're interested just look at the code.

Beside that, cached videos stick around for some days depending on when the service that cleans them runs.

## Usage

Host the dockerfile behind a reverse proxy and serve videos. If there's desire for more documentation on this please just make a issue or pr.

While it is mainly setup for docker, you can also install the prereqs and run it that way.

## Env Vars to Setup


| Variable                     | Value                         |
|------------------------------|-------------------------------|
| Path__CookiePath             | /path/to/cookies.txt          |
| Path__CachePath              | /path/to/cache/               |
| AllowedOrigins__0            | https://your-url.your-tld     |
| AllowedOrigins__1 (optional) | https://your-url-1.your-tld-1 |
| Protection__Enabled          | true                          |
| Protection__ApiSigningKey    | random value 1                |
| Protection__CookieSigningKey | random value 2                |
| Protection__Password         | yourpassword                  |

### Path__CookiePath

You can create a cookies.txt with yt-dlp with the following command: `yt-dlp --cookies-from-browser {browser} --cookies cookies.txt`.

**DO NOT USE YOUR MAIN ACCOUNT**.

### Protection

The protection module adds a set of signing keys and a password so your service is protected. I have not audited this in depth however it is relatively simply.

#### Generate signing keys

Use a random key for both signing keys. I use the following:

`openssl rand -hex 32`

#### Securing the app

Use a secure password and use somthing like `fail2ban` to protect the `POST api/login` route.

#### Details

The way it works is designed for vr chat and not the safest as it puts the api key in the query, however this was done specifically to allow cookie-less authentication for users.

1. Server owner sets a password
2. That password can be used by users on the webui to obtain a persistant token
3. The persistant token is needed to create a apitoken

Notes:
1. The persistant token lasts for many days, and will refresh each time you view the site
2. The api token is shorter lived

## Reverse Proxy

Considerations:
- Reverse proxies may block large file transfers by default, make sure it does. Nginx has no limit on outbound files by default.
- Reverse proxies may have gateway timeouts if no response is sent. In the case of this app, while yt-dlp downloads the video, it will not be sending any information to the client. You may want to add a larger timeout to your proxy.

## Self-Notes

Update integration tests to support protection and new endpoints
