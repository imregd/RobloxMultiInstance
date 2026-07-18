# Roblox Multi Instance

Run multiple instances of Roblox at once, useful for managing several accounts simultaneously.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)

<br>

## How it works

Roblox opens a event handle on launch to prevent a second instance from starting.
This tool closes that handle, allowing additional instances to open without being killed.

<br>
## Download
 
Grab the latest `.exe` from [Releases](https://github.com/imregd/RobloxMultiInstance/releases/tag/v1.0.0), or build from source below.
 
> Antivirus may flag the exe since it closes process handles. This is expected behavior for this type of tool.
 
<br>


## Usage
  (Must be ran in administrator mode)
```
1. Run RobloxMultiInstance.exe
2. Launch Roblox as many times as you need
3. Close the exe when finished
```

<br>

## Build from source

```bash
git clone https://github.com/imregd/RobloxMultiInstance.git
cd RobloxMultiInstance
dotnet build -c Release
```

<br>

## License

Licensed under [MIT](LICENSE).

<br>

---

<sub>This project violates Roblox's Terms of Use. Use at your own risk.</sub>
