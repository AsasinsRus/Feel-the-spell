# Feeling the spell - Audio server

## Setup

1. Install Python
2. Run `pip install -r requirements.txt`

## How to run

`python main.py` or `./main.py`

### Arguments

- `s`/`--single-word` makes the server react faster to words and sends them word for word, but may get them wrong
- `-l`/`--language`: Sets the language detected (for example. "en-us" or "de")
    - Default: "en-us"
- `-d`/`--device`: Sets the input device to be used (either the index or substring of name)
    - Default: The default input device on the computer
- `-L`/ `--list-devices`: List all input devices on the computer and then exit

## Errors

`OSError: PortAudio library not found`
--> install libportaudio2 (ubuntu) / portaudio (fedora)
