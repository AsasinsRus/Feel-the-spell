#!/usr/bin/env python 

from server import TCPServer
import argparse
from audio import AudioAnalyzer, all_audio_devices

def main():
    parser = argparse.ArgumentParser(
        description=__doc__,
        formatter_class=argparse.RawDescriptionHelpFormatter)

    parser.add_argument("-w", "--whole-line", action="store_true", help="If set the server will wait for the whole line to be spoken")
    parser.add_argument(
        "-l", "--language", type=str, help="language model; e.g. en-us, de, ...; default is en-us")
    parser.add_argument(
        "-d", "--device", type=int,
        help="input device (numeric ID); defaults to the default device, if possible")
    parser.add_argument(
        "-L", "--list-devices", action="store_true",
        help="show list of audio devices and exit")
    args = parser.parse_args()

    if args.list_devices:
        print(all_audio_devices())
        return

    analyzer = AudioAnalyzer(args.device, args.language, False if args.whole_line else True)

    server = TCPServer()

    for text in analyzer.listen():
        print(text)
        server.send(text)

if __name__ == "__main__":
    main()

