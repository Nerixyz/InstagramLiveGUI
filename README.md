# InstagramLiveGUI

This is a basic GUI for managing an Instagram Broadcast. It integrates with OBS and manages the key automatically.
It uses the [InstagramAPISharp](https://github.com/ramtinak/InstagramApiSharp/) in order to login and start the broadcast.
The GUI uses WPF, the [MaterialDesignInXAML](https://github.com/MaterialDesignInXAML/) library and [Caliburn.Micro](https://github.com/Caliburn-Micro/Caliburn.Micro/.).

## OBS Integration

In order to use the OBS-Integration you will have to intall the [obs-websocket](https://github.com/Palakis/obs-websocket/releases) Plugin.
Currently, the socket should be opened on port 4444 and require no password. In the future you will be able to configure the settings.


