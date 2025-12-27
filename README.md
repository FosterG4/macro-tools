# Macro Tool

**Macro Tool** is a lightweight, high-performance **keyboard macro** and automation utility for Windows. Built with .NET 8, it enables background key simulation with millisecond precision, allowing you to automate repetitive tasks in specific windows without losing focus.

## Features

- **Keyboard Macro**: Record and replay complex key sequences.
- **Background Automation**: Send keys to specific windows without bringing them to the front.
- **High Precision**: Millisecond-accurate timing for sensitive tasks.
- **Portable EXE**: Single standalone executable with zero installation.

## Requirements

- **Operating System**: Windows 10/11 (x64)
- **Runtime**: None required (Release version is self-contained)
- **Development**: .NET 8 SDK (only if building from source)

## How to Build

### 1. Prerequisites
Ensure you have the [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) installed.

### 2. Clone the Repository
```powershell
git clone <repository-url>
cd macroe
```

### 3. Build for Release (Single File)
To create a standalone, optimized executable (approx. 68MB):

```powershell
cd MacroTool
dotnet publish -c Release
```

The output file `MacroTool.exe` will be located in:
`bin\Release\net8.0-windows\win-x64\publish\`

## How to Use

1.  **Run the Application**: Double-click `MacroTool.exe`.
2.  **Select Target**:
    *   Click the dropdown menu to see a list of open windows.
    *   Select the window you want to send keys to.
    *   Click the **Refresh** (↻) button if your window isn't listed.
3.  **Define Keys**:
    *   Click the **"Set"** button (or the key input box).
    *   The button will turn orange, indicating "Recording Mode".
    *   Press the keys you want to automate (e.g., `F1`, `Ctrl+A`, `Space`).
    *   The recording stops automatically after your input.
4.  **Set Interval**:
    *   Enter the time delay in milliseconds (e.g., `1000` for 1 second).
5.  **Add Macro**:
    *   Click **Add** to put the task in the list.
6.  **Manage Macros**:
    *   **Start/Stop**: Check/Uncheck the "Active" box to toggle the macro.
    *   **Edit Keys**: Click the key text in the list to re-record.
    *   **Edit Interval**: Click the interval number to type a new value.
    *   **Options**:
        *   **Focus**: Brings the window to front before sending keys.
        *   **HW**: Uses hardware simulation (use only if standard mode fails).

## Support the Project

If this tool helps you automate your workflow and saves you time, consider supporting its development. Your contributions help keep the project updated and free for everyone.

<p align="center">
  <a href="https://ko-fi.com/U7U71NMCFN">
    <img src="https://ko-fi.com/img/githubbutton_sm.svg" alt="Support me on Ko-fi" />
  </a>
</p>

<p align="center">
  <a href="https://trakteer.id/dikky-hardian-9saev/tip" style="background-color: #be1e2d; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; font-weight: bold;">
    Support me on Trakteer
  </a>
</p>