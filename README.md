# MikuSharp
## The Full C# version of the Hatsune Miku Discord bot!

### The bot
Rewritten from JS to C# with completely redone Music part!

### Contact
* If you want something to talk to NND, look at
[NicoNicoNii](https://github.com/Sekoree/NicoNicoNii)
* For MeekMoe api stuff use
[MeekMoe.Images](https://github.com/Meek-Moe/MeekMoe.Images)
* For anything else feel free to visit our support server https://discord.gg/YPPA2Pu.

If you have good C# knowledge and want to help, feel free to contact us on Discord.

### What this is for
If you want to support the bot and/or fix bugs or add documentation, feel free to fork this and then submit a pull request with changes!

### Requirements

#### General
You need to init the submodules to get the nnd project:
```
git submodule init
git submodule sync
git submodule update
```

#### Linux
 * python3
 * python3-pip
 * nndownload
 * youtube-dl
 * ffmpeg

##### Install python stuff
```bash
#!/bin/bash
echo "Installing Python"
apt install python3 python3-pip -y
echo "Done"
echo "Installing nnddownloader"
pip3 install nndownload
echo "Done"
```

### Used libraries ❤
* [DisCatSharp](https://github.com/Aiko-IT-Systems/DisCatSharp) as Discord bot library
* [AngleSharp](https://github.com/AngleSharp/AngleSharp) for various HTML parsing things
* [FluentFTP](https://github.com/robinrodricks/FluentFTP) for saving the NND stuff
* [Google Youtube API](https://github.com/googleapis/google-api-dotnet-client) for fancy "now playing" command
* [Kitsu](https://github.com/KurozeroPB/Kitsu) to get Anime and Manga info
* [Newtonsoft Json](https://github.com/JamesNK/Newtonsoft.Json) to handle the response of various APIs
* [Mime](https://github.com/hey-red/Mime) to handle all commands that include files with varying file formats
* [Npgsql](https://github.com/npgsql/npgsql) to connect to the PostgreSQL DB for playlists and other persistent data
* [Weeb.net](https://github.com/Daniele122898/Weeb.net) to get stuff from the Weeb.sh API
* [AlbumArtExtraction](https://github.com/Legato-Dev/AlbumArtExtraction) to get album art from MP3's 
* [nndownload](https://github.com/AlexAplin/nndownload) to get niconico videos!
* [NicoNicoNii](https://github.com/Sekoree/NicoNicoNii) to get parse niconico video informations!

Original JS bot made by [davidcralph](https://github.com/davidcralph)
