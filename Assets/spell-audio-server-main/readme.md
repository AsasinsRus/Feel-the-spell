# Feeling the spell - Audio server

## Setup

1. Install Python
2. Run `pip install -r requirements.txt`

## How to run

`python main.py` or `./main.py`

### Arguments

- `w`/`--whole-line` makes the server react only after a whole line has been spoken
- `-l`/`--language`: Sets the language detected (for example. "en-us" or "de")
    - Default: "en-us"
- `-d`/`--device`: Sets the input device to be used (either the index or substring of name)
    - Default: The default input device on the computer
- `-L`/ `--list-devices`: List all input devices on the computer and then exit

## Errors

`OSError: PortAudio library not found`
--> install libportaudio2 (ubuntu) / portaudio (fedora)

## How to build

Run `pyinstaller main.spec` or `python -m PyInstaller main.spec`
