# Windows-Mobile-Browser-Streaming
"Run" chromium on your windows phone

Currently a proof of concept inspired by [browservice](https://github.com/ttalvitie/browservice).

Aims to be a usable modern browser for windows mobile devices that anyone can install on a PC (server) and have an up to date web browser on WP (client).


### What works
- [x] 2 way communication with websockets and JSON
- [x] Render buffer forwarding to a UWP client
- [x] Navigation events from UWP client
- [x] Touch input events (only 1 for now) from UWP client
- [X] Portrait orientation only for now
- [X] Connect from local network only (for now)

### What's needed
- [ ] Easy&secure remote connections via tunnels (Ngrok, ZeroTier..etc)
- [ ] Auto scaling renderview based on screen resolution/rotation/UWP viewport
- [ ] HiDPI
- [ ] Multitouch
- [ ] Text input
- [ ] Faster rendering (GPU?)
- [ ] Faster & smarter transport (chunking?)
- [ ] Configurable streaming quality (ondemand rendering?)
- [ ] Audio playback forwarding
- [ ] File uploads
- [ ] File downloads

### What's needed after
- [ ] Tabs
- [ ] In Private/Incognito
- [ ] Back/Forward
- [ ] General browser stuff
- [ ] Continuum support/verify
