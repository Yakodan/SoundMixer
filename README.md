\# SoundMixer Pro



\*\*SoundMixer Pro\*\* is a free, open-source virtual microphone mixer with a built-in soundboard for Windows.



It captures your real microphone, lets you apply audio effects (EQ, noise gate, compressor), and mix in sound clips — all output through a virtual audio cable. Your chat partners in Discord, Zoom, or Teams hear both your voice and the sounds you play.



Think of it as a free alternative to Soundpad.



\---



\## Features



\*\*Virtual Microphone\*\*

\- Routes your real microphone through the app and outputs to a virtual audio device

\- Works with Discord, Zoom, Teams, and any app that accepts a microphone input



\*\*Soundboard\*\*

\- Load audio files (MP3, WAV, FLAC, OGG, WMA, AAC)

\- Play sounds over your voice — your chat partner hears both

\- Assign global hotkeys to trigger sounds even when the app is in the background

\- Trim clips visually with a waveform editor and precision auto-zoom

\- Customize each sound with an emoji icon and a display name

\- Drag and drop to reorder sounds

\- Loop mode, per-clip volume control

\- All sounds are stored locally — deleting the original file won't break anything



\*\*Audio Processing\*\*

\- 10-band equalizer

\- Noise gate (removes background noise when you're silent)

\- Compressor (evens out volume)

\- Gain control



\*\*Monitoring\*\*

\- Listen to your own processed voice in headphones (optional)

\- Listen to soundboard sounds independently from your voice



\*\*Interface\*\*

\- 6 color themes (4 dark, 2 light)

\- System tray with minimize-to-tray

\- Optional Windows auto-start

\- Settings auto-save



\---



\## Requirements



| Requirement | Details |

|---|---|

| OS | Windows 10 or 11, 64-bit |

| Virtual Cable | \*\*\[VB-Audio Virtual Cable](https://vb-audio.com/Cable/)\*\* (free) — \*\*must be installed\*\* |

| Runtime | .NET 8.0 (included in the downloadable exe) |



\---



\## Installation



\### Step 1 — Install VB-Audio Virtual Cable



This is required. Without it, the app has no virtual microphone to output to.



1\. Go to \*\*\[https://vb-audio.com/Cable/](https://vb-audio.com/Cable/)\*\*

2\. Download \*\*VBCABLE\_Driver\_Pack\*\* (the big Download button)

3\. Extract the zip

4\. \*\*Right-click\*\* `VBCABLE\_Setup\_x64.exe` → \*\*Run as administrator\*\*

5\. Follow the installer

6\. \*\*Restart your computer\*\*



After restart, open \*\*Control Panel → Sound → Recording tab\*\* and confirm that \*\*CABLE Output (VB-Audio Virtual Cable)\*\* appears in the list.



\### Step 2 — Download and run SoundMixer Pro



1\. Go to the \[Releases](../../releases) page

2\. Download \*\*`SoundMixerPro.exe`\*\* from the latest release

3\. Run it (no installation needed — it's a single portable exe)



\### Step 3 — Configure



1\. In SoundMixer Pro:

&#x20;  - \*\*Microphone\*\*: select your real microphone

&#x20;  - \*\*Virtual Cable\*\*: select `CABLE Input (VB-Audio Virtual Cable)`

&#x20;  - \*\*Headphones\*\*: select your headphones/speakers (for monitoring)

&#x20;  - Click \*\*Start\*\*



2\. In your voice chat app (Discord, Zoom, Teams, etc.):

&#x20;  - Go to \*\*Settings → Voice / Audio\*\*

&#x20;  - Set \*\*Input Device\*\* to \*\*`CABLE Output (VB-Audio Virtual Cable)`\*\*



3\. Done. Your chat partner now hears your voice + any sounds you play.



\---



\## How to use the Soundboard



1\. Click \*\*+ Add\*\* to load an audio file

2\. Click the \*\*emoji button\*\* on a sound card to play it (click again to stop)

3\. Click the \*\*gear icon\*\* on a card to open settings:

&#x20;  - Rename the sound

&#x20;  - Change the emoji icon

&#x20;  - Adjust volume

&#x20;  - Assign a global hotkey

&#x20;  - Trim the clip

&#x20;  - Export or delete

4\. Drag and drop cards to reorder them



\---



\## Audio Signal Flow



```

&#x20;Real Microphone ──▶ DSP (EQ, Gate, Compressor) ──▶ Mixer ──▶ Virtual Cable ──▶ Discord/Zoom

&#x20;                                                     ▲

&#x20;                                                     │

&#x20;                             Soundboard sounds ──────┘

&#x20;                                                     │

&#x20;                                                     ▼

&#x20;                                              Headphones (optional monitoring)

```



\---



\## Building from source



\### Prerequisites



\- \[Visual Studio 2022](https://visualstudio.microsoft.com/) with the \*\*.NET Desktop Development\*\* workload

\- \[.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)



\### Build



1\. Clone the repository

&#x20;  ```

&#x20;  git clone https://github.com/YOUR\_USERNAME/SoundMixer.git

&#x20;  ```

2\. Open `SoundMixer.sln` in Visual Studio

3\. Set \*\*SoundMixer.App\*\* as the startup project

4\. Press \*\*F5\*\* to build and run



\### Publish a release build



Right-click \*\*SoundMixer.App\*\* → \*\*Publish\*\* → Folder, Release, win-x64, Self-contained, Single file.



Or via terminal:

```

dotnet publish SoundMixer.App\\SoundMixer.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish

```



\---



\## Technologies



\- C# / .NET 8.0

\- WPF

\- NAudio (WASAPI audio capture and playback)

\- VB-Audio Virtual Cable

\- Newtonsoft.Json

\- Hardcodet.NotifyIcon.Wpf (system tray)

\- Win32 RegisterHotKey API (global hotkeys)



\---



\## License



MIT — see \[LICENSE](LICENSE).

