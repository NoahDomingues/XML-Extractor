# XML Extractor

A simple, modern utility for extracting embedded XML blocks from EXE, DLL, and other container files—designed for modders, tool-builders, and anyone who needs to pull out plain-text XML with precision and style.

[<img src="https://github.com/user-attachments/assets/83949190-fb98-4c92-9db1-361d432d3ef8">](https://discord.gg/Z88NnTgpWU)

## 📄 XML Extractor

XML Extractor is a lightweight desktop tool that scans any file for valid raw XML files, extracts them, formats them, and saves them. It can be used for various purposes, including game modding - such as for Codemasters' 2009 title, **Operation Flashpoint: Dragon Rising**.

- Per-file output folders named after your source (dots → underscores)  
- Animated, always-visible progress bar  
- Overwrite prompts in a custom, on-brand dialog  
- Embedded Red Hat Display Bold font for crisp headings  
- Auto-scrolling log box that shows timestamps and details  

## 📥 Installation

XML Extractor is fully portable—no installer required. Download the latest release from **[Releases](https://github.com/NoahDomingues/XML-Extractor/releases)**, unzip, and launch:

- **Windows**: Double-click `XML Extractor.exe`.  
- **Linux/Mac**: Use [WINE](https://www.winehq.org) or Mono (`mono XML\ Extractor.exe`)—untested but often works.  

## 💡 Usage

<a href="https://discord.gg/Z88NnTgpWU"><img width="100%" alt="XML Extractor UI" src="https://github.com/user-attachments/assets/83efbc6e-8421-48fd-9130-cfb952ce7ae0"/></a>

1. Click **Select File…** to choose a single file, or **Select Folder…** to target a directory.  
2. Hit **Extract** to begin scanning.  
3. Watch the red progress bar fill, and follow detailed messages in the log.  
4. Extracted XML files appear in `YourFileName_ext` folders, named like `RootTag_01.xml`, `RootTag_02.xml`, etc.  

XML Extractor will prompt you before overwriting existing output folders.  

## 🛠️ Features

- Extracts only valid XML  
- Single-file and batch-folder modes  
- Automatic BOM detection (UTF-8/16LE/16BE/32)  
- Pretty-printed output with consistent indentation  
- Modern UI
- Logs & progress updates
- 
## 🤝 Support

Questions, bugs, or feature requests? Join our **[Discord server](https://discord.gg/Z88NnTgpWU)** or open an issue on GitHub! Pull requests are always welcome.

[<img src="https://github.com/user-attachments/assets/f61046f5-1dc5-4b0c-87f8-4a94d6cbac96">](https://discord.gg/Z88NnTgpWU)

## 👥 Contributors

[![Contributors][contributors-image]][contributors-link]

[contributors-image]: https://contrib.rocks/image?repo=NoahDomingues/XML-Extractor  
[contributors-link]: https://github.com/NoahDomingues/XML-Extractor/graphs/contributors  

**⭐ If this tool was of any use to you, please consider giving it a Star - it would make my day! ⭐**

<div align="center">  
  <img src="https://capsule-render.vercel.app/api?type=waving&color=gradient&height=100&section=footer" />  
</div>
