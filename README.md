![](https://raw.githubusercontent.com/micahmo/m3u8restreamer/master/logo.png)

# m3u8restreamer
This self-hosted service is intended to act as a middle-man between a front-end TV/DVR service like Plex or Emby, and a back-end tuner emulator, like [xTeVe](https://github.com/xteve-project/xTeVe) in cases where Plex is unable to play the given raw m3u8 URL.

# Setup
This service is intended to run in Docker and is available as an [image on Docker Hub](https://hub.docker.com/repository/docker/micahmo/m3u8restreamer).

```
docker run -d --name=m3u8restreamer -p 11034:11034 micahmo/m3u8restreamer
```

### Unraid
To run the container on Unraid, you can use the Docker template from this repository.

In Unraid, navigate to the Docker tab. At the bottom, add https://github.com/micahmo/m3u8restreamer on a new line to the list of template repositories. Save.
At the bottom, choose Add Container. From the template dropdown, choose m3u8restreamer.
Fill out the rest of the options as desired.

# Usage

When creating your .m3u playlists (e.g., for use in xTeVe), prefix the stream URL with the name/address of the server where m3u8restreamer is hosted, plus the mapped port (default 11034) followed by `getStream`, followed by the original URI.

> Note: Replace all forward slashes (`/`) in the original stream URL with `%2F`.


For example, where the original stream URL is `original.stream.com/name.m3u8`, your .m3u would look like the following.

```
#EXTM3U

#EXTINF:-1,CHANNELNAME
http://192.168.1.2:11034/getStream/http:%2F%2Foriginal.stream.com/name.m3u8
```

# Attribution

[Icon](https://www.flaticon.com/premium-icon/television_1487739) made by [Freepik](https://www.flaticon.com/authors/freepik) from [www.flaticon.com](https://www.flaticon.com/).