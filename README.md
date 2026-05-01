# YT.Cacher

## What Is This?

This is a basic program to get and cache videos on some local storage and them serve them via a basic web server.
The purpose of this is much the same as [VRCVideoCacher](https://github.com/EllyVR/VRCVideoCacher) and was inspired by by what that tool allows users to do.
This instead moves the caching and serving to a completely separate program, with the intention of hosting it on a type of server and then serving it to anyone as necessary

## Usage

Host the dockerfile behind a reverse proxy and serve videos.

Pass in a cookies.txt file to the container. You can use `Path__CookiePath` as an env variable to pass it in.

Maybe will add more documentation in the future, however for now this is all you get.

##
