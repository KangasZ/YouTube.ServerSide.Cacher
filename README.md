# YT.Cacher

## What Is This?

This is a basic program to get and cache videos on some local storage and them serve them via a basic web server.
The purpose of this is much the same as [VRCVideoCacher](https://github.com/EllyVR/VRCVideoCacher) and was inspired by by what that tool allows users to do.
This instead moves the caching and serving to a completely separate program, with the intention of hosting it on a type of server and then serving it to anyone as necessary

## Usage

Host the dockerfile behind a reverse proxy and serve videos.

Pass in a cookies.txt file to the container. You can use `Path__CookiePath` as an env variable to pass it in.

Maybe will add more documentation in the future, however for now this is all you get.

You need to have yt-dlp, deno, and ffmpeg installed and on the path. The dockerfile does this, however for local development you'll need to do it yourself.

### Reverse Proxy

Since this downloads an entire YT file before serving, along with sending a potentially very large file, you'll need to setup your reverse proxy to support large files.

Personally I use NGINX and use the following configuration to make sure nothing is cached. NGINX doesn't care how large of files the host is sending so there's no max size we need here specifically.

```nginx configuration
server {
    ...otherconfiguration

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
}
```
## Self-Notes

This is hosted both on my personal git server and on github. to push to both, use `git push github && git push gitea`
